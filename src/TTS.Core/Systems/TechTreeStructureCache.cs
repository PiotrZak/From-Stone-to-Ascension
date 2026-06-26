namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>Immutable structural prototype for tech tree UI — status is overlaid per civilization.</summary>
internal readonly record struct TechTreeNodePrototype(
    string Id,
    string Name,
    int Tier,
    string Branch,
    string Role,
    IReadOnlyList<string> Prerequisites,
    int RiskLevel,
    bool IsForbidden);

internal static class TechTreeStructureCache
{
    private static readonly object Sync = new();
    private static string? _cacheKey;
    private static IReadOnlyList<TechTreeNodePrototype>? _structure;

    public static IReadOnlyList<TechTreeNodePrototype> GetStructure(WorldState world)
    {
        var key = BuildKey(world.Technologies);
        lock (Sync)
        {
            if (_cacheKey == key && _structure is not null)
                return _structure;

            _cacheKey = key;
            _structure = BuildStructure(world.Technologies);
            return _structure;
        }
    }

    internal static void ClearForTests()
    {
        lock (Sync)
        {
            _cacheKey = null;
            _structure = null;
        }
    }

    private static string BuildKey(IReadOnlyList<Technology> technologies)
    {
        if (technologies.Count == 0)
            return "";

        return string.Join('|', technologies.OrderBy(t => t.Id).Select(t => t.Id));
    }

    private static IReadOnlyList<TechTreeNodePrototype> BuildStructure(IReadOnlyList<Technology> technologies) =>
        technologies
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
                return new TechTreeNodePrototype(
                    t.Id,
                    t.Name,
                    (int)t.Tier,
                    branch.Branch,
                    t.Role.ToString().ToLowerInvariant(),
                    t.Prerequisites,
                    t.RiskLevel,
                    t.IsForbidden);
            })
            .ToList();
}
