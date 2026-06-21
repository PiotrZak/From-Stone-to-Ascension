namespace TTS.Core.Simulation;

using TTS.Core.Models;

public readonly record struct MatchStatusInfo(
    string MatchId,
    string ModeDisplayName,
    MatchStatus Status,
    int TickCount,
    int MaxTicks,
    DateTimeOffset StartedAt,
    DateTimeOffset LastTickAt,
    DateTimeOffset NextTickAt,
    DateTimeOffset SimulatedNow,
    bool IsTickDue,
    IReadOnlyList<PendingGateInfo> PendingGates);

public readonly record struct PendingGateInfo(
    string CivilizationId,
    string CivilizationName,
    string GateId,
    GateType Type,
    string Title,
    DateTimeOffset ExpiresAt,
    string DefaultOptionId);

public enum MatchTickOutcome
{
    NotDue,
    MatchEnded,
    Completed
}

public readonly record struct MatchTickResult(
    MatchTickOutcome Outcome,
    TurnResult? Turn = null,
    string? Message = null);
