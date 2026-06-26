namespace TTS.Core.Systems;

using TTS.Core.Models;
using TTS.Core.Simulation;

/// <summary>Baseline technologies applied when a match starts at TTS 4.</summary>
public static class InformationAgeTechSpine
{
    /// <summary>Core path through TTS 1–3 plus Digital Computing and Cybersecurity.</summary>
    public static readonly string[] BaselineTechIds =
    [
        "tech-agriculture",
        "tech-governance",
        "tech-metallurgy",
        "tech-steam",
        "tech-electrical",
        "tech-computing",
        "tech-cybersecurity"
    ];

    /// <summary>Extended baseline including satellite network chain.</summary>
    public static readonly string[] ExtendedTechIds =
    [
        ..BaselineTechIds,
        "tech-data-storage",
        "tech-internet",
        "tech-satellite"
    ];

    public static IEnumerable<string> ResolvePriorEraFoundation(WorldState world, TechTier startingTier) =>
        world.Technologies
            .Where(t => t.Tier < startingTier && !t.IsForbidden)
            .OrderBy(t => (int)t.Tier)
            .ThenBy(t => t.Id)
            .Select(t => t.Id);

    public static IEnumerable<string> ResolveBootstrapTechIds(WorldState world, TechTier startingTier, bool extended = true)
    {
        if (startingTier < TechTier.InformationAge)
            return [];

        var spine = extended ? ExtendedTechIds : BaselineTechIds;
        var available = world.Technologies.Select(t => t.Id).ToHashSet();
        return ResolvePriorEraFoundation(world, startingTier)
            .Concat(spine.Where(available.Contains))
            .Distinct();
    }

    public static IEnumerable<string> ResolveFor(WorldState world, bool extended = true)
    {
        var ids = extended ? ExtendedTechIds : BaselineTechIds;
        var available = world.Technologies.Select(t => t.Id).ToHashSet();
        return ids.Where(available.Contains);
    }

    public static void ApplyBootstrap(WorldState world, SimulationServices services, TechTier startingTier)
    {
        if (startingTier < TechTier.InformationAge)
            return;

        var bootstrapIds = ResolveBootstrapTechIds(world, startingTier).ToList();

        foreach (var civ in world.Civilizations)
        {
            WorldAdvancer.GrantResearchHistory(civ, bootstrapIds);
            WorldAdvancer.SetTier(civ, TechTier.InformationAge);
            civ.PoliticalStability = civ.IsPlayerControlled ? 60 : 58;
            civ.EconomicStability = civ.IsPlayerControlled ? 58 : 56;
            civ.TechnologicalStability = civ.IsPlayerControlled ? 62 : 60;
        }
    }
}
