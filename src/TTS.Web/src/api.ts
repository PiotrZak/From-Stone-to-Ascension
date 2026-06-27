export interface MatchListItem {
  matchId: string;
  joinCode: string;
  modeId: string;
  modeDisplayName: string;
  status: string;
  tickCount: number;
  maxTicks: number;
  playerCount: number;
  maxPlayers: number;
  pendingGateCount: number;
  nextGateExpiresAt: string | null;
  startingTier: number;
  llmStatus: LlmLayerStatus | null;
}

export interface LlmLayerStatus {
  providerEnabled: boolean;
  provider: string;
  model: string;
  turnAgentReady: boolean;
  workflowReady: boolean;
  rivalTierGate: number;
  eligibleRivalCount: number;
  anyRivalEligible: boolean;
  maxTurnCallsPerTick: number;
  turnCallsUsedThisTick: number;
  maxAdvisorCallsPerTick: number;
  advisorCallsUsedThisTick: number;
  lastRivalRunner: string | null;
  statusMessage: string;
}

export interface PlayerSlot {
  playerId: string;
  playerName: string;
  civilizationId: string;
  civilizationName: string;
  isReady: boolean;
  isHost: boolean;
}

export interface DecisionOption {
  id: string;
  label: string;
  description: string;
  impactHint: string;
}

export interface DecisionGate {
  gateId: string;
  civilizationId: string;
  civilizationName: string;
  title: string;
  description: string;
  type: string;
  defaultOptionId: string;
  expiresAt: string;
  options: DecisionOption[];
  contextRegionId?: string | null;
  contextRegionName?: string | null;
  contextFactionName?: string | null;
  queueIndex?: number;
  queueTotal?: number;
}

export interface Civilization {
  id: string;
  name: string;
  tier: number;
  averageStability: number;
  politicalStability: number;
  economicStability: number;
  technologicalStability: number;
  policyLabel: string;
  techCount: number;
  lastAction: string | null;
}

export interface Region {
  id: string;
  name: string;
  controllingCivilizationId: string | null;
  controllingCivilizationName: string | null;
  population: number;
  infrastructure: number;
  resources: number;
  sourceState: string | null;
  dataYear: number | null;
  gdpPerCapita: number;
  unemploymentRate: number;
  povertyRate: number;
  economicHealth: number;
  crimePressure: number;
}

export interface MatchSummary {
  matchId: string;
  joinCode: string;
  modeId: string;
  modeDisplayName: string;
  status: string;
  tickCount: number;
  maxTicks: number;
  minPlayers: number;
  maxPlayers: number;
  readyCount: number;
  hostPlayerId: string | null;
  nextTickAt: string;
  simulatedNow: string;
  isTickDue: boolean;
  victoryTier: number;
  victoryStabilityMin: number;
  startingTier: number;
  players: PlayerSlot[];
  civilizations: Civilization[];
  regions: Region[];
  pendingGates: DecisionGate[];
  awaySummary: string | null;
  awaySummaryStructured: AwaySummaryStructured | null;
  resultsSummary: string | null;
  results: MatchResultEntry[];
  tickLogs: TickLogEntry[];
  llmStatus: LlmLayerStatus | null;
}

export interface AwaySummaryStructured {
  headline: string;
  bullets: string[];
  missedGates: string[];
}

export interface MatchResultEntry {
  rank: number;
  civilizationId: string;
  civilizationName: string;
  tier: number;
  stability: number;
  techCount: number;
  outcome: string;
  outcomeReason: string;
}

export interface TickLogEntry {
  tick: number;
  lines: string[];
}

export interface HexTile {
  q: number;
  r: number;
  biome: string;
  resourceYield: number;
  controllingCivilizationId: string | null;
  isCapital: boolean;
}

export interface HexMap {
  width: number;
  height: number;
  seed: number;
  tiles: HexTile[];
  capitalHexByCivilizationId: Record<string, string>;
}

export interface ClaimTerritoryResponse {
  success: boolean;
  message: string;
  hexKey: string | null;
}

export interface AdvisorOptionGuidance {
  optionId: string;
  label: string;
  stance: 'recommended' | 'caution' | 'neutral' | string;
  note: string;
}

