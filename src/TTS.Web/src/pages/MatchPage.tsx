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
import type { LlmLayerStatus } from '../api';

function formatCountdown(targetIso: string): string {
  const sec = Math.max(0, Math.floor((new Date(targetIso).getTime() - Date.now()) / 1000));
  if (sec === 0) return 'now';
  if (sec < 3600) return `${Math.floor(sec / 60)}m`;
  return `${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
}

function formatPopulation(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${Math.round(n / 1_000)}k`;
  return String(n);
}

function stabilityTone(value: number): string {
  if (value >= 70) return 'good';
  if (value >= 40) return 'mid';
  return 'low';
}

function CityLine({ city, mine, modern }: { city: Region; mine: boolean; modern: boolean }) {
  const extra = modern && city.gdpPerCapita > 0
    ? ` · crime ${Math.round(city.crimePressure)}`
    : '';
  return (
    <p className={`city-line${mine ? ' city-line-mine' : ''}`}>
      <strong>{city.name}</strong>
      <span className="muted">
        {city.controllingCivilizationName ? `${city.controllingCivilizationName} · ` : ''}
        pop {formatPopulation(city.population)}{extra}
      </span>
    </p>
  );
}

function CivStatus({ civ }: { civ: Civilization }) {
  const avg = Math.round(civ.averageStability);
  return (
    <div className="civ-status">
      <div className="civ-status-head">
        <div>
          <strong>{civ.name}</strong>
          <span className="muted civ-status-era">{tierLabel(civ.tier)} · TTS {civ.tier}</span>
        </div>
        <div className={`civ-status-avg stability-${stabilityTone(avg)}`}>{avg}</div>
      </div>
      <div className="civ-status-bars" aria-label="Stability pillars">
        {[
          ['Political', civ.politicalStability],
          ['Economic', civ.economicStability],
          ['Tech', civ.technologicalStability],
        ].map(([label, value]) => {
          const pct = Math.round(Math.max(0, Math.min(100, value as number)));
          return (
            <div key={label as string} className="civ-status-bar" title={`${label}: ${pct}`}>
              <div className={`stability-fill stability-${stabilityTone(pct)}`} style={{ width: `${pct}%` }} />
            </div>
          );
        })}
      </div>
      {civ.lastAction && <p className="muted civ-last-action">{civ.lastAction}</p>}
    </div>
  );
}

