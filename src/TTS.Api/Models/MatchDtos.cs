namespace TTS.Api.Models;

public sealed class LlmLayerStatusDto
{
    public bool ProviderEnabled { get; init; }
    public required string Provider { get; init; }
    public required string Model { get; init; }
    public bool TurnAgentReady { get; init; }
    public bool WorkflowReady { get; init; }
    public int RivalTierGate { get; init; }
    public int EligibleRivalCount { get; init; }
    public bool AnyRivalEligible { get; init; }
    public int MaxTurnCallsPerTick { get; init; }
    public int TurnCallsUsedThisTick { get; init; }
    public int MaxAdvisorCallsPerTick { get; init; }
    public int AdvisorCallsUsedThisTick { get; init; }
    public string? LastRivalRunner { get; init; }
    public required string StatusMessage { get; init; }
}

public sealed class MatchListItemDto
{
    public required string MatchId { get; init; }
    public required string JoinCode { get; init; }
    public required string ModeId { get; init; }
    public required string ModeDisplayName { get; init; }
    public required string Status { get; init; }
    public int TickCount { get; init; }
    public int MaxTicks { get; init; }
    public int PlayerCount { get; init; }
    public int MaxPlayers { get; init; }
    public int PendingGateCount { get; init; }
    public DateTimeOffset? NextGateExpiresAt { get; init; }
    public int StartingTier { get; init; }
    public LlmLayerStatusDto? LlmStatus { get; init; }
}

public sealed class CreateMatchRequestDto
{
    public string ModeId { get; init; } = "sprint-8h";
    public bool WithDemoGate { get; init; }
    public int? Seed { get; init; }
}

public sealed class CreateMatchResponseDto
{
    public required string MatchId { get; init; }
    public required string JoinCode { get; init; }
    public required string ModeDisplayName { get; init; }
}

public sealed class JoinMatchRequestDto
{
    public string PlayerName { get; init; } = "Governor";
}

public sealed class JoinMatchResponseDto
{
    public required string MatchId { get; init; }
    public required string PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string CivilizationId { get; init; }
    public required string CivilizationName { get; init; }
}

public sealed class CivilizationDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Tier { get; init; }
    public double AverageStability { get; init; }
    public double PoliticalStability { get; init; }
    public double EconomicStability { get; init; }
    public double TechnologicalStability { get; init; }
    public required string PolicyLabel { get; init; }
    public int TechCount { get; init; }
    public string? LastAction { get; init; }
}

public sealed class RegionDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? ControllingCivilizationId { get; init; }
    public string? ControllingCivilizationName { get; init; }
    public long Population { get; init; }
    public double Infrastructure { get; init; }
    public double Resources { get; init; }
    public string? SourceState { get; init; }
    public int? DataYear { get; init; }
    public double GdpPerCapita { get; init; }
    public double UnemploymentRate { get; init; }
    public double PovertyRate { get; init; }
    public double EconomicHealth { get; init; }
    public double CrimePressure { get; init; }
}

public sealed class MatchSummaryDto
{
    public required string MatchId { get; init; }
    public required string JoinCode { get; init; }
    public required string ModeId { get; init; }
    public required string ModeDisplayName { get; init; }
    public required string Status { get; init; }
    public int TickCount { get; init; }
    public int MaxTicks { get; init; }
    public int MinPlayers { get; init; }
    public int MaxPlayers { get; init; }
    public int ReadyCount { get; init; }
    public string? HostPlayerId { get; init; }
    public DateTimeOffset NextTickAt { get; init; }
    public DateTimeOffset SimulatedNow { get; init; }
    public bool IsTickDue { get; init; }
    public int VictoryTier { get; init; }
    public double VictoryStabilityMin { get; init; }
    public int StartingTier { get; init; }
    public required IReadOnlyList<PlayerSlotDto> Players { get; init; }
    public required IReadOnlyList<CivilizationDto> Civilizations { get; init; }
    public required IReadOnlyList<RegionDto> Regions { get; init; }
    public required IReadOnlyList<DecisionGateDto> PendingGates { get; init; }
    public string? AwaySummary { get; init; }
    public AwaySummaryStructuredDto? AwaySummaryStructured { get; init; }
    public string? ResultsSummary { get; init; }
    public required IReadOnlyList<MatchResultEntryDto> Results { get; init; }
    public required IReadOnlyList<TickLogEntryDto> TickLogs { get; init; }
    public LlmLayerStatusDto? LlmStatus { get; init; }
}

public sealed class MatchResultEntryDto
{
    public int Rank { get; init; }
    public required string CivilizationId { get; init; }
    public required string CivilizationName { get; init; }
    public int Tier { get; init; }
    public double Stability { get; init; }
    public int TechCount { get; init; }
    public required string Outcome { get; init; }
    public required string OutcomeReason { get; init; }
}

public sealed class AwaySummaryStructuredDto
{
    public required string Headline { get; init; }
    public required IReadOnlyList<string> Bullets { get; init; }
    public required IReadOnlyList<string> MissedGates { get; init; }
}

public sealed class TickLogEntryDto
{
    public int Tick { get; init; }
    public required IReadOnlyList<string> Lines { get; init; }
}

public sealed class PlayerSlotDto
{
    public required string PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string CivilizationId { get; init; }
    public required string CivilizationName { get; init; }
    public bool IsReady { get; init; }
    public bool IsHost { get; init; }
}

