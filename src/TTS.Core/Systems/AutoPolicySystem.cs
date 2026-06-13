namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Selects the next technology to research based on civilization policy.
/// </summary>
public class AutoPolicySystem
{
    private readonly TechTreeSystem _techTreeSystem;

    public AutoPolicySystem() : this(new TechTreeSystem())
    {
    }

    public AutoPolicySystem(TechTreeSystem techTreeSystem) => _techTreeSystem = techTreeSystem;

    public Technology? SelectNextTechnology(Civilization civilization, WorldState world, CivilizationPolicy policy)
    {
        var pick = EvaluateCandidates(civilization, world, policy).FirstOrDefault(c => c.AllowedByRisk);
        if (string.IsNullOrEmpty(pick.TechnologyId))
            return null;

        return world.Technologies.FirstOrDefault(t => t.Id == pick.TechnologyId);
    }

    public PolicyResearchAnalysis Analyze(Civilization civilization, WorldState world, CivilizationPolicy policy)
    {
        var ranked = EvaluateCandidates(civilization, world, policy);
        var recommended = ranked.FirstOrDefault(c => c.AllowedByRisk);
        ResearchCandidateEvaluation? pick = recommended.AllowedByRisk ? recommended : null;
        return new PolicyResearchAnalysis(
            policy.Research,
            policy.Risk,
            policy.BranchWeights,
            ranked,
            pick);
    }

    public IReadOnlyList<ResearchCandidateEvaluation> EvaluateCandidates(
        Civilization civilization,
        WorldState world,
        CivilizationPolicy policy)
    {
        return _techTreeSystem
            .GetAvailableTechnologies(civilization, world)
            .Select(t => BuildEvaluation(t, civilization, policy))
            .OrderByDescending(e => e.AllowedByRisk)
            .ThenByDescending(e => e.TotalScore)
            .ToList();
    }

    private static ResearchCandidateEvaluation BuildEvaluation(
        Technology technology,
        Civilization civilization,
        CivilizationPolicy policy)
    {
        var branch = TechBranchMapping.CategoryToBranch(technology.Category);
        var allowed = IsAllowedByRisk(technology, policy);
        var branchWeight = GetBranchWeight(technology, policy);
        var stanceBonus = GetStanceBonus(technology, civilization, policy);
        var total = allowed ? branchWeight + stanceBonus : 0;

        return new ResearchCandidateEvaluation(
            technology.Id,
            technology.Name,
            technology.Category,
            branch,
            technology.FusionTags,
            technology.Tier,
            technology.RiskLevel,
            technology.IsForbidden,
            allowed,
            branchWeight,
            stanceBonus,
            total,
            allowed ? null : DescribeRejection(technology, policy));
    }

    private static string DescribeRejection(Technology technology, CivilizationPolicy policy)
    {
        if (technology.IsForbidden && policy.Risk != RiskTolerance.High)
            return "forbidden tech blocked (risk tolerance too low)";

        return $"risk {technology.RiskLevel} exceeds policy cap";
    }

    private static bool IsAllowedByRisk(Technology technology, CivilizationPolicy policy)
    {
        if (technology.IsForbidden && policy.Risk != RiskTolerance.High)
            return false;

        var maxRisk = policy.Risk switch
        {
            RiskTolerance.Low => 25,
            RiskTolerance.Medium => policy.Research == ResearchStance.TechRush ? 80 : 50,
            RiskTolerance.High => 100,
            _ => 50
        };

        if (policy.Research == ResearchStance.StabilityFirst)
            maxRisk = Math.Min(maxRisk, 20);

        return technology.RiskLevel <= maxRisk;
    }

    private static double GetStanceBonus(Technology technology, Civilization civilization, CivilizationPolicy policy) =>
        policy.Research switch
        {
            ResearchStance.TechRush => (int)technology.Tier * 2.0 + technology.RiskLevel * 0.1,
            ResearchStance.StabilityFirst => -(technology.RiskLevel * 0.5),
            ResearchStance.Expansionist => IsNewBranch(technology, civilization) ? 3.0 : 0,
            _ => (int)technology.Tier * 0.5
        };

    private static double GetBranchWeight(Technology technology, CivilizationPolicy policy)
    {
        var score = 0.0;
        var matched = false;

        foreach (var tag in technology.FusionTags)
        {
            if (policy.BranchWeights.TryGetValue(tag, out var weight))
            {
                score += weight;
                matched = true;
            }
        }

        var categoryKey = TechBranchMapping.CategoryToBranch(technology.Category);
        if (policy.BranchWeights.TryGetValue(categoryKey, out var categoryWeight))
        {
            score += categoryWeight;
            matched = true;
        }

        return matched ? score : 1.0;
    }

    private static bool IsNewBranch(Technology technology, Civilization civilization)
    {
        var categoryKey = TechBranchMapping.CategoryToBranch(technology.Category);
        return !civilization.ResearchedTechnologyIds.Any(id =>
            id.Contains(categoryKey, StringComparison.OrdinalIgnoreCase));
    }
}