function RivalLine({ civ }: { civ: Civilization }) {
  return (
    <p className="rival-line">
      <strong>{civ.name}</strong>
      <span className="muted">TTS {civ.tier} · stability {Math.round(civ.averageStability)}</span>
    </p>
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
  const [moreOpen, setMoreOpen] = useState(false);
  const [awayOpen, setAwayOpen] = useState(false);
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
    if (tier < 4) { setAdvisor(null); return; }
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

  useEffect(() => { void refreshAdvisor(); }, [refreshAdvisor, summary?.tickCount]);

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
    } catch { /* ignore */ }
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

  if (loading && !summary) return <p className="muted page-loading">Loading…</p>;
  if (!summary) {
    return (
      <div className="card">
        <p className="error">{error ?? 'Match not found'}</p>
        <Link to="/">← Home</Link>
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
  const hasAway = !ended && (summary.awaySummaryStructured || summary.awaySummary) && summary.tickCount > 0;
  const moreSubtitle = [
    summary.regions.length > 0 ? `${summary.regions.length} cities` : null,
    rivals.length > 0 ? `${rivals.length} rival${rivals.length === 1 ? '' : 's'}` : null,
    dashboard?.researchedTech.length ? `${dashboard.researchedTech.length} techs` : null,
  ].filter(Boolean).join(' · ') || 'World, tech, log';

  return (
    <div className="match-page">
      <header className="match-top card">
        <div className="match-top-row">
          <Link to="/" className="back-link">←</Link>
          <div className="match-top-text">
            <h1 className="match-top-title">{summary.modeDisplayName}</h1>
            <p className="muted match-top-sub">
              {session
                ? `${session.civilizationName}${isHost ? ' · host' : ''}`
                : 'Spectating'}
              {!inLobby && !ended && (
                <> · tick {summary.tickCount}/{summary.maxTicks} · next {nextTickLabel}</>
              )}
            </p>
          </div>
          {myCiv && !inLobby && (
            <span className={`badge badge-tier ${tierClass(myCiv.tier)}`}>TTS {myCiv.tier}</span>
          )}
        </div>
      </header>

      {!session && (
        <div className="card join-banner">
          <div className="row join-row">
            <input value={playerName} onChange={(e) => setPlayerName(e.target.value)} placeholder="Your name" />
            <button className="btn btn-primary" disabled={busy} onClick={() => void handleJoin()}>
              Join
            </button>
          </div>
        </div>
      )}

      {myGates.map((gate) => (
        <section key={gate.gateId} className="card gate-card">
          <p className="gate-kicker">
            Decision · {formatCountdown(gate.expiresAt)} left
            <span className="muted"> · default {gate.defaultOptionId}</span>
          </p>
          <h2 className="gate-title">{gate.title}</h2>
          <p className="gate-desc">{gate.description}</p>
          <div className="gate-options-simple">
            {gate.options.map((opt) => (
              <button
                key={opt.id}
                className={`btn gate-opt${opt.id === gate.defaultOptionId ? ' gate-opt-default' : ''}`}
                disabled={busy || !session}
                onClick={() => void handleResolve(gate.gateId, opt.id)}
              >
                <span className="gate-opt-label">{opt.label}</span>
                {opt.impactHint && <span className="gate-opt-impact">{opt.impactHint}</span>}
              </button>
            ))}
          </div>
        </section>
      ))}

      {ended && summary.results.length > 0 && (
        <section className="card results-block">
          <h2 className="section-label">Results</h2>
          <ol className="results-list">
            {summary.results.map((r) => (
              <li key={r.civilizationId} className={r.civilizationId === civId ? 'results-you' : ''}>
                <span className="results-rank">{r.rank}</span>
                <div className="results-body">
                  <strong>{r.civilizationName}</strong>
                  <span className="muted">TTS {r.tier} · {Math.round(r.stability)} stability · {r.outcome}</span>
                </div>
              </li>
            ))}
          </ol>
        </section>
      )}

      {inLobby && (
        <section className="card">
          <p className="muted lobby-line">
            {summary.readyCount}/{summary.minPlayers} ready · {summary.players.length} players · code{' '}
            <button type="button" className="inline-code" onClick={() => void copyJoinCode(summary.joinCode)}>
              {copied ? 'copied' : summary.joinCode}
            </button>
          </p>
          <ul className="lobby-list-compact">
            {summary.players.map((p) => (
              <li key={p.playerId}>
                {p.playerName}
                <span className="muted">{p.civilizationName}</span>
                {p.isReady && <span className="badge badge-ready">ready</span>}
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
                    {ready ? 'Not ready' : 'Ready'}
                  </button>
                );
              })()}
              {canStart && (
                <button className="btn btn-primary" disabled={busy} onClick={() => void handleStart()}>
                  Start
                </button>
              )}
            </div>
          )}
        </section>
      )}

      {!inLobby && (
        <>
          {hasAway && (
            <CollapsibleSection
              title="While you were away"
              open={awayOpen}
              onToggle={setAwayOpen}
              className="card"
            >
              {summary.awaySummaryStructured
                ? <AwaySummaryView summary={summary.awaySummaryStructured} />
                : <pre className="away-summary">{summary.awaySummary}</pre>}
            </CollapsibleSection>
          )}

          {myCiv && (
            <section className="card">
              <CivStatus civ={myCiv} />
            </section>
          )}

          {session && dashboard && !ended && (
            <section className="card policy-bar">
              <div className="row join-row policy-row">
                <select value={policyPreset} onChange={(e) => setPolicyPreset(e.target.value)} aria-label="Policy">
                  {POLICY_PRESETS.map((p) => (
                    <option key={p.id} value={p.id}>{p.label}</option>
                  ))}
                </select>
                <button className="btn btn-primary" disabled={busy} onClick={() => void handlePolicySave()}>
                  Save
                </button>
              </div>
              {dashboard.recommendedTech && (
                <p className="muted policy-next">
                  Next research: <strong>{dashboard.recommendedTech.name}</strong>
                </p>
              )}
            </section>
          )}

          <CollapsibleSection
            title="More"
            subtitle={moreSubtitle}
            open={moreOpen}
            onToggle={setMoreOpen}
            className="card more-panel"
          >
            <div className="more-sections">
              {!inLobby && !ended && (
                <p className="muted more-line">
                  Victory TTS {summary.victoryTier}+ · {Math.round(summary.victoryStabilityMin)}+ stability · code{' '}
                  <button type="button" className="inline-code" onClick={() => void copyJoinCode(summary.joinCode)}>
                    {copied ? 'copied' : summary.joinCode}
                  </button>
                </p>
              )}

              {summary.llmStatus && !inLobby && (
                <LlmLine status={summary.llmStatus} />
              )}

              {summary.regions.length > 0 && (
                <div className="more-block">
                  <h3 className="more-label">Cities</h3>
                  {myCities.map((c) => <CityLine key={c.id} city={c} mine modern={showModernStats} />)}
                  {otherCities.map((c) => <CityLine key={c.id} city={c} mine={false} modern={showModernStats} />)}
                </div>
              )}

              {rivals.length > 0 && (
                <div className="more-block">
                  <h3 className="more-label">Rivals</h3>
                  {rivals.map((r) => <RivalLine key={r.id} civ={r} />)}
                </div>
              )}

              {dashboard?.crime && showModernStats && (
                <div className="more-block">
                  <h3 className="more-label">Crime pressure</h3>
                  <p className="muted">
                    Average {dashboard.crime.averageCrimePressure.toFixed(0)}
                    {dashboard.crime.cybersecurityMitigationActive && ' · cybersecurity active'}
                  </p>
                </div>
              )}

              {myCiv && myCiv.tier >= 4 && session && (
                <div className="more-block advisor-block">
                  <div className="row spread">
                    <h3 className="more-label">Advisor</h3>
                    <button type="button" className="btn btn-sm" disabled={advisorLoading} onClick={() => void refreshAdvisor()}>
                      {advisorLoading ? '…' : 'Ask'}
                    </button>
                  </div>
                  <p className="advisor-text">{advisor?.briefing ?? 'Tap Ask for guidance.'}</p>
                </div>
              )}

              {dashboard && session && (
                <div className="more-block">
                  <h3 className="more-label">Technology tree</h3>
                  <TechTreeView
                    nodes={dashboard.techTree ?? []}
                    currentTier={myCiv?.tier ?? 1}
                    recommendedId={dashboard.recommendedTech?.id}
                    startingTier={summary.startingTier}
                  />
                </div>
              )}

              {summary.tickLogs.length > 0 && (
                <div className="more-block">
                  <h3 className="more-label">Match log</h3>
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
                </div>
              )}
            </div>
          </CollapsibleSection>
        </>
      )}

      {error && <p className="error match-error">{error}</p>}
    </div>
  );
}

function LlmLine({ status }: { status: LlmLayerStatus }) {
  return <p className="muted more-line">{status.statusMessage}</p>;
}
