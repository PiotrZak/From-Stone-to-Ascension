namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Manages technology research, tier advancement, and prerequisite validation.
/// </summary>
public class TechTreeSystem
{
    public bool CanResearch(Civilization civilization, Technology technology)
    {
        if (civilization.ResearchedTechnologyIds.Contains(technology.Id))
            return false;

        if (civilization.BannedTechnologyIds.Contains(technology.Id))
            return false;

        if ((int)technology.Tier > (int)civilization.CurrentTier + 1)
            return false;

        return technology.Prerequisites.All(civilization.ResearchedTechnologyIds.Contains);
    }

    public ResearchResult Research(
        Civilization civilization,
        Technology technology,
        ForbiddenTechSystem forbiddenTechSystem,
        WorldState world)
    {
        if (!CanResearch(civilization, technology))
            return ResearchResult.Rejected("Prerequisites not met or technology already researched.");

        if (technology.IsForbidden)
            forbiddenTechSystem.ApplyForbiddenResearch(civilization, technology);

        civilization.ResearchedTechnologyIds.Add(technology.Id);
        TryAdvanceTier(civilization, world);

        return ResearchResult.Succeeded(technology.Id);
    }

    public IEnumerable<Technology> GetAvailableTechnologies(Civilization civilization, WorldState world)
    {
        return world.Technologies.Where(t => CanResearch(civilization, t));
    }

    public IEnumerable<Technology> GetTechnologiesForTier(WorldState world, TechTier tier)
    {
        return world.Technologies.Where(t => t.Tier == tier);
    }

    private static void TryAdvanceTier(Civilization civilization, WorldState world)
    {
        var peakResearchedTier = world.Technologies
            .Where(t => civilization.ResearchedTechnologyIds.Contains(t.Id))
            .Select(t => (int)t.Tier)
            .DefaultIfEmpty(0)
            .Max();

        if (peakResearchedTier == 0)
            return;

        var targetTier = (TechTier)peakResearchedTier;
        if ((int)targetTier > (int)civilization.CurrentTier)
            civilization.CurrentTier = targetTier;
    }
}

public readonly record struct ResearchResult(bool Success, string Message, string? TechnologyId = null)
{
    public static ResearchResult Succeeded(string technologyId) =>
        new(true, "Research completed.", technologyId);

    public static ResearchResult Rejected(string reason) =>
        new(false, reason);
}
