namespace TTS.Contracts;

[GenerateSerializer]
public enum GrainTickOutcomeKind
{
    NotDue,
    MatchEnded,
    Completed
}

[GenerateSerializer]
public sealed record GrainTickResult(
    [property: Id(0)] GrainTickOutcomeKind Outcome,
    [property: Id(1)] int Turn,
    [property: Id(2)] string Message,
    [property: Id(3)] List<GrainCivSnapshot> Civilizations,
    [property: Id(4)] List<GrainGateSnapshot> ActiveGates);

[GenerateSerializer]
public sealed record GrainCivSnapshot(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] int Tier,
    [property: Id(3)] double Stability,
    [property: Id(4)] int TechCount);

[GenerateSerializer]
public sealed record GrainGateSnapshot(
    [property: Id(0)] string Id,
    [property: Id(1)] string Title,
    [property: Id(2)] string Type,
    [property: Id(3)] string DefaultOptionId);

[GenerateSerializer]
public sealed record GrainMatchStatus(
    [property: Id(0)] string MatchId,
    [property: Id(1)] string ModeDisplayName,
    [property: Id(2)] string Status,
    [property: Id(3)] int TickCount,
    [property: Id(4)] int MaxTicks,
    [property: Id(5)] DateTimeOffset NextTickAt,
    [property: Id(6)] DateTimeOffset SimulatedNow,
    [property: Id(7)] bool IsTickDue,
    [property: Id(8)] List<GrainPendingGate> PendingGates);

[GenerateSerializer]
public sealed record GrainPendingGate(
    [property: Id(0)] string CivilizationId,
    [property: Id(1)] string CivilizationName,
    [property: Id(2)] string GateId,
    [property: Id(3)] string Title,
    [property: Id(4)] string Type,
    [property: Id(5)] DateTimeOffset ExpiresAt,
    [property: Id(6)] string DefaultOptionId);

[GenerateSerializer]
public sealed record GrainDecisionOptionDetail(
    [property: Id(0)] string Id,
    [property: Id(1)] string Label,
    [property: Id(2)] string Description,
    [property: Id(3)] string ImpactHint);

[GenerateSerializer]
public sealed record GrainDecisionGateDetail(
    [property: Id(0)] string GateId,
    [property: Id(1)] string CivilizationId,
    [property: Id(2)] string CivilizationName,
    [property: Id(3)] string Title,
    [property: Id(4)] string Description,
    [property: Id(5)] string Type,
    [property: Id(6)] string DefaultOptionId,
    [property: Id(7)] DateTimeOffset ExpiresAt,
    [property: Id(8)] List<GrainDecisionOptionDetail> Options);

[GenerateSerializer]
public sealed record GrainDecisionResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] string Message,
    [property: Id(2)] string? OptionId);

[GenerateSerializer]
public sealed record GrainMatchResultEntry(
    [property: Id(0)] int Rank,
    [property: Id(1)] string CivilizationId,
    [property: Id(2)] string CivilizationName,
    [property: Id(3)] int Tier,
    [property: Id(4)] double Stability,
    [property: Id(5)] int TechCount,
    [property: Id(6)] string Outcome,
    [property: Id(7)] string OutcomeReason);

[GenerateSerializer]
public sealed record GrainAwaySummary(
    [property: Id(0)] string Headline,
    [property: Id(1)] List<string> Bullets,
    [property: Id(2)] List<string> MissedGates);

[GenerateSerializer]
public sealed record GrainTickLogEntry(
    [property: Id(0)] int Tick,
    [property: Id(1)] List<string> Lines);

[GenerateSerializer]
public sealed record GrainTechTreeNode(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] int Tier,
    [property: Id(3)] string Branch,
    [property: Id(4)] string Role,
    [property: Id(5)] List<string> Prerequisites,
    [property: Id(6)] int RiskLevel,
    [property: Id(7)] bool IsForbidden,
    [property: Id(8)] string Status);

[GenerateSerializer]
public sealed record GrainCivDashboard(
    [property: Id(0)] string CivilizationId,
    [property: Id(1)] string PresetId,
    [property: Id(2)] string ResearchStance,
    [property: Id(3)] string RiskTolerance,
    [property: Id(4)] Dictionary<string, double> BranchWeights,
    [property: Id(5)] GrainRecommendedTech? RecommendedTech,
    [property: Id(6)] List<GrainTechEntry> ResearchedTech,
    [property: Id(7)] List<GrainTechEntry> AvailableTech,
    [property: Id(8)] GrainCrimePerspective? Crime,
    [property: Id(9)] List<GrainTechTreeNode> TechTree,
    [property: Id(10)] int ResearchSlotsPerTurn);

