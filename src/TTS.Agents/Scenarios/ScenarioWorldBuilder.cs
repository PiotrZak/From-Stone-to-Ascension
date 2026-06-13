namespace TTS.Agents.Scenarios;

using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Prepares demo worlds at specific TTS states for Ollama scenarios.</summary>
public static class ScenarioWorldBuilder
{
    public static (WorldState World, GameToolSurface Tools) CreateEarlyAiCrisis()
    {
        var world = SampleWorldFactory.Create();
        var loop = new GameLoop(world);
        var tools = new GameToolSurface(world);
        var forbidden = new ForbiddenTechSystem();
        var techTree = new TechTreeSystem();

        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var rival = world.Civilizations.First(c => !c.IsPlayerControlled);

        foreach (var techId in new[] { "tech-agriculture", "tech-governance", "tech-metallurgy", "tech-steam",
                     "tech-electrical", "tech-computing", "tech-ml" })
        {
            var tech = world.Technologies.First(t => t.Id == techId);
            techTree.Research(player, tech, forbidden);
            techTree.Research(rival, tech, forbidden);
        }

        player.CurrentTier = TechTier.EarlyAI;
        rival.CurrentTier = TechTier.EarlyAI;
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
        return (world, tools);
    }

    public static (WorldState World, GameToolSurface Tools) CreateInformationAgeWithCrime()
    {
        var world = SampleWorldFactory.Create();
        var tools = new GameToolSurface(world);
        var forbidden = new ForbiddenTechSystem();
        var techTree = new TechTreeSystem();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);

        foreach (var techId in new[] { "tech-agriculture", "tech-governance", "tech-metallurgy", "tech-steam", "tech-electrical", "tech-computing" })
        {
            var tech = world.Technologies.First(t => t.Id == techId);
            techTree.Research(player, tech, forbidden);
        }

        player.CurrentTier = TechTier.InformationAge;
        world.Turn = 30;
        return (world, tools);
    }

    public static (WorldState World, GameToolSurface Tools) CreateFreshWorld()
    {
        var world = SampleWorldFactory.Create();
        return (world, new GameToolSurface(world));
    }
}
