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

            var runner = tick.ResearchDecisions.FirstOrDefault(d => d.CivilizationId == civId).Runner;
            var tag = RunnerTag(runner);
            lines.Add($"{CivName(world, civId)} researched {string.Join(", ", techs)}{tag}");
        }

        foreach (var decision in tick.ResearchDecisions)
        {
            if (decision.Runner is not ("agent" or "classical-ai"))
                continue;

            if (decision.Researched)
                continue;

            if (string.IsNullOrWhiteSpace(decision.Message))
                continue;

            lines.Add($"{decision.CivilizationName}{RunnerTag(decision.Runner)}: {decision.Message}");
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

    private static string RunnerTag(string runner) => runner switch
    {
        "agent" => " · LLM",
        "classical-ai" => " · classical",
        _ => ""
    };
}

public sealed record MatchTickLogEntry(int Tick, IReadOnlyList<string> Lines);
