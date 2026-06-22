namespace TTS.Agents.Scenarios;

using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

/// <summary>Prepares demo worlds at specific TTS states for Ollama scenarios.</summary>
public static class ScenarioWorldBuilder
{
    private static readonly string[] EarlyAiTechPath =
    [
        "tech-agriculture", "tech-governance", "tech-metallurgy", "tech-steam",
        "tech-electrical", "tech-computing", "tech-ml"
    ];

    private static readonly string[] InformationAgeTechPath = InformationAgeTechSpine.BaselineTechIds;

    public static (WorldState World, GameToolSurface Tools, SimulationServices Services) CreateEarlyAiCrisis()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var tools = services.CreateToolSurface(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var rival = world.Civilizations.First(c => !c.IsPlayerControlled);

        WorldAdvancer.ResearchTechnologiesForAll(world, [player, rival], EarlyAiTechPath, services);
        WorldAdvancer.SetTier(player, TechTier.EarlyAI);
        WorldAdvancer.SetTier(rival, TechTier.EarlyAI);
        player.TechnologicalStability = 45;
        rival.TechnologicalStability = 40;

        world.ActiveEvents.Add(new GlobalEvent(
            "evt-ai-scenario",
            "AI Alignment Crisis",
            "Autonomous systems disagree on governance priorities.",
            TechTier.EarlyAI,
            severity: 3,
            duration: 3));

        world.Turn = 42;
        return (world, tools, services);
    }

    public static (WorldState World, GameToolSurface Tools, SimulationServices Services) CreateInformationAgeWithCrime()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var tools = services.CreateToolSurface(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);

        WorldAdvancer.ResearchTechnologies(world, player, InformationAgeTechPath, services);
        WorldAdvancer.SetTier(player, TechTier.InformationAge);
        world.Turn = 30;
        return (world, tools, services);
    }

    public static (WorldState World, GameToolSurface Tools, SimulationServices Services) CreateFreshWorld()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        return (world, services.CreateToolSurface(world), services);
    }
}
