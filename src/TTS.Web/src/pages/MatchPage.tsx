import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { AwaySummaryView } from '../components/AwaySummaryView';
import { CollapsibleSection } from '../components/CollapsibleSection';
import { TechTreeView } from '../components/TechTreeView';
import {
  api,
  loadSession,
  POLICY_PRESETS,
  saveSession,
  type AdvisorBriefing,
  type CivDashboard,
  type Civilization,
  type MatchSummary,
  type PlayerSession,
  type Region,
} from '../api';
import { tierClass, tierLabel } from '../tierLabels';
import { llmStatusLabel, llmStatusTone } from '../llmStatus';
import type { LlmLayerStatus } from '../api';

function formatCountdown(targetIso: string): string {
  const sec = Math.max(0, Math.floor((new Date(targetIso).getTime() - Date.now()) / 1000));
  if (sec === 0) return 'now';
  if (sec < 3600) return `${Math.floor(sec / 60)}m ${sec % 60}s`;
  return `${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
}

function formatPopulation(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${Math.round(n / 1_000)}k`;
  return String(n);
}

function statusLabel(status: string): string {
  switch (status) {
    case 'Lobby': return 'Lobby';
    case 'Active': return 'In progress';
    case 'Ended': return 'Finished';
    default: return status;
  }
}

function stabilityTone(value: number): string {
  if (value >= 70) return 'good';
  if (value >= 40) return 'mid';
  return 'low';
}

function CityCard({ city, highlight, showModernStats }: { city: Region; highlight: boolean; showModernStats: boolean }) {
  const hasData = showModernStats && city.gdpPerCapita > 0;
  return (
    <div className={`city-card${highlight ? ' city-card-mine' : ''}`}>
      <div className="city-card-head">
        <strong>{city.name}</strong>
        {city.controllingCivilizationName && (
          <span className="muted">{city.controllingCivilizationName}</span>
        )}
      </div>
      <div className="city-stats">
        <span>Pop {formatPopulation(city.population)}</span>
        <span>Infra {Math.round(city.infrastructure)}</span>
        <span>Resources {Math.round(city.resources)}</span>
      </div>
      {hasData && (
        <p className="muted city-modern">
          GDP/cap ${Math.round(city.gdpPerCapita / 1000)}k · unemployment {city.unemploymentRate.toFixed(1)}%
          · health {Math.round(city.economicHealth)}
          {city.crimePressure > 0 ? ` · crime ${Math.round(city.crimePressure)}` : ''}
        </p>
      )}
    </div>
  );
}

function StabilityBar({ label, value }: { label: string; value: number }) {
  const pct = Math.round(Math.max(0, Math.min(100, value)));
  return (
    <div className="stability-row">
      <span className="stability-label">{label}</span>
      <div className="stability-track">
        <div className={`stability-fill stability-${stabilityTone(pct)}`} style={{ width: `${pct}%` }} />
      </div>
      <span className="stability-value">{pct}</span>
    </div>
  );
}

function CivVitals({ civ, compact }: { civ: Civilization; compact?: boolean }) {
  return (
    <div className={`vitals-block${compact ? ' vitals-compact' : ''}`}>
      <div className="vitals-head">
        <strong>{civ.name}</strong>
        <span className={`badge badge-tier ${tierClass(civ.tier)}`}>TTS {civ.tier}</span>
        {!compact && <span className="muted">{civ.policyLabel}</span>}
      </div>
      {!compact && (
        <p className="muted vitals-meta">
          {tierLabel(civ.tier)} · {civ.techCount} technologies · avg stability {Math.round(civ.averageStability)}
        </p>
      )}
      {civ.lastAction && (
        <p className="muted civ-last-action">Last: {civ.lastAction}</p>
      )}
      <StabilityBar label="Political" value={civ.politicalStability} />
      <StabilityBar label="Economic" value={civ.economicStability} />
      <StabilityBar label="Tech" value={civ.technologicalStability} />
    </div>
  );
}

