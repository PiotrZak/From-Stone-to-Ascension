using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class CoreSystemsTests
{
    [Fact]
    public void TechTreeSystem_ResearchesPrerequisiteChain()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);

        var agriculture = world.Technologies.First(t => t.Id == "tech-agriculture");
        var result = services.Research.Execute(player, agriculture, world);

        Assert.True(result.Success);
        Assert.Contains("tech-agriculture", player.ResearchedTechnologyIds);
    }

    [Fact]
    public void ForbiddenTechSystem_AppliesExtraInstabilityOnEarlyUnlock()
    {
        var civilization = new Civilization("civ-test", "Test Civ");
        civilization.CurrentTier = TechTier.InformationAge;
        var tech = new Technology("tech-forbidden", "Forbidden AI", TechTier.EarlyAI, TechCategory.ArtificialIntelligence, isForbidden: true, riskLevel: 40);
        var stabilityBefore = civilization.TechnologicalStability;

        new SimulationServices().ForbiddenTech.ApplyForbiddenResearch(civilization, tech);

        Assert.True(civilization.TechnologicalStability < stabilityBefore);
    }

    [Fact]
    public void AgentOrchestrator_SkipsBelowTts5()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var rival = world.Civilizations.First(c => !c.IsPlayerControlled);
        rival.CurrentTier = TechTier.Industrial;

        var result = new AgentOrchestrator(services.CreateToolSurface(world)).RunTurn(rival, world);

        Assert.False(result.UsedAgent);
        Assert.Contains("TTS 5", result.Message);
    }

    [Fact]
    public void GameToolSurface_RejectsResearchPriorityBeforeTts5()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var tools = services.CreateToolSurface(world);

        var result = tools.SetResearchPriority(player.Id, "ai", 0.8);

        Assert.False(result.Accepted);
    }

    [Fact]
    public void GameToolSurface_ProposeResearch_ValidatesPrerequisites()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var tools = services.CreateToolSurface(world);

        var result = tools.ProposeResearch(player.Id, "tech-ml");

        Assert.False(result.Accepted);
    }

    [Fact]
    public void GameLoop_AdvancesTurnAndResearchesForPlayer()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var loop = services.CreateGameLoop(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var startingTechCount = player.ResearchedTechnologyIds.Count;

        var result = loop.RunTurn();

        Assert.Equal(1, result.Turn);
        Assert.True(player.ResearchedTechnologyIds.Count > startingTechCount);
    }
}