public sealed class DecisionGateDto
{
    public required string GateId { get; init; }
    public required string CivilizationId { get; init; }
    public required string CivilizationName { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Type { get; init; }
    public required string DefaultOptionId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public required IReadOnlyList<DecisionOptionDto> Options { get; init; }
}

public sealed class DecisionOptionDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Description { get; init; }
    public required string ImpactHint { get; init; }
}

public sealed class ResolveDecisionRequestDto
{
    public required string CivilizationId { get; init; }
    public required string GateId { get; init; }
    public required string OptionId { get; init; }
}

public sealed class ResolveDecisionResponseDto
{
    public bool Success { get; init; }
    public required string Message { get; init; }
    public string? OptionId { get; init; }
}

public sealed class MatchRegistryEntry
{
    public required string MatchId { get; init; }
    public required string JoinCode { get; init; }
    public required string ModeId { get; init; }
    public required string ModeDisplayName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int WorldSeed { get; set; }
    public string? HostPlayerId { get; set; }
    public List<MatchPlayerEntry> Players { get; set; } = [];
}

public sealed class MatchPlayerEntry
{
    public required string PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string CivilizationId { get; init; }
    public required string CivilizationName { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public bool IsReady { get; set; }
}

public sealed class MatchRegistryDocument
{
    public List<MatchRegistryEntry> Matches { get; set; } = [];
}

public sealed class UpdatePolicyRequestDto
{
    public required string PresetId { get; init; }
}

public sealed class ReadyRequestDto
{
    public required string PlayerId { get; init; }
    public bool Ready { get; init; } = true;
}

public sealed class StartMatchRequestDto
{
    public required string PlayerId { get; init; }
}

public sealed class CivDashboardDto
{
    public required string CivilizationId { get; init; }
    public required string PresetId { get; init; }
    public required string ResearchStance { get; init; }
    public required string RiskTolerance { get; init; }
    public required IReadOnlyDictionary<string, double> BranchWeights { get; init; }
    public RecommendedTechDto? RecommendedTech { get; init; }
    public required IReadOnlyList<TechEntryDto> ResearchedTech { get; init; }
    public required IReadOnlyList<TechEntryDto> AvailableTech { get; init; }
    public CrimePerspectiveDto? Crime { get; init; }
    public required IReadOnlyList<TechTreeNodeDto> TechTree { get; init; }
    public int ResearchSlotsPerTurn { get; init; }
}

public sealed class AdvisorOptionGuidanceDto
{
    public required string OptionId { get; init; }
    public required string Label { get; init; }
    public required string Stance { get; init; }
    public required string Note { get; init; }
}

public sealed class AdvisorGateFocusDto
{
    public required string GateId { get; init; }
    public required string Title { get; init; }
    public required string GateType { get; init; }
    public required string Rationale { get; init; }
    public required string RecommendedOptionId { get; init; }
    public required string RecommendedOptionLabel { get; init; }
    public required IReadOnlyList<AdvisorOptionGuidanceDto> Options { get; init; }
}

public sealed class AdvisorBriefingDto
{
    public bool Available { get; init; }
    public required string Briefing { get; init; }
    public required string Source { get; init; }
    public required string Headline { get; init; }
    public required IReadOnlyList<string> Highlights { get; init; }
    public string? RecommendedTechId { get; init; }
    public string? RecommendedTechName { get; init; }
    public AdvisorGateFocusDto? GateFocus { get; init; }
}

public sealed class TechTreeNodeDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Tier { get; init; }
    public required string Branch { get; init; }
    public required string Role { get; init; }
    public required IReadOnlyList<string> Prerequisites { get; init; }
    public int RiskLevel { get; init; }
    public bool IsForbidden { get; init; }
    public required string Status { get; init; }
}

public sealed class RecommendedTechDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Tier { get; init; }
    public required string Branch { get; init; }
    public double Score { get; init; }
}

public sealed class TechEntryDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Tier { get; init; }
    public required string Branch { get; init; }
}

public sealed class CrimePerspectiveDto
{
    public double AverageCrimePressure { get; init; }
    public double AverageViolentCrimeRate { get; init; }
    public double AveragePovertyRate { get; init; }
    public bool CybersecurityMitigationActive { get; init; }
    public required IReadOnlyList<RegionCrimeDto> Regions { get; init; }
}

public sealed class RegionCrimeDto
{
    public required string RegionName { get; init; }
    public required string SourceState { get; init; }
    public double CrimePressure { get; init; }
}

public sealed class HexTileDto
{
    public int Q { get; init; }
    public int R { get; init; }
    public required string Biome { get; init; }
    public double ResourceYield { get; init; }
    public string? ControllingCivilizationId { get; init; }
    public bool IsCapital { get; init; }
}

public sealed class HexMapDto
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Seed { get; init; }
    public required IReadOnlyList<HexTileDto> Tiles { get; init; }
    public required IReadOnlyDictionary<string, string> CapitalHexByCivilizationId { get; init; }
}

public sealed class ClaimTerritoryRequestDto
{
    public required string CivilizationId { get; init; }
    public int Q { get; init; }
    public int R { get; init; }
}

public sealed class ClaimTerritoryResponseDto
{
    public bool Success { get; init; }
    public required string Message { get; init; }
    public string? HexKey { get; init; }
}
