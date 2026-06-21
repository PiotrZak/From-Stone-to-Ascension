namespace TTS.Core.Systems;

using TTS.Core.Models;
using TTS.Core.Simulation;

public static class MatchLogBuilder
{
    public static IReadOnlyList<MatchTickLogEntry> Build(
        WorldState world,
        IReadOnlyList<TurnSnapshot> history)
    {
        return history
            .OrderBy(h => h.Turn)
            .Select(tick => new MatchTickLogEntry(tick.Turn, FormatTickLines(world, tick)))
            .ToList();
    }

    private static List<string> FormatTickLines(WorldState world, TurnSnapshot tick)
    {
        var lines = new List<string>();

        foreach (var (civId, tierChange) in tick.TierChanges)
        {
            var name = CivName(world, civId);
            lines.Add($"{name} reached TTS {(int)tierChange.To}");
        }

        foreach (var (civId, techs) in tick.ResearchedThisTurn)
        {
            if (techs.Count == 0)
                continue;

            lines.Add($"{CivName(world, civId)} researched {string.Join(", ", techs)}");
        }

        foreach (var resolution in tick.GateResolutions)
        {
            var auto = resolution.WasAutoResolved ? " (auto)" : "";
            lines.Add($"{CivName(world, resolution.CivilizationId)} decided '{resolution.Title}': {resolution.OptionId}{auto}");
        }

        foreach (var eventName in tick.NewEvents)
            lines.Add($"Global event: {eventName}");

        if (lines.Count == 0)
            lines.Add("World advanced — no major headlines.");

        return lines;
    }

    private static string CivName(WorldState world, string civId) =>
        world.Civilizations.FirstOrDefault(c => c.Id == civId)?.Name ?? civId;
}

public sealed record MatchTickLogEntry(int Tick, IReadOnlyList<string> Lines);
