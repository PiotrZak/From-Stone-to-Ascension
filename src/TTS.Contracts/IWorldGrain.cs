using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Contracts;

public interface IWorldGrain : IGrainWithStringKey
{
    Task InitializeMatchAsync(string modeId, bool withDemoGate = false);
    Task<GrainMatchStatus> GetStatusAsync();
    Task<GrainTickResult> AdvanceTickIfDueAsync();
    Task<GrainDecisionResult> ResolveDecisionAsync(string civilizationId, string gateId, string optionId);
    Task<string> GetAwaySummaryAsync(int fromTurn, int toTurn);
    Task<IReadOnlyList<GrainDecisionGateDetail>> GetPendingGatesAsync(string? civilizationId = null);
    Task<IReadOnlyList<GrainCivDetail>> GetCivilizationsAsync();
    Task<IReadOnlyList<GrainMatchResultEntry>> GetMatchResultsAsync();
    Task<IReadOnlyList<GrainTickLogEntry>> GetMatchLogAsync();
    Task<GrainCivDashboard> GetCivDashboardAsync(string civilizationId);
    Task UpdatePolicyAsync(string civilizationId, string presetId);
    Task StartMatchAsync();
}

public static class GrainMapping
{
    public static GrainMatchStatus ToGrain(MatchStatusInfo status) => new(
        status.MatchId,
        status.ModeDisplayName,
        status.Status.ToString(),
        status.TickCount,
        status.MaxTicks,
        status.NextTickAt,
        status.SimulatedNow,
        status.IsTickDue,
        status.PendingGates.Select(g => new GrainPendingGate(
            g.CivilizationId,
            g.CivilizationName, g.GateId, g.Title, g.Type.ToString(), g.ExpiresAt, g.DefaultOptionId)).ToList());

    public static GrainTickResult ToGrain(MatchTickResult result) => result.Outcome switch
    {
        MatchTickOutcome.NotDue => new(GrainTickOutcomeKind.NotDue, 0, result.Message ?? "", [], []),
        MatchTickOutcome.MatchEnded => new(GrainTickOutcomeKind.MatchEnded, 0, result.Message ?? "", [], []),
        MatchTickOutcome.Completed when result.Turn is { } turn => new(
            GrainTickOutcomeKind.Completed,
            turn.Turn,
            "Tick completed.",
            turn.Outcomes.Select(o => new GrainCivSnapshot(
                o.Civilization.Id,
                o.Civilization.Name,
                (int)o.Civilization.CurrentTier,
                o.Civilization.AverageStability,
                o.Civilization.ResearchedTechnologyIds.Count)).ToList(),
            turn.ActiveGates.Select(g => new GrainGateSnapshot(
                g.Id, g.Title, g.Type.ToString(), g.DefaultOptionId)).ToList()),
        _ => new(GrainTickOutcomeKind.NotDue, 0, "Unknown result.", [], [])
    };

    public static GrainDecisionResult ToGrain(GateResolutionResult result) =>
        new(result.Success, result.Message, result.OptionId);
}
