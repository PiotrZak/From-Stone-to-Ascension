using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;

namespace TTS.Tests;

public class InformationAgeBootstrapTests
{
    [Fact]
    public void SprintMatch_StartsAtInformationAgeWithTechSpine()
    {
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h);

        foreach (var civ in world.Civilizations)
        {
            Assert.Equal(TechTier.InformationAge, civ.CurrentTier);
            Assert.Contains("tech-computing", civ.ResearchedTechnologyIds);
            Assert.Contains("tech-cybersecurity", civ.ResearchedTechnologyIds);
        }
    }

    [Fact]
    public void SprintMatch_DoesNotJumpToPostSingularityAfterFirstTick()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h);
        services.CreateGameLoop(world).RunTurn();

        foreach (var civ in world.Civilizations)
            Assert.True((int)civ.CurrentTier <= (int)TechTier.EarlyAI);
    }

    [Fact]
    public void ClassicStone_StartsAtPreIndustrial()
    {
        var world = SampleWorldFactory.Create(MatchPresets.ClassicStone);

        foreach (var civ in world.Civilizations)
        {
            Assert.Equal(TechTier.PreIndustrial, civ.CurrentTier);
            Assert.Empty(civ.ResearchedTechnologyIds);
        }
    }

    [Fact]
    public void AwaySummaryStructured_IncludesHeadlineAndBullets()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.DevBlitz3m);
        var loop = services.CreateGameLoop(world);

        loop.RunTurn();
        loop.RunTurn();

        var summary = services.AwaySummary.Build(world, services.TurnHistory, 1, 2);
        var structured = summary.ToStructured(world);

        Assert.False(string.IsNullOrWhiteSpace(structured.Headline));
    }
}