[GenerateSerializer]
public sealed record GrainAdvisorOptionGuidance(
    [property: Id(0)] string OptionId,
    [property: Id(1)] string Label,
    [property: Id(2)] string Stance,
    [property: Id(3)] string Note);

[GenerateSerializer]
public sealed record GrainAdvisorGateFocus(
    [property: Id(0)] string GateId,
    [property: Id(1)] string Title,
    [property: Id(2)] string GateType,
    [property: Id(3)] string Rationale,
    [property: Id(4)] string RecommendedOptionId,
    [property: Id(5)] string RecommendedOptionLabel,
    [property: Id(6)] List<GrainAdvisorOptionGuidance> Options);

[GenerateSerializer]
public sealed record GrainAdvisorBriefing(
    [property: Id(0)] bool Available,
    [property: Id(1)] string Briefing,
    [property: Id(2)] string Source,
    [property: Id(3)] string Headline = "",
    [property: Id(4)] List<string>? Highlights = null,
    [property: Id(5)] string? RecommendedTechId = null,
    [property: Id(6)] string? RecommendedTechName = null,
    [property: Id(7)] GrainAdvisorGateFocus? GateFocus = null);

[GenerateSerializer]
public sealed record GrainRecommendedTech(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] int Tier,
    [property: Id(3)] string Branch,
    [property: Id(4)] double Score);

[GenerateSerializer]
public sealed record GrainTechEntry(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] int Tier,
    [property: Id(3)] string Branch);

[GenerateSerializer]
public sealed record GrainCrimePerspective(
    [property: Id(0)] double AverageCrimePressure,
    [property: Id(1)] double AverageViolentCrimeRate,
    [property: Id(2)] double AveragePovertyRate,
    [property: Id(3)] bool CybersecurityMitigationActive,
    [property: Id(4)] List<GrainRegionCrime> Regions);

[GenerateSerializer]
public sealed record GrainRegionCrime(
    [property: Id(0)] string RegionName,
    [property: Id(1)] string SourceState,
    [property: Id(2)] double CrimePressure);

[GenerateSerializer]
public sealed record GrainRegionDetail(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] string? ControllingCivilizationId,
    [property: Id(3)] string? ControllingCivilizationName,
    [property: Id(4)] long Population,
    [property: Id(5)] double Infrastructure,
    [property: Id(6)] double Resources,
    [property: Id(7)] string? SourceState,
    [property: Id(8)] int? DataYear,
    [property: Id(9)] double GdpPerCapita,
    [property: Id(10)] double UnemploymentRate,
    [property: Id(11)] double PovertyRate,
    [property: Id(12)] double EconomicHealth,
    [property: Id(13)] double CrimePressure);

[GenerateSerializer]
public sealed record GrainLlmLayerStatus(
    [property: Id(0)] bool ProviderEnabled,
    [property: Id(1)] string Provider,
    [property: Id(2)] string Model,
    [property: Id(3)] bool TurnAgentReady,
    [property: Id(4)] bool WorkflowReady,
    [property: Id(5)] int RivalTierGate,
    [property: Id(6)] int EligibleRivalCount,
    [property: Id(7)] bool AnyRivalEligible,
    [property: Id(8)] int MaxTurnCallsPerTick,
    [property: Id(9)] int TurnCallsUsedThisTick,
    [property: Id(10)] int MaxAdvisorCallsPerTick,
    [property: Id(11)] int AdvisorCallsUsedThisTick,
    [property: Id(12)] string? LastRivalRunner,
    [property: Id(13)] string StatusMessage);

[GenerateSerializer]
public sealed record GrainCivDetail(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] int Tier,
    [property: Id(3)] double AverageStability,
    [property: Id(4)] double PoliticalStability,
    [property: Id(5)] double EconomicStability,
    [property: Id(6)] double TechnologicalStability,
    [property: Id(7)] string PolicyLabel,
    [property: Id(8)] int TechCount,
    [property: Id(9)] string? LastAction);

[GenerateSerializer]
public sealed record GrainHexTile(
    [property: Id(0)] int Q,
    [property: Id(1)] int R,
    [property: Id(2)] string Biome,
    [property: Id(3)] double ResourceYield,
    [property: Id(4)] string? ControllingCivilizationId,
    [property: Id(5)] bool IsCapital);

[GenerateSerializer]
public sealed record GrainHexMap(
    [property: Id(0)] int Width,
    [property: Id(1)] int Height,
    [property: Id(2)] int Seed,
    [property: Id(3)] List<GrainHexTile> Tiles,
    [property: Id(4)] Dictionary<string, string> CapitalHexByCivilizationId);

[GenerateSerializer]
public sealed record GrainTerritoryClaimResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] string Message,
    [property: Id(2)] string? HexKey);
