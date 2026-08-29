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

export interface Vec3 {
  x: number;
  y: number;
  z: number;
}

export interface HexTile {
  id: string;
  biome: string;
  resourceYield: number;
  controllingCivilizationId: string | null;
  isCapital: boolean;
  worldRegionId?: string | null;
  worldRegionName?: string | null;
  centerX: number;
  centerY: number;
  centerZ: number;
  normalX: number;
  normalY: number;
  normalZ: number;
  polygonVertices: Vec3[];
  neighbourIds: string[];
  isPentagon: boolean;
}

export interface HexMap {
  planetRadius: number;
  frequency: number;
  seed: number;
  tiles: HexTile[];
  capitalTileByCivilizationId: Record<string, string>;
}

export interface TradeHubRouteSummary {
  counterpartHubId: string;
  counterpartName: string;
  direction: string;
  shipmentCount: number;
  totalValueUsd: number;
}

export interface TradeHubStats {
  hubId: string;
  outboundShipments: number;
  inboundShipments: number;
  outboundValueUsd: number;
  inboundValueUsd: number;
  topCommodities: string[];
  topRoutes: TradeHubRouteSummary[];
}

export interface TradeCountryStats {
  countryId: string;
  displayName: string;
  shipmentCount: number;
  totalValueUsd: number;
  topCommodity: string;
}

export interface TradeHub {
  id: string;
  name: string;
  countryId: string;
  latDeg: number;
  lonDeg: number;
  tileId: string;
  centerX: number;
  centerY: number;
  centerZ: number;
  isPort: boolean;
}

export interface TradeFlow {
  fromHubId: string;
  toHubId: string;
  departurePort: string;
  arrivalPort: string;
  importCountry: string;
  shipmentCount: number;
  totalValueUsd: number;
}

export interface TradeGlobe {
  map: HexMap;
  countries: TradeCountryStats[];
  hubs: TradeHub[];
  flows: TradeFlow[];
  tradeCountryByTileId: Record<string, string>;
  maxCountryValueUsd: number;
  commodities: string[];
  importCountries: string[];
  transportModes: string[];
  activeCountryIds: string[];
  activeHubIds: string[];
  hubStats: Record<string, TradeHubStats>;
  appliedCommodity?: string | null;
  appliedImportCountry?: string | null;
  appliedTransportMode?: string | null;
  filteredShipmentCount: number;
}

function normalizeTradeGlobe(raw: Partial<TradeGlobe> & Record<string, unknown>): TradeGlobe {
  return {
    map: raw.map as TradeGlobe['map'],
    countries: Array.isArray(raw.countries) ? raw.countries : [],
    hubs: Array.isArray(raw.hubs) ? raw.hubs : [],
    flows: Array.isArray(raw.flows) ? raw.flows : [],
    tradeCountryByTileId:
      raw.tradeCountryByTileId && typeof raw.tradeCountryByTileId === 'object'
        ? (raw.tradeCountryByTileId as Record<string, string>)
        : {},
    maxCountryValueUsd: typeof raw.maxCountryValueUsd === 'number' ? raw.maxCountryValueUsd : 0,
    commodities: Array.isArray(raw.commodities) ? raw.commodities : [],
    importCountries: Array.isArray(raw.importCountries) ? raw.importCountries : [],
    transportModes: Array.isArray(raw.transportModes) ? raw.transportModes : [],
    activeCountryIds: Array.isArray(raw.activeCountryIds) ? raw.activeCountryIds : [],
    activeHubIds: Array.isArray(raw.activeHubIds) ? raw.activeHubIds : [],
    hubStats:
      raw.hubStats && typeof raw.hubStats === 'object'
        ? (raw.hubStats as Record<string, TradeHubStats>)
        : {},
    appliedCommodity: typeof raw.appliedCommodity === 'string' ? raw.appliedCommodity : null,
    appliedImportCountry:
      typeof raw.appliedImportCountry === 'string' ? raw.appliedImportCountry : null,
    appliedTransportMode:
      typeof raw.appliedTransportMode === 'string' ? raw.appliedTransportMode : null,
    filteredShipmentCount:
      typeof raw.filteredShipmentCount === 'number' ? raw.filteredShipmentCount : 0,
  };
}

export interface SatelliteBody {
  id: string;
  name: string;
  constellationId: string;
  altitudeFactor: number;
  inclinationRad: number;
  phaseRad: number;
  angularSpeed: number;
  role: string;
  operator: string;
  country: string;
  users: string;
  purpose: string;
  orbitClass: string;
  orbitType: string;
  perigeeKm: number;
  apogeeKm: number;
  inclinationDeg: number;
  periodMinutes: number;
  launchDate: string;
  launchSite: string;
  noradNumber: string;
}

