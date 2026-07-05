import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { AlertCircle, ArrowLeft, Copy, Gavel, Check, Loader2 } from 'lucide-react';
import { AccordionSection } from '../components/AccordionSection';
import { AwaySummaryView } from '../components/AwaySummaryView';
import { defaultTerritoryHint, HexMapView } from '../components/HexMapView';
import { formatGateCountdown } from '../components/TickClock';
import { IntelligenceFeed } from '../components/IntelligenceFeed';
import { MatchSidebar, type MatchSection } from '../components/MatchSidebar';
import { CommandStatusBar } from '../components/CommandStatusBar';
import { StrategicAdvisorPanel } from '../components/StrategicAdvisorPanel';
import { TechTreeView } from '../components/TechTreeView';
import { TickChronicle } from '../components/TickChronicle';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
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
  const [focusedGateId, setFocusedGateId] = useState<string | null>(null);
  const [territoryMeta, setTerritoryMeta] = useState(defaultTerritoryHint());
  const [activeSection, setActiveSection] = useState<MatchSection>('territory');
  const [, setClock] = useState(0);

  const scrollToSection = (section: MatchSection) => {
    setActiveSection(section);
    const targetId =
      section === 'economy' && focusedGate ? 'section-gate' : `section-${section}`;
    document.getElementById(targetId)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  };

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
    if (!summary) return;
    const gates = summary.pendingGates.filter((g) => g.civilizationId === civId);
    if (gates.length === 0) {
      setFocusedGateId(null);
      return;
    }
    if (!focusedGateId || !gates.some((g) => g.gateId === focusedGateId)) {
      setFocusedGateId(gates[0].gateId);
    }
  }, [summary, civId, focusedGateId]);

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

  const handleClaimHex = async (tileId: string) => {
    if (!session) return;
    setBusy(true);
    try {
      const result = await api.claimTerritory(matchId, session.civilizationId, tileId);
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

  if (loading && !summary) {
    return (
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading match…
      </div>
    );
  }
  if (!summary) {
    return (
      <Card>
        <CardContent className="space-y-4 pt-6">
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{error ?? 'Match not found'}</AlertDescription>
          </Alert>
          <Button variant="outline" asChild>
            <Link to="/">← Home</Link>
          </Button>
        </CardContent>
      </Card>
    );
  }

  const ended = summary.status === 'Ended';
  const inLobby = summary.status === 'Lobby';
  const isHost = session?.playerId === summary.hostPlayerId;
  const canStart = inLobby && isHost && summary.readyCount >= summary.minPlayers;
  const myCiv = summary.civilizations.find((c) => c.id === civId);
  const rivals = summary.civilizations.filter((c) => c.id !== civId);
  const myGates = summary.pendingGates.filter((g) => g.civilizationId === civId);
  const focusedGate = myGates.find((g) => g.gateId === focusedGateId) ?? myGates[0] ?? null;
  const gateFocusForHero =
    advisor?.gateFocus && focusedGate && advisor.gateFocus.gateId === focusedGate.gateId
      ? advisor.gateFocus
      : null;
  const myCities = summary.regions.filter((r) => r.controllingCivilizationId === civId);
  const otherCities = summary.regions.filter((r) => r.controllingCivilizationId !== civId);
  const showModernStats = (summary.startingTier >= 4) || (myCiv?.tier ?? 1) >= 4;
  const hasAway = !ended && (summary.awaySummaryStructured || summary.awaySummary) && summary.tickCount > 0;
  const totalPopulation = myCities.reduce((sum, c) => sum + c.population, 0);
  const avgYield = myCities.length > 0
    ? myCities.reduce((sum, c) => sum + c.resources, 0) / myCities.length
    : 0;

  const gatePanel = focusedGate && (
    <section key={focusedGate.gateId} className="gate-hero gate-hero-rail" id="section-gate">
      <div className="gate-rail-head">
        <div className="gate-rail-head-top">
          <span className="gc-urgent-badge">Urgent decision</span>
          <span className="gate-hero-label gate-rail-countdown">
            {formatGateCountdown(focusedGate.expiresAt)}
          </span>
        </div>
        <p className="gate-rail-queue-label">
          Queue {focusedGate.queueIndex ?? 1} of {focusedGate.queueTotal ?? myGates.length}
        </p>
      </div>

      {myGates.length > 1 && (
        <div className="gate-rail-tabs">
          <Tabs value={focusedGate.gateId} onValueChange={setFocusedGateId}>
            <TabsList className="gate-rail-tablist h-auto w-full flex-col items-stretch gap-1 bg-muted/40 p-1">
              {myGates.map((gate) => (
                <TabsTrigger key={gate.gateId} value={gate.gateId} className="gate-rail-tab text-left text-xs">
                  {gate.title}
                </TabsTrigger>
              ))}
            </TabsList>
          </Tabs>
        </div>
      )}

      {(focusedGate.contextRegionName || focusedGate.contextFactionName) && (
        <div className="gate-context-row gate-rail-context flex flex-wrap gap-2">
          {focusedGate.contextRegionName && (
            <Badge variant="outline" className="gate-context-chip font-normal">{focusedGate.contextRegionName}</Badge>
          )}
          {focusedGate.contextFactionName && (
            <Badge variant="outline" className="gate-context-chip font-normal">{focusedGate.contextFactionName}</Badge>
          )}
        </div>
      )}

      <div className="gate-rail-body">
        <h2 className="gate-title gate-rail-title">{focusedGate.title}</h2>
        <p className="gate-desc gate-rail-desc">{focusedGate.description}</p>
        <p className="gate-impact-note gate-rail-impact">
          Resolving unlocks auto-research and applies effects on the next tick.
        </p>
        <div className="gate-options-grid gate-rail-options">
          {focusedGate.options.map((opt, i) => {
            const guidance = gateFocusForHero?.options.find((o) => o.optionId === opt.id);
            const isRecommended = guidance?.stance === 'recommended';
            const isCaution = guidance?.stance === 'caution';
            return (
              <button
                key={opt.id}
                type="button"
                className={cn(
                  'gate-option-card gate-rail-option text-left',
                  gateOptionClass(i, opt.label),
                  isRecommended && 'gate-option-recommended',
                  isCaution && 'gate-option-caution',
                )}
                disabled={busy || !session}
                onClick={() => void handleResolve(focusedGate.gateId, opt.id)}
              >
                <span className="gate-option-card-top">
                  <strong>{opt.label}</strong>
                  {isRecommended && <Badge variant="success" className="gate-option-badge">Recommended</Badge>}
                  {isCaution && <Badge variant="warning" className="gate-option-badge">Risky</Badge>}
                </span>
                <span className="gate-option-card-desc">{opt.description}</span>
                {opt.impactHint && <span className="gate-option-card-impact">{opt.impactHint}</span>}
              </button>
            );
          })}
        </div>
      </div>
    </section>
  );

  return (
    <div className="match-page governor-command">
      <header className="gc-command-bar">
        <div>
          <p className="gc-command-title">Governor command</p>
          <p className="gc-command-sector">
            {session?.civilizationName ?? 'Spectating'} :: {summary.modeDisplayName}
            {ended && ' :: match ended'}
          </p>
        </div>
        <div className="match-hud-right flex items-center gap-2">
          {inLobby && (
            <Badge variant="secondary">Lobby · {summary.readyCount}/{summary.minPlayers} ready</Badge>
          )}
          {ended && (
            <Badge variant="outline">Finished · tick {summary.tickCount}/{summary.maxTicks}</Badge>
          )}
          {!inLobby && (
            <Button type="button" variant="outline" size="sm" className="font-mono text-xs" onClick={() => void copyJoinCode(summary.joinCode)}>
              {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
              {copied ? 'copied' : summary.joinCode}
            </Button>
          )}
          <Button variant="ghost" size="icon" asChild>
            <Link to="/" aria-label="Home"><ArrowLeft className="h-4 w-4" /></Link>
          </Button>
          {myCiv && !inLobby && <Badge className="badge-tier-hud">{tierLabel(myCiv.tier)}</Badge>}
        </div>
      </header>

      {!inLobby && myCiv && !ended && (
        <CommandStatusBar
          summary={summary}
          myCiv={myCiv}
          modeId={summary.modeId}
          population={totalPopulation}
          resourceYield={avgYield}
          cityCount={myCities.length}
          rivalCount={rivals.length}
          pendingGateCount={myGates.length}
          formatPopulation={formatPopulation}
        />
      )}

      <div className="gc-workspace">
        <MatchSidebar
          active={activeSection}
          civilizationName={session?.civilizationName}
          hasUrgentGate={!!focusedGate}
          onNavigate={scrollToSection}
          onExecuteCommands={
            focusedGate
              ? () => scrollToSection('economy')
              : session && !ended && !inLobby
                ? () => scrollToSection('economy')
                : undefined
          }
        />

        <div className="gc-primary">
          <div className={cn('gc-command-stage', focusedGate && !inLobby && 'gc-command-stage-has-gate')}>
            {hexMap && (
              <aside id="section-territory" className="match-map-panel">
                <div className="territory-panel-head">
                  <span className="territory-panel-label">Strategic map</span>
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

            {focusedGate && !inLobby && (
              <aside className="gc-decision-panel" aria-label="Decision panel">
                <header className="gc-decision-head">
                  <div className="gc-decision-head-title">
                    <Gavel className="gc-decision-icon" aria-hidden />
                    <h2 className="gc-panel-title">Decision panel</h2>
                  </div>
                  <span className="gate-pulse-dots" aria-hidden>
                    <span className="gate-pulse-dot gate-pulse-dot-live" />
                    <span className="gate-pulse-dot gate-pulse-dot-dim" />
                  </span>
                </header>
                {gatePanel}
              </aside>
            )}
          </div>

          {!inLobby && (
            <div id="section-intel" className="gc-briefing-below">
              <IntelligenceFeed
                summary={summary}
                myCiv={myCiv}
                civilizationName={session?.civilizationName}
                dashboard={dashboard}
                cityCount={myCities.length}
              />
            </div>
          )}

          <div className="match-layout">
            <div className="match-main">
          {!session && (
            <Card className="join-banner border-dashed">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm">Join match</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-2 sm:flex-row">
                <Input value={playerName} onChange={(e) => setPlayerName(e.target.value)} placeholder="Governor name" />
                <Button disabled={busy} onClick={() => void handleJoin()}>
                  Join
                </Button>
              </CardContent>
            </Card>
          )}

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
            <Card className="lobby-card">
              <CardHeader className="lobby-card-head flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium uppercase tracking-wide text-muted-foreground">Lobby</CardTitle>
                <Button type="button" variant="outline" size="sm" className="inline-code font-mono text-xs" onClick={() => void copyJoinCode(summary.joinCode)}>
                  {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
                  {copied ? 'copied' : summary.joinCode}
                </Button>
              </CardHeader>
              <CardContent className="space-y-4">
              <ul className="lobby-list-compact space-y-2">
                {summary.players.map((p) => (
                  <li key={p.playerId} className="flex items-center justify-between gap-2 text-sm">
                    <span>
                      {p.playerName}
                      <span className="ml-2 text-muted-foreground">{p.civilizationName}</span>
                    </span>
                    {p.isReady && <Badge variant="success">ready</Badge>}
                  </li>
                ))}
              </ul>
              {session && (
                <div className="flex flex-wrap gap-2 lobby-actions">
                  {(() => {
                    const me = summary.players.find((p) => p.playerId === session.playerId);
                    const ready = me?.isReady ?? false;
                    return (
                      <Button variant="secondary" disabled={busy} onClick={() => void handleReady(!ready)}>
                        {ready ? 'Not ready' : 'Ready up'}
                      </Button>
                    );
                  })()}
                  {canStart && (
                    <Button disabled={busy} onClick={() => void handleStart()}>
                      Start match
                    </Button>
                  )}
                </div>
              )}
              </CardContent>
            </Card>
          )}

          {!inLobby && (
            <>
              {(myCiv || (session && dashboard && !ended)) && (
                <section id="section-economy" className="match-command">
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
                          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        >
                          {POLICY_PRESETS.map((p) => (
                            <option key={p.id} value={p.id}>
                              {p.id === policyPreset ? `Current policy: ${p.label}` : p.label}
                            </option>
                          ))}
                        </select>
                      </div>
                      <Button
                        type="button"
                        size="sm"
                        className="btn-save-policy"
                        disabled={busy}
                        onClick={() => void handlePolicySave()}
                      >
                        Save policy
                      </Button>
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
                  activeGate={focusedGate}
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
                    <div id="section-civics" className="city-grid">
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
                  <div id="section-rivals">
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
                  </div>
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
                    <div id="section-tech">
                    <TechTreeView
                      nodes={dashboard.techTree ?? []}
                      currentTier={myCiv?.tier ?? 1}
                      recommendedId={dashboard.recommendedTech?.id}
                      startingTier={summary.startingTier}
                    />
                    </div>
                  </AccordionSection>
                )}

                <AccordionSection icon="article" title="Match log archive" defaultOpen={false}>
                  <p className="muted" style={{ margin: '0 0 0.75rem', fontSize: '12px' }}>
                    Victory TTS {summary.victoryTier}+ · {Math.round(summary.victoryStabilityMin)}+ stability
                    {summary.llmStatus && ` · ${summary.llmStatus.statusMessage}`}
                  </p>
                  <TickChronicle
                    tickLogs={summary.tickLogs}
                    civilizationName={session?.civilizationName ?? myCiv?.name}
                    newestFirst={false}
                    maxHeight="min(36vh, 360px)"
                  />
                </AccordionSection>
              </div>
            </>
          )}

          {error && (
            <Alert variant="destructive" className="match-error mt-4">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
