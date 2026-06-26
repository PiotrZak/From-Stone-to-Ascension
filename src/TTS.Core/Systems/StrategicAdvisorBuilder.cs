namespace TTS.Core.Systems;

using TTS.Core.Agents;
using TTS.Core.Models;

public sealed record StrategicAdvisorBriefing(
    string Headline,
    IReadOnlyList<string> Highlights,
    string Briefing,
    string? RecommendedTechId,
    string? RecommendedTechName,
    string Source,
    AdvisorGateFocus? GateFocus = null);

public static class StrategicAdvisorBuilder
{
    public static StrategicAdvisorBriefing BuildClassical(Civilization civ, IGameToolSurface tools)
    {
        var analysis = tools.GetPolicyResearchAnalysis(civ.Id);
        var gateFocus = GateAdvisorLogic.BuildFocus(civ, tools);

        if (gateFocus is not null)
            return BuildGateCentric(civ, tools, analysis, gateFocus);

        return BuildGeneral(civ, analysis, tools);
    }

    public static StrategicAdvisorBriefing FromLlmText(
        string text,
        Civilization civ,
        IGameToolSurface tools,
        PolicyResearchAnalysis analysis)
    {
        var gateFocus = GateAdvisorLogic.BuildFocus(civ, tools);
        var (headline, highlights, body) = ParseLlmBriefing(text);

        if (gateFocus is not null)
        {
            headline = $"On \"{gateFocus.Title}\": choose {gateFocus.RecommendedOptionLabel}";
            highlights =
            [
                $"Recommended: {gateFocus.RecommendedOptionLabel} — {gateFocus.Rationale}",
                ..gateFocus.Options
                    .Where(o => o.Stance == "caution")
                    .Select(o => $"Avoid {o.Label} — {o.Note}"),
                ..highlights.Take(2),
            ];
        }

        return new StrategicAdvisorBriefing(
            headline,
            highlights.Take(4).ToList(),
            gateFocus is not null ? $"{gateFocus.Rationale} {body}" : body,
            analysis.Recommended?.TechnologyId,
            analysis.Recommended?.Name,
            "llm-tools",
            gateFocus);
    }

    public static StrategicAdvisorBriefing Unavailable(string message, string source = "system") =>
        new(message, [], message, null, null, source);

    private static StrategicAdvisorBriefing BuildGateCentric(
        Civilization civ,
        IGameToolSurface tools,
        PolicyResearchAnalysis analysis,
        AdvisorGateFocus gateFocus)
    {
        var highlights = gateFocus.Options
            .Select(o => o.Stance switch
            {
                "recommended" => $"✓ {o.Label} — {o.Note}",
                "caution" => $"✗ {o.Label} — {o.Note}",
                _ => $"· {o.Label} — {o.Note}",
            })
            .Take(4)
            .ToList();

        var secondary = analysis.Recommended is { } rec
            ? $"After resolving the gate, prioritize {rec.Name} for research."
            : "Resolve the gate before adjusting long-term research.";

        return new StrategicAdvisorBriefing(
            $"Resolve gate: choose {gateFocus.RecommendedOptionLabel}",
            highlights,
            secondary,
            analysis.Recommended?.TechnologyId,
            analysis.Recommended?.Name,
            "classical",
            gateFocus);
    }