export interface AdvisorGateFocus {
  gateId: string;
  title: string;
  gateType: string;
  rationale: string;
  recommendedOptionId: string;
  recommendedOptionLabel: string;
  options: AdvisorOptionGuidance[];
}

export interface AdvisorBriefing {
  available: boolean;
  briefing: string;
  source: string;
  headline: string;
  highlights: string[];
  recommendedTechId: string | null;
  recommendedTechName: string | null;
  gateFocus: AdvisorGateFocus | null;
}

function normalizeGateFocus(raw: unknown): AdvisorGateFocus | null {
  if (!raw || typeof raw !== 'object') return null;
  const g = raw as Record<string, unknown>;
  const optionsRaw = g.options ?? g.Options;
  const options = Array.isArray(optionsRaw)
    ? optionsRaw.map((item) => {
        const o = item as Record<string, unknown>;
        return {
          optionId: String(o.optionId ?? o.OptionId ?? ''),
          label: String(o.label ?? o.Label ?? ''),
          stance: String(o.stance ?? o.Stance ?? 'neutral'),
          note: String(o.note ?? o.Note ?? ''),
        };
      })
    : [];

  const gateId = String(g.gateId ?? g.GateId ?? '');
  if (!gateId) return null;

  return {
    gateId,
    title: String(g.title ?? g.Title ?? ''),
    gateType: String(g.gateType ?? g.GateType ?? ''),
    rationale: String(g.rationale ?? g.Rationale ?? ''),
    recommendedOptionId: String(g.recommendedOptionId ?? g.RecommendedOptionId ?? ''),
    recommendedOptionLabel: String(g.recommendedOptionLabel ?? g.RecommendedOptionLabel ?? ''),
    options,
  };
}

function normalizeAdvisorBriefing(raw: Partial<AdvisorBriefing> & Record<string, unknown>): AdvisorBriefing {
  const briefing = String(raw.briefing ?? raw.Briefing ?? '');
  const headline = String(raw.headline ?? raw.Headline ?? '').trim();
  const rawHighlights = raw.highlights ?? raw.Highlights;
  const highlights = Array.isArray(rawHighlights)
    ? rawHighlights.map((line) => String(line))
    : [];

  return {
    available: Boolean(raw.available ?? raw.Available),
    briefing,
    source: String(raw.source ?? raw.Source ?? 'system'),
    headline: headline || briefing.split(/[.!?]/)[0]?.trim() || 'Strategic assessment',
    highlights,
    recommendedTechId: (raw.recommendedTechId ?? raw.RecommendedTechId ?? null) as string | null,
    recommendedTechName: (raw.recommendedTechName ?? raw.RecommendedTechName ?? null) as string | null,
    gateFocus: normalizeGateFocus(raw.gateFocus ?? raw.GateFocus),
  };
}

export interface TechTreeNode {
  id: string;
  name: string;
  tier: number;
  branch: string;
  role: string;
  prerequisites: string[];
  riskLevel: number;
  isForbidden: boolean;
  status: 'researched' | 'available' | 'locked' | 'blocked';
}

export interface CivDashboard {
  civilizationId: string;
  presetId: string;
  researchStance: string;
  riskTolerance: string;
  branchWeights: Record<string, number>;
  recommendedTech: { id: string; name: string; tier: number; branch: string; score: number } | null;
  researchedTech: { id: string; name: string; tier: number; branch: string }[];
  availableTech: { id: string; name: string; tier: number; branch: string }[];
  techTree: TechTreeNode[];
  researchSlotsPerTurn: number;
  crime: {
    averageCrimePressure: number;
    averageViolentCrimeRate: number;
    averagePovertyRate: number;
    cybersecurityMitigationActive: boolean;
    regions: { regionName: string; sourceState: string; crimePressure: number }[];
  } | null;
}

export const POLICY_PRESETS = [
  { id: 'balanced', label: 'Balanced' },
  { id: 'tech-rush', label: 'Tech Rush' },
  { id: 'stability-first', label: 'Stability First' },
  { id: 'expansionist', label: 'Expansionist' },
  { id: 'diplomatic', label: 'Diplomatic' },
] as const;

