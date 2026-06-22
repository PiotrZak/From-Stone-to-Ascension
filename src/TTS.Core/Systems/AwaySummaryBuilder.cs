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

public sealed record AwaySummaryStructured(
    string Headline,
    IReadOnlyList<string> Bullets,
    IReadOnlyList<string> MissedGates);

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

    public AwaySummaryStructured ToStructured(WorldState world) => BuildStructured(world);

    public string Format(WorldState world) => FormatStructured(BuildStructured(world));

    private AwaySummaryStructured BuildStructured(WorldState world)
    {
        var bullets = new List<string>();
        var missedGates = new List<string>();
        string? headline = null;

        foreach (var tick in Ticks)
        {
            foreach (var (civId, tierChange) in tick.TierChanges)
            {
                var civ = world.Civilizations.First(c => c.Id == civId);
                var line = $"{civ.Name} reached TTS {(int)tierChange.To}";
                bullets.Add(line);
                headline ??= line;
            }

            foreach (var (civId, techs) in tick.ResearchedThisTurn)
            {
                if (techs.Count == 0)
                    continue;

                var civ = world.Civilizations.First(c => c.Id == civId);
                bullets.Add($"{civ.Name} researched {string.Join(", ", techs)}");
            }

            foreach (var resolution in tick.GateResolutions)
            {
                var civ = world.Civilizations.FirstOrDefault(c => c.Id == resolution.CivilizationId);
                var civName = civ?.Name ?? resolution.CivilizationId;
                if (resolution.WasAutoResolved)
                {
                    var missed = $"{resolution.Title}: default '{resolution.OptionId}' applied";
                    missedGates.Add(missed);
                    bullets.Add($"Gate auto-resolved — {civName}: {resolution.Title} → {resolution.OptionId}");
                }
                else
                {
                    bullets.Add($"{civName} resolved {resolution.Title} → {resolution.OptionId}");
                }
            }

            foreach (var eventName in tick.NewEvents)
                bullets.Add($"Global event: {eventName}");
        }

        if (PendingGates.Count > 0)
        {
            foreach (var gate in PendingGates)
                bullets.Add($"Pending decision: {gate.Title}");
            headline ??= $"{PendingGates.Count} decision(s) awaiting your response";
        }

        headline ??= Ticks.Count switch
        {
            0 => "No ticks recorded yet",
            1 => "One quiet tick while you were away",
            _ => $"{Ticks.Count} ticks passed — review changes below"
        };

        return new AwaySummaryStructured(headline, bullets, missedGates);
    }

    private static string FormatStructured(AwaySummaryStructured structured)
    {
        var sb = new StringBuilder();
        sb.AppendLine(structured.Headline);

        if (structured.Bullets.Count > 0)
        {
            sb.AppendLine();
            foreach (var bullet in structured.Bullets)
                sb.AppendLine($"  · {bullet}");
        }

        if (structured.MissedGates.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("  Missed gates (defaults applied):");
            foreach (var missed in structured.MissedGates)
                sb.AppendLine($"    — {missed}");
        }

        return sb.ToString().TrimEnd();
    }
}
