using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class AutoPolicyTests
{
    [Fact]
    public void AutoPolicy_StabilityFirst_AvoidsForbiddenTech()
    {
        var world = CreatePolicyTestWorld();
        var civ = world.Civilizations[0];
        civ.ResearchedTechnologyIds.Add("tech-ml");
        civ.CurrentTier = TechTier.InformationAge;

        var services = new SimulationServices();
        var next = services.AutoPolicy.SelectNextTechnology(civ, world, CivilizationPolicy.StabilityFirst());

        Assert.NotNull(next);
        Assert.NotEqual("tech-forbidden", next.Id);
    }

    [Fact]
    public void AutoPolicy_TechRush_PrefersRiskyAiTech()
    {
        var world = CreatePolicyTestWorld();
        var civ = world.Civilizations[0];
        civ.ResearchedTechnologyIds.Add("tech-ml");
        civ.CurrentTier = TechTier.InformationAge;
        civ.Policy = CivilizationPolicy.TechRush();

        var services = new SimulationServices();
        var next = services.AutoPolicy.SelectNextTechnology(civ, world, civ.Policy);

        Assert.NotNull(next);
        Assert.Equal("tech-recursive-ai", next.Id);
    }

    [Fact]
    public void AutoPolicy_EvaluateCandidates_IncludesScoreBreakdown()
    {
        var world = SampleWorldFactory.Create();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var services = new SimulationServices();

        var candidates = services.AutoPolicy.EvaluateCandidates(player, world, player.Policy);
        var pick = candidates.First(c => c.AllowedByRisk);

        Assert.True(pick.TotalScore > 0);
        Assert.Equal("agriculture", pick.Branch);
        Assert.Equal(pick.BranchWeightScore + pick.StanceBonus, pick.TotalScore);
    }

    [Fact]
    public void ClassicalAi_RivalResearchesOnFirstTurn()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var rival = world.Civilizations.First(c => !c.IsPlayerControlled);

        var result = services.ClassicalAi.RunTurn(rival, world);

        Assert.True(result.DidResearch);
        Assert.InRange(rival.ResearchedTechnologyIds.Count, 1, 2);
    }

    [Fact]
    public void GameLoop_RivalProgressesOverMultipleTurns()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var loop = services.CreateGameLoop(world);
        var rival = world.Civilizations.First(c => !c.IsPlayerControlled);

        for (var i = 0; i < 4; i++)
            loop.RunTurn();

        Assert.True(rival.ResearchedTechnologyIds.Count >= 3);
        Assert.True((int)rival.CurrentTier >= (int)TechTier.EarlyElectronics);
    }

    [Fact]
    public void GameLoop_PlayerUsesPolicyNotFirstTechOnly()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        player.Policy = CivilizationPolicy.StabilityFirst();

        services.CreateGameLoop(world).RunTurn();

        Assert.Contains("tech-agriculture", player.ResearchedTechnologyIds);
    }

    private static WorldState CreatePolicyTestWorld()
    {
        var world = SampleWorldFactory.Create();
        world.Civilizations.Clear();
        world.Civilizations.Add(new Civilization("civ-test", "Test Civ"));
        return world;
    }
}