export interface SatelliteGroundStation {
  id: string;
  name: string;
  region: string;
  constellationId: string;
  tileId: string;
  latDeg: number;
  lonDeg: number;
  centerX: number;
  centerY: number;
  centerZ: number;
  launchCount: number;
}

export interface SatelliteConstellation {
  id: string;
  name: string;
  domain: string;
  color: string;
  summary: string;
  satelliteCount: number;
  averageCoverage: number;
  groundStationCount: number;
}

export interface SatelliteGlobe {
  map: HexMap;
  constellations: SatelliteConstellation[];
  availableConstellations: SatelliteConstellation[];
  satellites: SatelliteBody[];
  groundStations: SatelliteGroundStation[];
  coverageByTileId: Record<string, number>;
  maxCoverage: number;
  orbitClasses: string[];
  countries: string[];
  totalSatelliteCount: number;
  visibleSatelliteCount: number;
  appliedPurposeGroupId?: string | null;
  appliedOrbitClass?: string | null;
  appliedCountry?: string | null;
}

function normalizeSatelliteGlobe(
  raw: Partial<SatelliteGlobe> & Record<string, unknown>,
): SatelliteGlobe {
  return {
    map: raw.map as SatelliteGlobe['map'],
    constellations: Array.isArray(raw.constellations) ? raw.constellations : [],
    availableConstellations: Array.isArray(raw.availableConstellations)
      ? raw.availableConstellations
      : Array.isArray(raw.constellations)
        ? raw.constellations
        : [],
    satellites: Array.isArray(raw.satellites) ? raw.satellites : [],
    groundStations: Array.isArray(raw.groundStations) ? raw.groundStations : [],
    coverageByTileId:
      raw.coverageByTileId && typeof raw.coverageByTileId === 'object'
        ? (raw.coverageByTileId as Record<string, number>)
        : {},
    maxCoverage: typeof raw.maxCoverage === 'number' ? raw.maxCoverage : 0,
    orbitClasses: Array.isArray(raw.orbitClasses) ? raw.orbitClasses : [],
    countries: Array.isArray(raw.countries) ? raw.countries : [],
    totalSatelliteCount:
      typeof raw.totalSatelliteCount === 'number' ? raw.totalSatelliteCount : 0,
    visibleSatelliteCount:
      typeof raw.visibleSatelliteCount === 'number' ? raw.visibleSatelliteCount : 0,
    appliedPurposeGroupId:
      typeof raw.appliedPurposeGroupId === 'string' ? raw.appliedPurposeGroupId : null,
    appliedOrbitClass: typeof raw.appliedOrbitClass === 'string' ? raw.appliedOrbitClass : null,
    appliedCountry: typeof raw.appliedCountry === 'string' ? raw.appliedCountry : null,
  };
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

  getTradeGlobe: (params?: {
    commodity?: string;
    importCountry?: string;
    transportMode?: string;
  }) => {
    const qs = new URLSearchParams();
    if (params?.commodity) qs.set('commodity', params.commodity);
    if (params?.importCountry) qs.set('importCountry', params.importCountry);
    if (params?.transportMode) qs.set('transportMode', params.transportMode);
    const query = qs.toString();
    return request<Partial<TradeGlobe> & Record<string, unknown>>(
      `/api/trade/globe${query ? `?${query}` : ''}`,
    ).then(normalizeTradeGlobe);
  },

  getSatelliteGlobe: (params?: {
    purpose?: string;
    orbitClass?: string;
    country?: string;
  }) => {
    const qs = new URLSearchParams();
    if (params?.purpose) qs.set('purpose', params.purpose);
    if (params?.orbitClass) qs.set('orbitClass', params.orbitClass);
    if (params?.country) qs.set('country', params.country);
    const query = qs.toString();
    return request<Partial<SatelliteGlobe> & Record<string, unknown>>(
      `/api/satellites/globe${query ? `?${query}` : ''}`,
    ).then(normalizeSatelliteGlobe);
  },

  claimTerritory: (matchId: string, civilizationId: string, tileId: string) =>
    request<ClaimTerritoryResponse>(`/api/matches/${encodeURIComponent(matchId)}/territory/claim`, {
      method: 'POST',
      body: JSON.stringify({ civilizationId, tileId }),
    }),
};