    private static StrategicAdvisorBriefing BuildGeneral(
        Civilization civ,
        PolicyResearchAnalysis analysis,
        IGameToolSurface tools)
    {
        var tensions = tools.GetFactionTensions(civ.Id);
        var crime = tools.GetCrimePerspective(civ.Id);
        var highlights = new List<string>();
        var avg = civ.AverageStability;

        var topTension = tensions.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
        if (topTension.Key is not null && topTension.Value >= 0.35)
            highlights.Add($"Faction tension rising with {topTension.Key} ({topTension.Value:P0})");

        if (crime.Available && crime.AverageCrimePressure >= 40)
        {
            highlights.Add(
                crime.CybersecurityMitigationActive
                    ? $"Crime pressure elevated ({crime.AverageCrimePressure:F0}) — cybersecurity active"
                    : $"Crime pressure elevated ({crime.AverageCrimePressure:F0}) — consider stability-first policy");
        }

        var weakest = WeakestPillar(civ);
        if (weakest.value < 45)
            highlights.Add($"{weakest.label} stability low at {weakest.value:F0}% — recovery should be a priority");

        if (analysis.Recommended is { } rec)
            highlights.Add($"Policy engine favors {rec.Name} ({rec.Branch}, risk {rec.RiskLevel})");

        if (highlights.Count == 0)
            highlights.Add($"Stability holding at {avg:F0} — maintain current {FormatStance(analysis.ResearchStance)} policy");

        var headline = weakest.value < 45
            ? $"Strengthen {weakest.label.ToLower()} stability before pushing tier gains"
            : analysis.Recommended is { } top
                ? $"Advance {top.Branch} research under {FormatStance(analysis.ResearchStance)} policy"
                : $"Hold course — {civ.Name} at TTS {(int)civ.CurrentTier}";

        var branchSummary = string.Join(", ",
            analysis.BranchWeights.OrderByDescending(kvp => kvp.Value).Take(3).Select(kvp => $"{kvp.Key} {kvp.Value:P0}"));

        var briefing =
            $"Governance stance is {FormatStance(analysis.ResearchStance)} with {FormatRisk(analysis.RiskTolerance)} risk tolerance. " +
            $"Top research branches: {branchSummary}. " +
            (analysis.Recommended is { } recommended
                ? $"Recommend prioritizing {recommended.Name} next — it scores highest for your current policy."
                : "No strong research candidate this tick — review policy presets or wait for new unlocks.");

        return new StrategicAdvisorBriefing(
            headline,
            highlights.Take(4).ToList(),
            briefing,
            analysis.Recommended?.TechnologyId,
            analysis.Recommended?.Name,
            "classical");
    }

    private static (string Headline, IReadOnlyList<string> Highlights, string Body) ParseLlmBriefing(string text)
    {
        var lines = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeBullet)
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
            return ("Strategic assessment", [], "Advisor returned an empty briefing.");

        var headline = lines[0];
        var highlights = new List<string>();
        var bodyLines = new List<string>();

        foreach (var line in lines.Skip(1))
        {
            if (IsBulletLine(line))
                highlights.Add(StripBullet(line));
            else
                bodyLines.Add(line);
        }

        var body = bodyLines.Count > 0
            ? string.Join(' ', bodyLines)
            : highlights.Count > 0
                ? highlights[^1]
                : headline;

        if (highlights.Count == 0 && lines.Count > 1)
            highlights.AddRange(lines.Skip(1).Take(3));

        return (headline, highlights.Take(4).ToList(), body);
    }

    private static bool IsBulletLine(string line) =>
        line.StartsWith("•", StringComparison.Ordinal) ||
        line.StartsWith("-", StringComparison.Ordinal) ||
        line.StartsWith("*", StringComparison.Ordinal);

    private static string StripBullet(string line) =>
        line.TrimStart('•', '-', '*', ' ').Trim();

    private static string NormalizeBullet(string line) => line.Trim();

    private static (string label, double value) WeakestPillar(Civilization civ)
    {
        var pillars = new (string Label, double Value)[]
        {
            ("Political", civ.PoliticalStability),
            ("Economic", civ.EconomicStability),
            ("Technological", civ.TechnologicalStability),
        };
        return pillars.MinBy(p => p.Value);
    }

    private static string FormatStance(ResearchStance stance) => stance switch
    {
        ResearchStance.TechRush => "tech rush",
        ResearchStance.StabilityFirst => "stability-first",
        ResearchStance.Expansionist => "expansionist",
        _ => "balanced",
    };

    private static string FormatRisk(RiskTolerance risk) => risk switch
    {
        RiskTolerance.High => "high",
        RiskTolerance.Low => "low",
        _ => "moderate",
    };
}
