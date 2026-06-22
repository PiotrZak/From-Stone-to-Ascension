namespace TTS.Core.Simulation;

using TTS.Core.Models;

/// <summary>Per-civ state at turn start — used for gate triggers and away summaries.</summary>
public readonly record struct CivTurnStartSnapshot(
    TechTier Tier,
    double AverageStability,
    int ResearchedCount,
    IReadOnlyList<string> ResearchedTechnologyIds);

public sealed class TurnSnapshot
{
    public int Turn { get; init; }
    public DateTimeOffset SimulatedAt { get; init; }
    public Dictionary<string, CivTurnStartSnapshot> CivilizationsAtStart { get; init; } = new();
    public List<GateResolutionRecord> GateResolutions { get; } = [];
    public List<string> NewEvents { get; } = [];
    public Dictionary<string, List<string>> ResearchedThisTurn { get; } = new();
    public Dictionary<string, TierChangeRecord> TierChanges { get; } = new();
    public List<TurnResearchDecisionSnapshot> ResearchDecisions { get; } = [];
}

public readonly record struct TurnResearchDecisionSnapshot(
    string CivilizationId,
    string CivilizationName,
    string Runner,
    bool Researched,
    string Message);

public readonly record struct GateResolutionRecord(
    string CivilizationId,
    string GateId,
    GateType GateType,
    string Title,
    string OptionId,
    bool WasAutoResolved);

public readonly record struct TierChangeRecord(TechTier From, TechTier To);