function LlmStatusStrip({ status, inLobby }: { status: LlmLayerStatus | null; inLobby: boolean }) {
  if (inLobby || !status) return null;

  const tone = llmStatusTone(status);
  return (
    <div className={`llm-status-strip llm-status-${tone}`}>
      <div className="llm-status-head">
        <span className={`badge badge-llm badge-llm-${tone}`}>{llmStatusLabel(status)}</span>
        {status.providerEnabled && (
          <span className="muted llm-status-provider">
            {status.provider} · {status.model}
          </span>
        )}
      </div>
      <p className="muted llm-status-message">{status.statusMessage}</p>
      {status.anyRivalEligible && (
        <p className="muted llm-status-meta">
          Turn budget {status.turnCallsUsedThisTick}/{status.maxTurnCallsPerTick} this tick
          {status.lastRivalRunner ? ` · last rival runner: ${status.lastRivalRunner}` : ''}
        </p>
      )}
    </div>
  );
}

function TickProgress({ tickCount, maxTicks }: { tickCount: number; maxTicks: number }) {
  const pct = maxTicks > 0 ? Math.round((tickCount / maxTicks) * 100) : 0;
  return (
    <div className="tick-progress">
      <div className="tick-progress-labels">
        <span>Tick {tickCount}</span>
        <span className="muted">of {maxTicks}</span>
      </div>
      <div className="stability-track tick-progress-track">
        <div className="stability-fill stability-good" style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}

export function MatchPage() {
  const { matchId = '' } = useParams();
  const [summary, setSummary] = useState<MatchSummary | null>(null);
  const [dashboard, setDashboard] = useState<CivDashboard | null>(null);
  const [session, setSession] = useState<PlayerSession | null>(() => loadSession(matchId));
  const [playerName, setPlayerName] = useState('Governor');
  const [policyPreset, setPolicyPreset] = useState('balanced');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [awayOpen, setAwayOpen] = useState(false);
  const [logOpen, setLogOpen] = useState(false);
  const [techTreeOpen, setTechTreeOpen] = useState(false);
  const [citiesOpen, setCitiesOpen] = useState(false);
  const [rivalsOpen, setRivalsOpen] = useState(false);
  const [advisorOpen, setAdvisorOpen] = useState(false);
  const [matchInfoOpen, setMatchInfoOpen] = useState(false);
  const [copied, setCopied] = useState(false);
  const [advisor, setAdvisor] = useState<AdvisorBriefing | null>(null);
  const [advisorLoading, setAdvisorLoading] = useState(false);
  const [, setClock] = useState(0);

  const civId = session?.civilizationId ?? 'civ-player';

  useEffect(() => {
    const timer = setInterval(() => setClock((t) => t + 1), 1000);
    return () => clearInterval(timer);
  }, []);

  const refreshAdvisor = useCallback(async () => {
    if (!matchId || !session) return;
    const tier = summary?.civilizations.find((c) => c.id === civId)?.tier ?? 0;
    if (tier < 4) {
      setAdvisor(null);
      return;
    }
    setAdvisorLoading(true);
    try {
      setAdvisor(await api.getAdvisorBriefing(matchId, civId));
    } catch {
      setAdvisor(null);
    } finally {
      setAdvisorLoading(false);
    }
  }, [matchId, civId, session, summary?.civilizations]);

  const refresh = useCallback(async () => {
    if (!matchId) return;
    try {
      const next = await api.getMatch(matchId);
      setSummary(next);
      if (session || loadSession(matchId)) {
        const dash = await api.getCivDashboard(matchId, civId);
        setDashboard(dash);
        setPolicyPreset(dash.presetId === 'custom' ? 'balanced' : dash.presetId);
      }
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load match');
    } finally {
      setLoading(false);
    }
  }, [matchId, civId, session]);

  useEffect(() => {
    void refreshAdvisor();
  }, [refreshAdvisor, summary?.tickCount]);

  useEffect(() => {
    void refresh();
    const ms = summary?.modeId === 'dev-blitz-3m' ? 3000 : 15000;
    const timer = setInterval(() => void refresh(), ms);
    return () => clearInterval(timer);
  }, [refresh, summary?.modeId]);

  const copyJoinCode = async (code: string) => {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      /* ignore */
    }
  };

  const handleJoin = async () => {
    setBusy(true);
    try {
      const joined = await api.joinMatch(matchId, playerName);
      const next = {
        playerId: joined.playerId,
        playerName: joined.playerName,
        civilizationId: joined.civilizationId,
        civilizationName: joined.civilizationName,
      };
      saveSession(matchId, next);
      setSession(next);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to join');
    } finally {
      setBusy(false);
    }
  };

  const handleResolve = async (gateId: string, optionId: string) => {
    if (!session) return;
    setBusy(true);
    try {
      await api.resolveDecision(matchId, session.civilizationId, gateId, optionId);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to resolve decision');
    } finally {
      setBusy(false);
    }
  };

  const handlePolicySave = async () => {
    if (!session) return;
    setBusy(true);
    try {
      const dash = await api.updatePolicy(matchId, session.civilizationId, policyPreset);
      setDashboard(dash);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update policy');
    } finally {
      setBusy(false);
    }
  };

  const handleReady = async (ready: boolean) => {
    if (!session) return;
    setBusy(true);
    try {
      await api.setReady(matchId, session.playerId, ready);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update ready status');
    } finally {
      setBusy(false);
    }
  };

  const handleStart = async () => {
    if (!session) return;
    setBusy(true);
    try {
      await api.startMatch(matchId, session.playerId);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to start match');
    } finally {
      setBusy(false);
    }
  };

  if (loading && !summary) return <p className="muted">Loading match…</p>;
  if (!summary) {
    return (
      <div className="card">
        <p className="error">{error ?? 'Match not found'}</p>
        <Link to="/">← Back to home</Link>
      </div>
    );
  }

  const ended = summary.status === 'Ended';
  const inLobby = summary.status === 'Lobby';
  const isHost = session?.playerId === summary.hostPlayerId;
  const canStart = inLobby && isHost && summary.readyCount >= summary.minPlayers;
  const myCiv = summary.civilizations.find((c) => c.id === civId);
  const rivals = summary.civilizations.filter((c) => c.id !== civId);
  const myGates = summary.pendingGates.filter((g) => g.civilizationId === civId);
  const myCities = summary.regions.filter((r) => r.controllingCivilizationId === civId);
  const otherCities = summary.regions.filter((r) => r.controllingCivilizationId !== civId);
  const showModernStats = (summary.startingTier >= 4) || (myCiv?.tier ?? 1) >= 4;
  const nextTickLabel = summary.isTickDue ? 'due now' : formatCountdown(summary.nextTickAt);
  const showOnboarding = !ended && !inLobby && summary.tickCount === 0 && session;

  return (
    <div className="match-page">
      <header className="match-header card">
        <div className="row spread match-header-top">
          <Link to="/" className="back-link">← Home</Link>
          <span className={`badge badge-status badge-${summary.status.toLowerCase()}`}>
            {statusLabel(summary.status)}
          </span>
        </div>

        <h2 className="match-title">{summary.modeDisplayName}</h2>

        {myCiv && !inLobby && (
          <div className={`era-band ${tierClass(myCiv.tier)}`}>
            <span className="era-band-label">{tierLabel(myCiv.tier)}</span>
            <span className="muted">TTS {myCiv.tier}</span>
          </div>
        )}

        {!inLobby && !ended && (
          <div className="match-header-summary">
            <TickProgress tickCount={summary.tickCount} maxTicks={summary.maxTicks} />
            <div className="tick-countdown">
              <span className="muted">Next tick</span>
              <strong className={summary.isTickDue ? 'due-now' : ''}>{nextTickLabel}</strong>
            </div>
          </div>
        )}

        <CollapsibleSection
          title="Match info"
          subtitle={`Code ${summary.joinCode}`}
          open={matchInfoOpen}
          onToggle={setMatchInfoOpen}
          className="match-info-panel"
          badge={
            summary.llmStatus && !inLobby ? (
              <span className={`badge badge-llm badge-llm-${llmStatusTone(summary.llmStatus)}`}>
                {llmStatusLabel(summary.llmStatus)}
              </span>
            ) : undefined
          }
        >
          <div className="match-meta">
            <button
              type="button"
              className="join-code-btn"
              onClick={() => void copyJoinCode(summary.joinCode)}
              title="Copy join code"
            >
              <span className="join-code-label">Code</span>
              <span className="join-code-value">{summary.joinCode}</span>
              <span className="muted">{copied ? 'Copied' : 'Copy'}</span>
            </button>
          </div>

          {summary.llmStatus && !inLobby && (
            <LlmStatusStrip status={summary.llmStatus} inLobby={inLobby} />
          )}

          {!inLobby && !ended && (
            <p className="victory-banner">
              Victory: reach <strong>TTS {summary.victoryTier}+</strong> with{' '}
              <strong>{Math.round(summary.victoryStabilityMin)}+</strong> average stability
              {summary.startingTier > 1 && (
                <> · started at TTS {summary.startingTier}</>
              )}
            </p>
          )}
        </CollapsibleSection>
      </header>

      {!session ? (
        <div className="card join-banner">
          <p>Join this match to play as a governor.</p>
          <div className="row join-row">
            <input value={playerName} onChange={(e) => setPlayerName(e.target.value)} placeholder="Your name" />
            <button className="btn btn-primary" disabled={busy} onClick={() => void handleJoin()}>
              Join match
            </button>
          </div>
        </div>
      ) : (
        <div className="player-chip card">
          <span>{session.playerName}</span>
          <span className="muted">·</span>
          <strong>{session.civilizationName}</strong>
          {isHost && <span className="badge badge-host">Host</span>}
        </div>
      )}

      {showOnboarding && (
        <p className="onboarding-strip card muted">
          The world advances on schedule. Resolve gates when they appear; adjust policy between visits.
        </p>
      )}

      {myGates.map((gate) => (
        <section key={gate.gateId} className="card gate-hero gate-hero-sticky">
          <div className="gate-hero-head">
            <span className="gate-hero-label">Decision required</span>
            <span className="gate-timer">
              {formatCountdown(gate.expiresAt)} left · default <strong>{gate.defaultOptionId.toUpperCase()}</strong>
            </span>
          </div>
          <h3 className="gate-title">{gate.title}</h3>
          <p className="fable">{gate.description}</p>
          <div className="gate-options">
            {gate.options.map((opt) => (
              <div key={opt.id} className="gate-option-card">
                <button
                  className={`btn btn-primary gate-option-btn${opt.id === gate.defaultOptionId ? ' gate-default' : ''}`}
                  disabled={busy || !session}
                  onClick={() => void handleResolve(gate.gateId, opt.id)}
                >
                  <span className="gate-option-id">{opt.id.toUpperCase()}</span>
                  <span>{opt.label}</span>
                  {opt.id === gate.defaultOptionId && <span className="gate-default-tag">default</span>}
                </button>
                <p className="muted option-hint">{opt.description}</p>
                {opt.impactHint && <p className="impact-hint">{opt.impactHint}</p>}
              </div>
            ))}
          </div>
        </section>
      ))}

      {ended && summary.results.length > 0 && (
        <section className="card results-block">
          <h3 className="panel-title">Final standings</h3>
          <ol className="results-list">
            {summary.results.map((r) => (
              <li key={r.civilizationId} className={r.civilizationId === civId ? 'results-you' : ''}>
                <span className="results-rank">{r.rank}</span>
                <div className="results-body">
                  <strong>{r.civilizationName}</strong>
                  <span className="muted">
                    TTS {r.tier} · stability {Math.round(r.stability)} · {r.techCount} techs · {r.outcome}
                  </span>
                  {r.outcomeReason && r.outcomeReason !== 'Simulation in progress.' && (
                    <span className="results-reason muted">{r.outcomeReason}</span>
                  )}
                </div>
              </li>
            ))}
          </ol>
        </section>
      )}

      {inLobby && (
        <section className="card panel-block">
          <h3 className="panel-title">Lobby</h3>
          <p className="muted lobby-stats">
            {summary.readyCount}/{summary.minPlayers} ready · {summary.players.length}/{summary.maxPlayers} players
          </p>
          <ul className="lobby-list">
            {summary.players.map((p) => (
              <li key={p.playerId}>
                <span>{p.playerName}</span>
                <span className="muted">{p.civilizationName}</span>
                <span className="lobby-tags">
                  {p.isHost && <span className="badge badge-host">Host</span>}
                  {p.isReady && <span className="badge badge-ready">Ready</span>}
                </span>
              </li>
            ))}
          </ul>
          {session && (
            <div className="row lobby-actions">
              {(() => {
                const me = summary.players.find((p) => p.playerId === session.playerId);
                const ready = me?.isReady ?? false;
                return (
                  <button className="btn" disabled={busy} onClick={() => void handleReady(!ready)}>
                    {ready ? 'Not ready' : 'Ready up'}
                  </button>
                );
              })()}
              {canStart && (
                <button className="btn btn-primary" disabled={busy} onClick={() => void handleStart()}>
                  Start match
                </button>
              )}
            </div>
          )}
          {!session && <p className="muted">Join to ready up.</p>}
        </section>
      )}

      {!inLobby && (
        <div className="match-grid">
          <div className="match-col">
            {myCiv && (
              <section className="card panel-block">
                <h3 className="panel-title">Your civilization</h3>
                <CivVitals civ={myCiv} />
              </section>
            )}

            {!ended && (summary.awaySummaryStructured || summary.awaySummary) && summary.tickCount > 0 && (
              <CollapsibleSection
                title="While you were away"
                subtitle="Recent changes since your last visit"
                open={awayOpen}
                onToggle={setAwayOpen}
                className="card panel-block away-block"
              >
                {summary.awaySummaryStructured
                  ? <AwaySummaryView summary={summary.awaySummaryStructured} />
                  : <pre className="away-summary">{summary.awaySummary}</pre>}
              </CollapsibleSection>
            )}

            {summary.regions.length > 0 && (
              <CollapsibleSection
                title="Cities"
                subtitle={`${summary.regions.length} region${summary.regions.length === 1 ? '' : 's'}`}
                open={citiesOpen}
                onToggle={setCitiesOpen}
                className="card panel-block"
              >
                <div className="city-grid">
                  {myCities.map((city) => (
                    <CityCard key={city.id} city={city} highlight showModernStats={showModernStats} />
                  ))}
                  {otherCities.map((city) => (
                    <CityCard key={city.id} city={city} highlight={false} showModernStats={showModernStats} />
                  ))}
                </div>
              </CollapsibleSection>
            )}

            {rivals.length > 0 && (
              <CollapsibleSection
                title="Rivals"
                subtitle={rivals.map((r) => r.name).join(' · ')}
                open={rivalsOpen}
                onToggle={setRivalsOpen}
                className="card panel-block"
              >
                {rivals.map((r) => (
                  <CivVitals key={r.id} civ={r} compact />
                ))}
              </CollapsibleSection>
            )}
          </div>

          {session && dashboard && !ended && (
            <div className="match-col">
              <section className="card panel-block">
                <h3 className="panel-title">Policy</h3>
                <div className="row join-row policy-row">
                  <select value={policyPreset} onChange={(e) => setPolicyPreset(e.target.value)}>
                    {POLICY_PRESETS.map((p) => (
                      <option key={p.id} value={p.id}>{p.label}</option>
                    ))}
                  </select>
                  <button className="btn btn-primary" disabled={busy} onClick={() => void handlePolicySave()}>
                    Save
                  </button>
                </div>
                <p className="muted">
                  {dashboard.researchStance} · risk {dashboard.riskTolerance.toLowerCase()}
                </p>
                {dashboard.recommendedTech && (
                  <div className="recommended-tech">
                    <span className="muted">Recommended next</span>
                    <strong>{dashboard.recommendedTech.name}</strong>
                    <span className="tech-pill">TTS {dashboard.recommendedTech.tier}</span>
                    <span className="tech-pill tech-pill-branch">{dashboard.recommendedTech.branch}</span>
                  </div>
                )}
              </section>

              {myCiv && myCiv.tier >= 4 && (
                <CollapsibleSection
                  title={myCiv.tier >= 5 ? 'Strategic advisor' : 'Policy advisor'}
                  subtitle={advisor?.briefing ? 'Briefing ready' : 'Tap to consult'}
                  open={advisorOpen}
                  onToggle={setAdvisorOpen}
                  className="card panel-block advisor-panel"
                >
                  <div className="advisor-head">
                    <button
                      type="button"
                      className="btn"
                      disabled={advisorLoading}
                      onClick={() => void refreshAdvisor()}
                    >
                      {advisorLoading ? 'Thinking…' : 'Refresh'}
                    </button>
                  </div>
                  <div className="advisor-chips">
                    {['What should I research?', 'Explain stability', 'Summarize rivals'].map((q) => (
                      <button
                        key={q}
                        type="button"
                        className="btn advisor-chip"
                        disabled={advisorLoading}
                        onClick={() => void refreshAdvisor()}
                      >
                        {q}
                      </button>
                    ))}
                  </div>
                  <p className="fable advisor-text">
                    {advisor?.briefing ?? (advisorLoading ? 'Consulting advisor…' : 'Tap Refresh or a prompt for guidance.')}
                  </p>
                  {advisor && (
                    <p className="muted advisor-source">via {advisor.source}{myCiv.tier < 5 ? ' · LLM at TTS 5+' : ''}</p>
                  )}
                </CollapsibleSection>
              )}

              {dashboard.crime && showModernStats && (
                <CollapsibleSection
                  title="Socioeconomic pressure"
                  subtitle={`Crime ${dashboard.crime.averageCrimePressure.toFixed(0)}`}
                  className="card panel-block crime-panel"
                >
                  <p className="muted">
                    Avg crime pressure <strong>{dashboard.crime.averageCrimePressure.toFixed(0)}</strong>
                    {dashboard.crime.cybersecurityMitigationActive && (
                      <span className="mitigation-tag"> · cybersecurity −40% impact</span>
                    )}
                  </p>
                </CollapsibleSection>
              )}
            </div>
          )}

          {session && dashboard && !inLobby && (
            <CollapsibleSection
              title="Technology tree"
              subtitle={`${dashboard.researchedTech.length} researched · ${dashboard.researchSlotsPerTurn}/tick`}
              open={techTreeOpen}
              onToggle={setTechTreeOpen}
              className="card panel-block tech-tree-panel"
            >
              <TechTreeView
                nodes={dashboard.techTree ?? []}
                currentTier={myCiv?.tier ?? 1}
                recommendedId={dashboard.recommendedTech?.id}
                startingTier={summary.startingTier}
              />
            </CollapsibleSection>
          )}
        </div>
      )}

      {summary.tickLogs.length > 0 && (
        <CollapsibleSection
          title={`Match log (${summary.tickLogs.length} ticks)`}
          subtitle="Tick-by-tick events"
          open={logOpen}
          onToggle={setLogOpen}
          className="card log-block"
        >
          <div className="log-scroll">
            {summary.tickLogs.map((entry) => (
              <div key={entry.tick} className="log-tick">
                <p className="log-tick-title">Tick {entry.tick}</p>
                <ul className="simple-list compact">
                  {entry.lines.map((line) => (
                    <li
                      key={line}
                      className={`muted${line.includes('· LLM') ? ' log-line-llm' : line.includes('· classical') ? ' log-line-classical' : ''}`}
                    >
                      {line}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </CollapsibleSection>
      )}

      {error && <p className="error match-error">{error}</p>}
    </div>
  );
}
