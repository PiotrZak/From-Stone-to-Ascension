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

        foreach (var civ in world.Civilizations)
        {
            WorldAdvancer.ResearchTechnologies(world, civ, ResolveFor(world), services);
            WorldAdvancer.SetTier(civ, TechTier.InformationAge);
            civ.PoliticalStability = civ.IsPlayerControlled ? 60 : 58;
            civ.EconomicStability = civ.IsPlayerControlled ? 58 : 56;
            civ.TechnologicalStability = civ.IsPlayerControlled ? 62 : 60;
        }
    }
}
