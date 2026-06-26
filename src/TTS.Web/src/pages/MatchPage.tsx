import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { AccordionSection } from '../components/AccordionSection';
import { AwaySummaryView } from '../components/AwaySummaryView';
import { defaultTerritoryHint, HexMapView } from '../components/HexMapView';
import { formatGateCountdown, TickClock } from '../components/TickClock';
import { StrategicAdvisorPanel } from '../components/StrategicAdvisorPanel';
import { TechTreeView } from '../components/TechTreeView';
import {
  api,
  loadSession,
  POLICY_PRESETS,
  saveSession,
  type AdvisorBriefing,
  type CivDashboard,
  type HexMap,
  type MatchSummary,
  type PlayerSession,
  type Region,
} from '../api';
import { tierLabel } from '../tierLabels';

function formatPopulation(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${Math.round(n / 1_000)}k`;
  return String(n);
}

function cityStatus(city: Region, modern: boolean): { label: string; tone: 'stable' | 'tension' | 'crisis' } {
  if (modern && city.crimePressure >= 60) return { label: 'Crisis', tone: 'crisis' };
  if (modern && city.crimePressure >= 35) return { label: 'Tension', tone: 'tension' };
  return { label: 'Stable', tone: 'stable' };
}

function gateOptionClass(index: number, label: string): string {
  const lower = label.toLowerCase();
  if (lower.includes('ban') || lower.includes('reject') || lower.includes('deny')) return 'gate-opt-danger';
  if (index === 0) return 'gate-opt-primary';
  return 'gate-opt-neutral';
}

function pillarFillClass(value: number): string {
  if (value >= 70) return 'pillar-fill-green';
  if (value >= 40) return 'pillar-fill-amber';
  return 'pillar-fill-blue';
}

export function MatchPage() {
  const { matchId = '' } = useParams();
  const [summary, setSummary] = useState<MatchSummary | null>(null);
  const [dashboard, setDashboard] = useState<CivDashboard | null>(null);
  const [hexMap, setHexMap] = useState<HexMap | null>(null);
  const [session, setSession] = useState<PlayerSession | null>(() => loadSession(matchId));
  const [playerName, setPlayerName] = useState('Governor');
  const [policyPreset, setPolicyPreset] = useState('balanced');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [copied, setCopied] = useState(false);
  const [advisor, setAdvisor] = useState<AdvisorBriefing | null>(null);
  const [advisorLoading, setAdvisorLoading] = useState(false);
  const [territoryMeta, setTerritoryMeta] = useState(defaultTerritoryHint());
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
      try {
        setHexMap(await api.getHexMap(matchId));
      } catch {
        setHexMap(null);
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

  const handleClaimHex = async (q: number, r: number) => {
    if (!session) return;
    setBusy(true);
    try {
      const result = await api.claimTerritory(matchId, session.civilizationId, q, r);
      if (!result.success) setError(result.message);
      else await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to claim territory');
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

  if (loading && !summary) return <p className="muted page-loading">Loading match…</p>;
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
  const hasAway = !ended && (summary.awaySummaryStructured || summary.awaySummary) && summary.tickCount > 0;
  return (
    <div className="match-page">
      <header className="match-hud">
        <div className="match-hud-left">
          <Link to="/" className="match-hud-back" aria-label="Home">
            <span className="material-symbols-outlined">arrow_back</span>
          </Link>
          <div className="match-hud-identity">
            <h1 className="match-hud-title">{summary.modeDisplayName}</h1>
            <p className="match-hud-sub">
              {session
                ? `${session.civilizationName}${isHost ? ' · host' : ''}`
                : 'Spectating'}
              {ended && ' · match ended'}
            </p>
          </div>
        </div>

        <div className="match-hud-center">
          {inLobby && (
            <div className="match-hud-pill">
              Lobby · {summary.readyCount}/{summary.minPlayers} ready
            </div>
          )}
          {!inLobby && !ended && (
            <TickClock
              modeId={summary.modeId}
              nextTickAt={summary.nextTickAt}
              isTickDue={summary.isTickDue}
              tickCount={summary.tickCount}
              maxTicks={summary.maxTicks}
            />
          )}
          {ended && (
            <div className="match-hud-pill">
              Finished · tick {summary.tickCount}/{summary.maxTicks}
            </div>
          )}
        </div>

        <div className="match-hud-right">
          {!inLobby && (
            <button
              type="button"
              className="match-hud-code"
              onClick={() => void copyJoinCode(summary.joinCode)}
              title="Copy join code"
            >
              {copied ? 'copied' : summary.joinCode}
            </button>
          )}
          {myCiv && !inLobby && (
            <span className="badge-tier-hud">{tierLabel(myCiv.tier)}</span>
          )}
        </div>
      </header>

      <div className="match-layout">
        {hexMap && (
          <aside className="match-map-panel">
            <div className="territory-panel-head">
              <span className="label-caps">Territory</span>
              <span className="territory-panel-meta">{territoryMeta}</span>
            </div>
            <div className="territory-map-body">
              <HexMapView
                map={hexMap}
                myCivilizationId={session?.civilizationId ?? null}
                disabled={busy || ended}
                onClaim={session && !ended && !inLobby ? handleClaimHex : undefined}
                onSelectionChange={(_, meta) => setTerritoryMeta(meta ?? defaultTerritoryHint())}
              />
            </div>
          </aside>
        )}

        <div className="match-main">
          {!session && (
            <div className="join-banner">
              <p className="label-caps">Join match</p>
              <div className="row join-row">
                <input value={playerName} onChange={(e) => setPlayerName(e.target.value)} placeholder="Governor name" />
                <button className="btn btn-primary" disabled={busy} onClick={() => void handleJoin()}>
                  Join
                </button>
              </div>
            </div>
          )}

          {myGates.map((gate) => (
            <section key={gate.gateId} className="gate-hero">
              <div className="gate-hero-head">
                <div className="gate-hero-head-left">
                  <div className="gate-hero-icon">
                    <span className="material-symbols-outlined">gavel</span>
                  </div>
                  <span className="gate-hero-label">Decision · {formatGateCountdown(gate.expiresAt)}</span>
                </div>
                <div className="gate-pulse-dots" aria-hidden>
                  <span className="gate-pulse-dot gate-pulse-dot-live" />
                  <span className="gate-pulse-dot gate-pulse-dot-dim" />
                </div>
              </div>
              <h2 className="gate-title">{gate.title}</h2>
              <p className="gate-desc">{gate.description}</p>
              <div className="gate-options-grid">
                {gate.options.map((opt, i) => (
                  <button
                    key={opt.id}
                    type="button"
                    className={gateOptionClass(i, opt.label)}
                    disabled={busy || !session}
                    onClick={() => void handleResolve(gate.gateId, opt.id)}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </section>
          ))}

          {ended && summary.results.length > 0 && (
            <section className="results-block">
              <h2 className="label-caps">Final standings</h2>
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
            <section className="lobby-card">
              <div className="lobby-card-head">
                <h2 className="label-caps">Lobby</h2>
                <button type="button" className="inline-code" onClick={() => void copyJoinCode(summary.joinCode)}>
                  {copied ? 'copied' : summary.joinCode}
                </button>
              </div>
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
            </section>
          )}

          {!inLobby && (
            <>
              {(myCiv || (session && dashboard && !ended)) && (
                <section className="match-command">
                  {myCiv && (
                    <div className="command-strip-top">
                      <div className="command-strip-vitals">
                        <h3 className="command-strip-name">{myCiv.name}</h3>
                        <p className="command-strip-kicker">Faction overview · {tierLabel(myCiv.tier)}</p>
                        <div className="pillar-grid">
                          {[
                            ['Social', myCiv.politicalStability, 'green'],
                            ['Econ', myCiv.economicStability, 'amber'],
                            ['Order', myCiv.technologicalStability, 'blue'],
                          ].map(([label, value]) => {
                            const pct = Math.round(Math.max(0, Math.min(100, value as number)));
                            return (
                              <div key={label as string}>
                                <div className="pillar-row-label">
                                  <span>{label as string}</span>
                                  <span>{pct}%</span>
                                </div>
                                <div className="pillar-bar">
                                  <div className={pillarFillClass(pct)} style={{ width: `${pct}%` }} />
                                </div>
                              </div>
                            );
                          })}
                        </div>
                        {myCiv.lastAction && <p className="muted policy-next">{myCiv.lastAction}</p>}
                      </div>
                      <div className="command-strip-score">
                        <span className="command-strip-score-value">{Math.round(myCiv.averageStability)}</span>
                        <span className="command-strip-score-label">Stability index</span>
                      </div>
                    </div>
                  )}
                  {session && dashboard && !ended && (
                    <div className="match-command-policy">
                      <div className="policy-select-wrap">
                        <select
                          id="policy-select"
                          value={policyPreset}
                          onChange={(e) => setPolicyPreset(e.target.value)}
                          aria-label="Governance policy"
                        >
                          {POLICY_PRESETS.map((p) => (
                            <option key={p.id} value={p.id}>
                              {p.id === policyPreset ? `Current policy: ${p.label}` : p.label}
                            </option>
                          ))}
                        </select>
                        <span className="material-symbols-outlined policy-select-chevron">expand_more</span>
                      </div>
                      <button
                        type="button"
                        className="btn-save-policy"
                        disabled={busy}
                        onClick={() => void handlePolicySave()}
                      >
                        Save policy
                      </button>
                    </div>
                  )}
                  {dashboard?.recommendedTech && !ended && (
                    <p className="policy-next">
                      Researching next: <strong>{dashboard.recommendedTech.name}</strong>
                    </p>
                  )}
                </section>
              )}

              {myCiv && session && !ended && (
                <StrategicAdvisorPanel
                  tier={myCiv.tier}
                  advisor={advisor}
                  loading={advisorLoading}
                  canRefresh={!!session}
                  canApply={!!session && !busy}
                  llmStatus={summary.llmStatus}
                  activeGate={myGates[0] ?? null}
                  onRefresh={() => void refreshAdvisor()}
                  onApplyRecommendation={(gateId, optionId) => void handleResolve(gateId, optionId)}
                />
              )}

              <div className="match-accordions">
                {hasAway && (
                  <AccordionSection icon="history" title="While you were away" defaultOpen>
                    {summary.awaySummaryStructured
                      ? <AwaySummaryView summary={summary.awaySummaryStructured} />
                      : <pre className="away-summary">{summary.awaySummary}</pre>}
                  </AccordionSection>
                )}

                {summary.regions.length > 0 && (
                  <AccordionSection icon="location_city" title="Metropolitan hubs" defaultOpen={myCities.length > 0}>
                    <div className="city-grid">
                      {[...myCities, ...otherCities].map((city) => {
                        const mine = city.controllingCivilizationId === civId;
                        const status = cityStatus(city, showModernStats);
                        return (
                          <div key={city.id} className={`city-card${mine ? ' city-card-mine' : ''}`}>
                            <div>
                              <span className="city-card-name">{city.name}</span>
                              <p className="muted" style={{ margin: '0.15rem 0 0', fontSize: '12px' }}>
                                {city.controllingCivilizationName ?? 'Unclaimed'} · pop {formatPopulation(city.population)}
                                {showModernStats && city.gdpPerCapita > 0 ? ` · crime ${Math.round(city.crimePressure)}` : ''}
                              </p>
                            </div>
                            <span className={`label-caps city-status-${status.tone}`}>{status.label}</span>
                          </div>
                        );
                      })}
                    </div>
                  </AccordionSection>
                )}

                <AccordionSection icon="groups" title="Global competitors" defaultOpen={rivals.length > 0 && rivals.length <= 3}>
                  {rivals.length === 0 ? (
                    <p className="accordion-empty">Scanning for regional rivals…</p>
                  ) : (
                    <div className="rival-grid">
                      {rivals.map((civ) => (
                        <div key={civ.id} className="rival-card">
                          <strong>{civ.name}</strong>
                          <span>TTS {civ.tier} · {Math.round(civ.averageStability)} stability</span>
                        </div>
                      ))}
                    </div>
                  )}
                </AccordionSection>

                {dashboard?.crime && showModernStats && (
                  <AccordionSection icon="shield" title="Crime pressure">
                    <p className="accordion-empty" style={{ fontStyle: 'normal' }}>
                      Average {dashboard.crime.averageCrimePressure.toFixed(0)}
                      {dashboard.crime.cybersecurityMitigationActive && ' · cybersecurity active'}
                    </p>
                  </AccordionSection>
                )}

                {dashboard && session && (
                  <AccordionSection icon="account_tree" title="Technological tree">
                    <TechTreeView
                      nodes={dashboard.techTree ?? []}
                      currentTier={myCiv?.tier ?? 1}
                      recommendedId={dashboard.recommendedTech?.id}
                      startingTier={summary.startingTier}
                    />
                  </AccordionSection>
                )}

                <AccordionSection icon="article" title="Match log">
                  {!ended && (
                    <p className="muted" style={{ margin: '0 0 0.75rem', fontSize: '12px' }}>
                      Victory TTS {summary.victoryTier}+ · {Math.round(summary.victoryStabilityMin)}+ stability
                      {summary.llmStatus && ` · ${summary.llmStatus.statusMessage}`}
                    </p>
                  )}
                  {summary.tickLogs.length === 0 ? (
                    <p className="accordion-empty">No events logged yet.</p>
                  ) : (
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
                </AccordionSection>
              </div>
            </>
          )}

          {error && <p className="error match-error">{error}</p>}
        </div>
      </div>
    </div>
  );
}
