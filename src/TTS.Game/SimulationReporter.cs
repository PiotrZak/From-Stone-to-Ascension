using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

namespace TTS.Game;

internal static class SimulationReporter
{
    public static void PrintTechCatalog(IReadOnlyList<Technology> technologies, GameToolSurface tools)
    {
        Console.WriteLine();
        Console.WriteLine("Technology catalog (category → branch):");
        foreach (var tech in technologies.OrderBy(t => (int)t.Tier).ThenBy(t => t.Name))
        {
            var detail = tools.GetTechnologyDetail(tech.Id);
            var tags = detail.FusionTags.Count > 0 ? $" tags=[{string.Join(", ", detail.FusionTags)}]" : "";
            var risk = detail.RiskLevel > 0 ? $" risk={detail.RiskLevel}" : "";
            var forbidden = detail.IsForbidden ? " [FORBIDDEN]" : "";
            Console.WriteLine(
                $"  {detail.Id}: {detail.Category} → {detail.Branch} (TTS {(int)detail.Tier}){tags}{risk}{forbidden}");
        }
    }

    public static void PrintPolicyBranches(Civilization civilization, GameToolSurface tools)
    {
        var weights = tools.GetPolicyBranchWeights(civilization.Id);
        var weightText = weights.Count > 0
            ? string.Join(", ", weights.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value:F1}"))
            : "none";
        Console.WriteLine(
            $"  Policy: {civilization.Policy.Research}, risk={civilization.Policy.Risk}, branches: {weightText}");
    }

    public static void PrintResearchDecision(TurnResearchDecision decision, GameToolSurface tools)
    {
        if (!decision.Researched || decision.TechnologyId is null)
        {
            Console.WriteLine($"  Research [{decision.Runner}]: skipped — {decision.Message}");
            return;
        }

        var detail = tools.GetTechnologyDetail(decision.TechnologyId);
        Console.WriteLine(
            $"  Research [{decision.Runner}]: {detail.Name} ({detail.Id})");
        Console.WriteLine(
            $"    category {detail.Category} → branch {detail.Branch}" +
            (detail.FusionTags.Count > 0 ? $", fusion tags [{string.Join(", ", detail.FusionTags)}]" : ""));

        if (decision.Evaluation is { } eval)
        {
            Console.WriteLine(
                $"    score {eval.TotalScore:F1} = branch {eval.BranchWeightScore:F1} + stance {eval.StanceBonus:F1}" +
                (eval.RiskLevel > 0 ? $", risk {eval.RiskLevel}" : "") +
                (eval.IsForbidden ? ", FORBIDDEN" : ""));
        }
    }

    public static void PrintResearchCandidates(Civilization civilization, GameToolSurface tools, int top = 3)
    {
        var analysis = tools.GetPolicyResearchAnalysis(civilization.Id);
        if (analysis.RankedCandidates.Count == 0)
            return;

        Console.WriteLine($"  Candidates ({civilization.Name}, top {top}):");
        foreach (var candidate in analysis.RankedCandidates.Take(top))
        {
            var status = candidate.AllowedByRisk ? "ok" : $"blocked ({candidate.RejectionReason})";
            Console.WriteLine(
                $"    {candidate.Name}: {candidate.Category}→{candidate.Branch}, " +
                $"score {candidate.TotalScore:F1} ({candidate.BranchWeightScore:F1}+{candidate.StanceBonus:F1}) [{status}]");
        }
    }
}
