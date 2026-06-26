namespace TTS.Core.Systems;

using TTS.Core.Agents;
using TTS.Core.Models;

public sealed record AdvisorOptionGuidance(
    string OptionId,
    string Label,
    string Stance,
    string Note);

public sealed record AdvisorGateFocus(
    string GateId,
    string Title,
    GateType Type,
    string Rationale,
    string RecommendedOptionId,
    string RecommendedOptionLabel,
    IReadOnlyList<AdvisorOptionGuidance> Options);

public static class GateAdvisorLogic
{
    public static AdvisorGateFocus? BuildFocus(Civilization civ, IGameToolSurface tools)
    {
        var gate = tools.GetPendingDecisions(civ.Id).FirstOrDefault();
        if (gate is null)
            return null;

        var analysis = tools.GetPolicyResearchAnalysis(civ.Id);
        var crime = tools.GetCrimePerspective(civ.Id);
        var recommendedId = RecommendOptionId(gate, civ, analysis, crime);
        var recommended = gate.Options.First(o => o.Id == recommendedId);
        var rationale = BuildRationale(gate, civ, recommendedId, analysis, crime);
        var cautionId = CautionOptionId(gate, recommendedId);

        var options = gate.Options.Select(o => new AdvisorOptionGuidance(
            o.Id,
            o.Label,
            StanceFor(o.Id, recommendedId, cautionId),
            OptionNote(o, o.Id == recommendedId, o.Id == cautionId))).ToList();

        return new AdvisorGateFocus(
            gate.Id,
            gate.Title,
            gate.Type,
            rationale,
            recommendedId,
            recommended.Label,
            options);
    }

    private static string RecommendOptionId(
        DecisionGate gate,
        Civilization civ,
        PolicyResearchAnalysis analysis,
        CrimePerspectiveSummary crime) => gate.Type switch
    {
        GateType.CrimePressure => RecommendCrime(civ, analysis, crime),
        GateType.FactionCrisis => RecommendFaction(civ, analysis),
        GateType.ForbiddenTech => RecommendForbidden(analysis),
        GateType.TierAdvancement => RecommendTierAdvancement(analysis),
        GateType.GlobalCrisis => RecommendGlobalCrisis(analysis),
        GateType.AiAlignment => RecommendAiAlignment(civ, analysis),
        _ => gate.DefaultOptionId,
    };

    private static string RecommendCrime(
        Civilization civ,
        PolicyResearchAnalysis analysis,
        CrimePerspectiveSummary crime)
    {
        if (analysis.ResearchStance == ResearchStance.TechRush && civ.AverageStability >= 62 && crime.AverageCrimePressure < 50)
            return "crackdown";
        if (analysis.ResearchStance == ResearchStance.StabilityFirst || civ.AverageStability < 55
            || (crime.Available && crime.AverageCrimePressure >= 35))
            return "invest";
        return "invest";
    }

    private static string RecommendFaction(Civilization civ, PolicyResearchAnalysis analysis)
    {
        if (analysis.ResearchStance == ResearchStance.TechRush || analysis.ResearchStance == ResearchStance.Expansionist)
            return "suppress";
        if (civ.PoliticalStability < 50 || analysis.ResearchStance == ResearchStance.StabilityFirst)
            return "appease";
        return "reform";
    }

    private static string RecommendForbidden(PolicyResearchAnalysis analysis) =>
        analysis.ResearchStance == ResearchStance.TechRush && analysis.RiskTolerance == RiskTolerance.High
            ? "pursue"
            : analysis.ResearchStance == ResearchStance.StabilityFirst
                ? "ban"
                : "delay";

    private static string RecommendTierAdvancement(PolicyResearchAnalysis analysis) =>
        analysis.ResearchStance == ResearchStance.TechRush ? "embrace"
            : analysis.ResearchStance == ResearchStance.StabilityFirst ? "delay"
            : "regulate";

    private static string RecommendGlobalCrisis(PolicyResearchAnalysis analysis) =>
        analysis.ResearchStance == ResearchStance.TechRush ? "accelerate"
            : analysis.ResearchStance == ResearchStance.StabilityFirst ? "regulate"
            : "isolate";

    private static string RecommendAiAlignment(Civilization civ, PolicyResearchAnalysis analysis)
    {
        if (analysis.ResearchStance == ResearchStance.TechRush && civ.TechnologicalStability >= 55)
            return "merge";
        if (analysis.ResearchStance == ResearchStance.StabilityFirst)
            return "contain";
        return "align";
    }

    private static string? CautionOptionId(DecisionGate gate, string recommendedId)
    {
        var ids = gate.Options.Select(o => o.Id).ToHashSet();
        return gate.Type switch
        {
            GateType.CrimePressure when recommendedId == "invest" => ids.Contains("ignore") ? "ignore" : null,
            GateType.FactionCrisis when recommendedId is "appease" or "reform" => ids.Contains("suppress") ? "suppress" : null,
            GateType.ForbiddenTech when recommendedId is "ban" or "delay" => ids.Contains("pursue") ? "pursue" : null,
            GateType.TierAdvancement when recommendedId is "regulate" or "delay" => ids.Contains("embrace") ? "embrace" : null,
            GateType.GlobalCrisis when recommendedId is "regulate" or "isolate" => ids.Contains("accelerate") ? "accelerate" : null,
            GateType.AiAlignment when recommendedId is "align" or "contain" => ids.Contains("merge") ? "merge" : null,
            _ => null,
        };
    }

    private static string StanceFor(string optionId, string recommendedId, string? cautionId) =>
        optionId == recommendedId ? "recommended"
            : optionId == cautionId ? "caution"
            : "neutral";

    private static string OptionNote(DecisionOption option, bool recommended, bool caution)
    {
        if (recommended)
            return string.IsNullOrWhiteSpace(option.ImpactHint) ? "Best fit for current policy" : option.ImpactHint;
        if (caution)
            return "High risk under current stability and policy";
        return string.IsNullOrWhiteSpace(option.ImpactHint) ? option.Description : option.ImpactHint;
    }

    private static string BuildRationale(
        DecisionGate gate,
        Civilization civ,
        string recommendedId,
        PolicyResearchAnalysis analysis,
        CrimePerspectiveSummary crime)
    {
        var option = gate.Options.First(o => o.Id == recommendedId);
        var stance = analysis.ResearchStance switch
        {
            ResearchStance.TechRush => "tech-rush",
            ResearchStance.StabilityFirst => "stability-first",
            ResearchStance.Expansionist => "expansionist",
            _ => "balanced",
        };

        var context = gate.Type switch
        {
            GateType.CrimePressure when crime.Available =>
                $"Crime pressure is {crime.AverageCrimePressure:F0} and average stability is {civ.AverageStability:F0}.",
            GateType.FactionCrisis =>
                $"Political stability is {civ.PoliticalStability:F0} during internal faction pressure.",
            GateType.ForbiddenTech =>
                $"Forbidden research is on the table with {analysis.RiskTolerance} risk tolerance.",
            GateType.TierAdvancement =>
                $"A tier shift is pending at TTS {(int)civ.CurrentTier}.",
            GateType.AiAlignment =>
                $"Technological stability is {civ.TechnologicalStability:F0} as autonomous systems escalate.",
            _ => $"Average stability is {civ.AverageStability:F0}.",
        };

        return $"{context} Under {stance} policy, {option.Label} is the strongest response to \"{gate.Title}\". {option.Description}";
    }
}
