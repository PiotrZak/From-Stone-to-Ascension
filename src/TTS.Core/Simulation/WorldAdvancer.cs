namespace TTS.Core.Simulation;

using TTS.Core.Models;

/// <summary>Advances demo/scenario worlds using the same rules as the live simulation.</summary>
public static class WorldAdvancer
{
    public static void ResearchTechnologies(
        WorldState world,
        Civilization civilization,
        IEnumerable<string> technologyIds,
        SimulationServices services)
    {
        foreach (var technologyId in technologyIds)
        {
            var technology = world.Technologies.First(t => t.Id == technologyId);
            services.Research.Execute(civilization, technology, world);
        }
    }

    public static void ResearchTechnologiesForAll(
        WorldState world,
        IEnumerable<Civilization> civilizations,
        IEnumerable<string> technologyIds,
        SimulationServices services)
    {
        foreach (var civilization in civilizations)
            ResearchTechnologies(world, civilization, technologyIds, services);
    }

    public static void SetTier(Civilization civilization, TechTier tier) =>
        civilization.CurrentTier = tier;
}
