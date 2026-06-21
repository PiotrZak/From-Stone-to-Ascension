import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { TechTreeView } from '../components/TechTreeView';
import {
  api,
  loadSession,
  POLICY_PRESETS,
  saveSession,
  type CivDashboard,
  type Civilization,
  type MatchSummary,
  type PlayerSession,
  type Region,
} from '../api';

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
        <span className="badge badge-tier">TTS {civ.tier}</span>
        {!compact && <span className="muted">{civ.policyLabel}</span>}
      </div>
      {!compact && (
        <p className="muted vitals-meta">{civ.techCount} technologies · avg stability {Math.round(civ.averageStability)}</p>
      )}
      <StabilityBar label="Political" value={civ.politicalStability} />
      <StabilityBar label="Economic" value={civ.economicStability} />
      <StabilityBar label="Tech" value={civ.technologicalStability} />
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
  const [awayOpen, setAwayOpen] = useState(true);
  const [logOpen, setLogOpen] = useState(false);
  const [copied, setCopied] = useState(false);
  const [, setClock] = useState(0);

  const civId = session?.civilizationId ?? 'civ-player';

  useEffect(() => {
    const timer = setInterval(() => setClock((t) => t + 1), 1000);
    return () => clearInterval(timer);
  }, []);

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
  const showModernStats = (myCiv?.tier ?? 1) >= 4;
  const nextTickLabel = summary.isTickDue ? 'due now' : formatCountdown(summary.nextTickAt);

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

          {!inLobby && !ended && (
            <>
              <TickProgress tickCount={summary.tickCount} maxTicks={summary.maxTicks} />
              <div className="tick-countdown">
                <span className="muted">Next tick</span>
                <strong className={summary.isTickDue ? 'due-now' : ''}>{nextTickLabel}</strong>
              </div>
            </>
          )}
        </div>

        {!inLobby && !ended && (
          <p className="victory-banner">
            Victory: reach <strong>TTS {summary.victoryTier}+</strong> with{' '}
            <strong>{Math.round(summary.victoryStabilityMin)}+</strong> average stability
          </p>
        )}
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

      {myGates.map((gate) => (
        <section key={gate.gateId} className="card gate-hero">
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

            {!ended && summary.awaySummary && summary.tickCount > 0 && (
              <section className="card panel-block away-block">
                <button type="button" className="panel-toggle" onClick={() => setAwayOpen((v) => !v)}>
                  <span>While you were away</span>
                  <span className="muted">{awayOpen ? '▾' : '▸'}</span>
                </button>
                {awayOpen && <pre className="away-summary">{summary.awaySummary}</pre>}
              </section>
            )}

            {summary.regions.length > 0 && (
              <section className="card panel-block">
                <h3 className="panel-title">Cities</h3>
                {!showModernStats && (
                  <p className="muted panel-hint">Modern economic stats appear at TTS 4+.</p>
                )}
                <div className="city-grid">
                  {myCities.map((city) => (
                    <CityCard key={city.id} city={city} highlight showModernStats={showModernStats} />
                  ))}
                  {otherCities.map((city) => (
                    <CityCard key={city.id} city={city} highlight={false} showModernStats={showModernStats} />
                  ))}
                </div>
              </section>
            )}

            {rivals.length > 0 && (
              <section className="card panel-block">
                <h3 className="panel-title">Rivals</h3>
                {rivals.map((r) => (
                  <CivVitals key={r.id} civ={r} compact />
                ))}
              </section>
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

              {dashboard.crime && myCiv && myCiv.tier >= 4 && (
                <section className="card panel-block">
                  <h3 className="panel-title">Socioeconomic pressure</h3>
                  <p className="muted">
                    Avg crime pressure <strong>{dashboard.crime.averageCrimePressure.toFixed(0)}</strong>
                    {dashboard.crime.cybersecurityMitigationActive && (
                      <span className="mitigation-tag"> · cybersecurity −40% impact</span>
                    )}
                  </p>
                </section>
              )}
            </div>
          )}

          {session && dashboard && !inLobby && (
            <section className="card panel-block tech-tree-panel">
              <div className="tech-tree-panel-head">
                <h3 className="panel-title">Technology tree</h3>
                <p className="muted">
                  {dashboard.researchSlotsPerTurn} researches per tick · {dashboard.researchedTech.length} completed
                  {dashboard.recommendedTech ? ` · next: ${dashboard.recommendedTech.name}` : ''}
                </p>
              </div>
              <TechTreeView
                nodes={dashboard.techTree ?? []}
                currentTier={myCiv?.tier ?? 1}
                recommendedId={dashboard.recommendedTech?.id}
              />
            </section>
          )}
        </div>
      )}

      {summary.tickLogs.length > 0 && (
        <section className="card log-block">
          <button type="button" className="panel-toggle" onClick={() => setLogOpen((v) => !v)}>
            <span>Match log ({summary.tickLogs.length} ticks)</span>
            <span className="muted">{logOpen ? '▾' : '▸'}</span>
          </button>
          {logOpen && (
            <div className="log-scroll">
              {summary.tickLogs.map((entry) => (
                <div key={entry.tick} className="log-tick">
                  <p className="log-tick-title">Tick {entry.tick}</p>
                  <ul className="simple-list compact">
                    {entry.lines.map((line) => (
                      <li key={line} className="muted">{line}</li>
                    ))}
                  </ul>
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {error && <p className="error match-error">{error}</p>}
    </div>
  );
}
