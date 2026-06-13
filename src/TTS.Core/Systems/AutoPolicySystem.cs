namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Selects the next technology to research based on civilization policy.
/// </summary>
public class AutoPolicySystem
{
    private readonly TechTreeSystem _techTreeSystem = new();

    public Technology? SelectNextTechnology(Civilization civilization, WorldState world, CivilizationPolicy policy)
    {
        var candidates = _techTreeSystem
            .GetAvailableTechnologies(civilization, world)
            .Where(t => IsAllowedByRisk(t, policy))
            .Select(t => (Tech: t, Score: ScoreTechnology(t, civilization, policy)))
            .OrderByDescending(x => x.Score)
            .ToList();

        return candidates.FirstOrDefault().Tech;
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

    private static double ScoreTechnology(Technology technology, Civilization civilization, CivilizationPolicy policy)
    {
        var score = GetBranchWeight(technology, policy);

        score += policy.Research switch
        {
            ResearchStance.TechRush => (int)technology.Tier * 2.0 + technology.RiskLevel * 0.1,
            ResearchStance.StabilityFirst => -(technology.RiskLevel * 0.5),
            ResearchStance.Expansionist => IsNewBranch(technology, civilization) ? 3.0 : 0,
            _ => (int)technology.Tier * 0.5
        };

        return score;
    }

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

        var categoryKey = CategoryToBranch(technology.Category);
        if (policy.BranchWeights.TryGetValue(categoryKey, out var categoryWeight))
        {
            score += categoryWeight;
            matched = true;
        }

        return matched ? score : 1.0;
    }

    private static bool IsNewBranch(Technology technology, Civilization civilization)
    {
        var categoryKey = CategoryToBranch(technology.Category);
        return !civilization.ResearchedTechnologyIds.Any(id =>
            id.Contains(categoryKey, StringComparison.OrdinalIgnoreCase));
    }

    private static string CategoryToBranch(TechCategory category) => category switch
    {
        TechCategory.Agriculture => "agriculture",
        TechCategory.Manufacturing => "manufacturing",
        TechCategory.Energy => "energy",
        TechCategory.Communication => "communication",
        TechCategory.Computing => "computing",
        TechCategory.Military => "military",
        TechCategory.Biology => "biology",
        TechCategory.Nanotechnology => "nano",
        TechCategory.ArtificialIntelligence => "ai",
        TechCategory.TemporalManipulation => "temporal",
        TechCategory.RealityEngineering => "reality",
        _ => "general"
    };
}
