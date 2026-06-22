namespace TTS.Core.Systems;

using System.Text;
using TTS.Core.Models;
using TTS.Core.Simulation;

public sealed class MatchResultsBuilder
{
    public IReadOnlyList<MatchResultEntry> Build(WorldState world, SimulationServices services)
    {
        return world.Civilizations
            .Select(c =>
            {
                var outcome = services.WinLoss.Evaluate(c, world.Match?.Config);
                return new MatchResultEntry(
                    c.Id,
                    c.Name,
                    (int)c.CurrentTier,
                    c.AverageStability,
                    c.ResearchedTechnologyIds.Count,
                    FormatOutcomeLabel(outcome),
                    outcome.Message);
            })
            .OrderByDescending(r => r.Tier)
            .ThenByDescending(r => r.Stability)
            .ThenByDescending(r => r.TechCount)
            .Select((r, i) => r with { Rank = i + 1 })
            .ToList();
    }

    public string Format(IReadOnlyList<MatchResultEntry> results, string modeDisplayName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Match ended — {modeDisplayName}");
        sb.AppendLine();

        foreach (var r in results)
        {
            sb.AppendLine(
                $"{r.Rank}. {r.CivilizationName} — TTS {r.Tier}, stability {r.Stability:F0}, {r.TechCount} techs ({r.Outcome})");
            if (!string.IsNullOrWhiteSpace(r.OutcomeReason) && r.OutcomeReason != "Simulation in progress.")
                sb.AppendLine($"     {r.OutcomeReason}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatOutcomeLabel(GameOutcome outcome) =>
        outcome.IsVictory ? "victory" : outcome.IsDefeat ? "defeat" : "finished";
}

public sealed record MatchResultEntry(
    string CivilizationId,
    string CivilizationName,
    int Tier,
    double Stability,
    int TechCount,
    string Outcome,
    string OutcomeReason)
{
    public int Rank { get; init; }
}