export interface JoinResponse {
  matchId: string;
  playerId: string;
  playerName: string;
  civilizationId: string;
  civilizationName: string;
}

export interface CreateMatchResponse {
  matchId: string;
  joinCode: string;
  modeDisplayName: string;
}

export interface PlayerSession {
  playerId: string;
  playerName: string;
  civilizationId: string;
  civilizationName: string;
}

const SESSION_KEY = 'tts-player-session';

export function saveSession(matchId: string, session: PlayerSession) {
  localStorage.setItem(`${SESSION_KEY}:${matchId}`, JSON.stringify(session));
}

export function loadSession(matchId: string): PlayerSession | null {
  const raw = localStorage.getItem(`${SESSION_KEY}:${matchId}`);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as PlayerSession;
  } catch {
    return null;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(body.error ?? `Request failed (${res.status})`);
  }
  return res.json() as Promise<T>;
}

export const api = {
  listMatches: () => request<MatchListItem[]>('/api/matches'),

  createMatch: (modeId: string, withDemoGate: boolean) =>
    request<CreateMatchResponse>('/api/matches', {
      method: 'POST',
      body: JSON.stringify({ modeId, withDemoGate }),
    }),

  joinByCode: (joinCode: string, playerName: string) =>
    request<JoinResponse>(`/api/matches/join/${encodeURIComponent(joinCode)}`, {
      method: 'POST',
      body: JSON.stringify({ playerName }),
    }),

  joinMatch: (matchId: string, playerName: string) =>
    request<JoinResponse>(`/api/matches/${encodeURIComponent(matchId)}/join`, {
      method: 'POST',
      body: JSON.stringify({ playerName }),
    }),

  getMatch: (matchId: string) =>
    request<MatchSummary>(`/api/matches/${encodeURIComponent(matchId)}`),

  resolveDecision: (
    matchId: string,
    civilizationId: string,
    gateId: string,
    optionId: string,
  ) =>
    request<{ success: boolean; message: string }>(`/api/matches/${encodeURIComponent(matchId)}/decisions`, {
      method: 'POST',
      body: JSON.stringify({ civilizationId, gateId, optionId }),
    }),

  advanceTick: (matchId: string) =>
    request<unknown>(`/api/matches/${encodeURIComponent(matchId)}/tick`, { method: 'POST' }),

  getCivDashboard: (matchId: string, civilizationId: string) =>
    request<CivDashboard>(`/api/matches/${encodeURIComponent(matchId)}/civs/${encodeURIComponent(civilizationId)}`),

  getAdvisorBriefing: async (matchId: string, civilizationId: string) =>
    normalizeAdvisorBriefing(
      await request<Partial<AdvisorBriefing> & Record<string, unknown>>(
        `/api/matches/${encodeURIComponent(matchId)}/civs/${encodeURIComponent(civilizationId)}/advisor`,
      ),
    ),

  updatePolicy: (matchId: string, civilizationId: string, presetId: string) =>
    request<CivDashboard>(`/api/matches/${encodeURIComponent(matchId)}/civs/${encodeURIComponent(civilizationId)}/policy`, {
      method: 'PUT',
      body: JSON.stringify({ presetId }),
    }),

  setReady: (matchId: string, playerId: string, ready: boolean) =>
    request<{ playerId: string; ready: boolean }>(`/api/matches/${encodeURIComponent(matchId)}/ready`, {
      method: 'POST',
      body: JSON.stringify({ playerId, ready }),
    }),

  startMatch: (matchId: string, playerId: string) =>
    request<{ started: boolean }>(`/api/matches/${encodeURIComponent(matchId)}/start`, {
      method: 'POST',
      body: JSON.stringify({ playerId }),
    }),

  getHexMap: (matchId: string) =>
    request<HexMap>(`/api/matches/${encodeURIComponent(matchId)}/map`),

  claimTerritory: (matchId: string, civilizationId: string, q: number, r: number) =>
    request<ClaimTerritoryResponse>(`/api/matches/${encodeURIComponent(matchId)}/territory/claim`, {
      method: 'POST',
      body: JSON.stringify({ civilizationId, q, r }),
    }),
};
