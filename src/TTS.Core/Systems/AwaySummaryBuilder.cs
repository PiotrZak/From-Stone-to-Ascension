namespace TTS.Core.Systems;

using System.Text;
using TTS.Core.Models;
using TTS.Core.Simulation;

public sealed class AwaySummaryBuilder
{
    public AwaySummary Build(WorldState world, IReadOnlyList<TurnSnapshot> history, int fromTurn, int toTurn)
    {
        var ticks = history
            .Where(s => s.Turn >= fromTurn && s.Turn <= toTurn)
            .OrderBy(s => s.Turn)
            .ToList();

        var elapsed = ticks.Count >= 2
            ? ticks[^1].SimulatedAt - ticks[0].SimulatedAt
            : TimeSpan.Zero;

        var pending = world.Civilizations
            .SelectMany(c => c.PendingDecisions.Where(g => !g.IsResolved))
            .ToList();

        return new AwaySummary(fromTurn, toTurn, elapsed, ticks, pending, world.Match?.Config.DisplayName);
    }
}

public sealed class AwaySummary
{
    public int FromTurn { get; }
    public int ToTurn { get; }
    public TimeSpan Elapsed { get; }
    public IReadOnlyList<TurnSnapshot> Ticks { get; }
    public IReadOnlyList<DecisionGate> PendingGates { get; }
    public string? MatchDisplayName { get; }

    public AwaySummary(
        int fromTurn,
        int toTurn,
        TimeSpan elapsed,
        IReadOnlyList<TurnSnapshot> ticks,
        IReadOnlyList<DecisionGate> pendingGates,
        string? matchDisplayName = null)
    {
        FromTurn = fromTurn;
        ToTurn = toTurn;
        Elapsed = elapsed;
        Ticks = ticks;
        PendingGates = pendingGates;
        MatchDisplayName = matchDisplayName;
    }

    public string Format(WorldState world)
    {
        var sb = new StringBuilder();
        var matchLine = MatchDisplayName is not null ? $"Match: {MatchDisplayName} — " : "";
        sb.AppendLine($"{matchLine}While you were away ({Ticks.Count} ticks, {FormatElapsed(Elapsed)})");
        sb.AppendLine();

        foreach (var tick in Ticks)
        {
            foreach (var (civId, tierChange) in tick.TierChanges)
            {
                var civ = world.Civilizations.First(c => c.Id == civId);
                sb.AppendLine($"  TTS    {(int)tierChange.From} → {(int)tierChange.To}  ({civ.Name})");
            }

            foreach (var (civId, techs) in tick.ResearchedThisTurn)
            {
                if (techs.Count == 0)
                    continue;

                var civ = world.Civilizations.First(c => c.Id == civId);
                sb.AppendLine($"  Tech   {civ.Name}: {string.Join(", ", techs)}");
            }

            foreach (var resolution in tick.GateResolutions)
            {
                var auto = resolution.WasAutoResolved ? " (auto)" : "";
                sb.AppendLine($"  Gate   {resolution.Title}: {resolution.OptionId}{auto}");
            }

            foreach (var eventName in tick.NewEvents)
                sb.AppendLine($"  Event  {eventName}");
        }

        if (PendingGates.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"  Pending: {PendingGates.Count} decision(s)");
            foreach (var gate in PendingGates)
                sb.AppendLine($"    — {gate.Title}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"~{elapsed.TotalHours:F0}h";
        if (elapsed.TotalMinutes >= 1)
            return $"~{elapsed.TotalMinutes:F0}m";
        return $"{elapsed.TotalSeconds:F0}s";
    }
}
