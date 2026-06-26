namespace TTS.Core.Systems;

using TTS.Core.Models;

public static class TechTreeViewBuilder
{
    public static IReadOnlyList<TechTreeNodeView> Build(Civilization civilization, WorldState world, TechTreeSystem techTree)
    {
        var available = techTree.GetAvailableTechnologies(civilization, world)
            .Select(t => t.Id)
            .ToHashSet(StringComparer.Ordinal);

        var structure = TechTreeStructureCache.GetStructure(world);
        var nodes = new TechTreeNodeView[structure.Count];

        for (var i = 0; i < structure.Count; i++)
        {
            var proto = structure[i];
            nodes[i] = new TechTreeNodeView(
                proto.Id,
                proto.Name,
                proto.Tier,
                proto.Branch,
                proto.Role,
                proto.Prerequisites,
                proto.RiskLevel,
                proto.IsForbidden,
                ResolveStatus(civilization, proto, available));
        }

        return nodes;
    }

    private static string ResolveStatus(Civilization civilization, TechTreeNodePrototype proto, HashSet<string> available)
    {
        if (civilization.ResearchedTechnologyIds.Contains(proto.Id))
            return "researched";

        if (civilization.BannedTechnologyIds.Contains(proto.Id))
            return "blocked";

        if (proto.Tier > (int)civilization.CurrentTier + 1)
            return "blocked";

        if (available.Contains(proto.Id))
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
