namespace TTS.Core.Systems;

using TTS.Core.Models;

public static class TechTreeViewBuilder
{
    public static IReadOnlyList<TechTreeNodeView> Build(Civilization civilization, WorldState world, TechTreeSystem techTree)
    {
        var available = techTree.GetAvailableTechnologies(civilization, world)
            .Select(t => t.Id)
            .ToHashSet(StringComparer.Ordinal);

        return world.Technologies
            .OrderBy(t => (int)t.Tier)
            .ThenBy(t => t.Role switch
            {
                TechNodeRole.Core => 0,
                TechNodeRole.Branch => 1,
                TechNodeRole.Fusion => 2,
                TechNodeRole.Forbidden => 3,
                _ => 4
            })
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .Select(t =>
            {
                var branch = TechBranchMapping.Describe(t);
                var status = ResolveStatus(civilization, t, available);
                return new TechTreeNodeView(
                    t.Id,
                    t.Name,
                    (int)t.Tier,
                    branch.Branch,
                    t.Role.ToString().ToLowerInvariant(),
                    t.Prerequisites.ToList(),
                    t.RiskLevel,
                    t.IsForbidden,
                    status);
            })
            .ToList();
    }

    private static string ResolveStatus(Civilization civilization, Technology technology, HashSet<string> available)
    {
        if (civilization.ResearchedTechnologyIds.Contains(technology.Id))
            return "researched";

        if (civilization.BannedTechnologyIds.Contains(technology.Id))
            return "blocked";

        if ((int)technology.Tier > (int)civilization.CurrentTier + 1)
            return "blocked";

        if (available.Contains(technology.Id))
            return "available";

        return "locked";
    }
}

public readonly record struct TechTreeNodeView(
    string Id,
    string Name,
    int Tier,
    string Branch,
    string Role,
    IReadOnlyList<string> Prerequisites,
    int RiskLevel,
    bool IsForbidden,
    string Status);
