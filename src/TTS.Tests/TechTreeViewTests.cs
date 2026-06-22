using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class TechTreeViewTests
{
    [Fact]
    public void TechTreeViewBuilder_MarksStartingTechAvailable()
    {
        var world = SampleWorldFactory.Create(MatchPresets.ClassicStone);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var services = new SimulationServices();

        var nodes = TechTreeViewBuilder.Build(player, world, services.TechTree);
        var agriculture = nodes.First(n => n.Id == "tech-agriculture");

        Assert.Equal("available", agriculture.Status);
    }

    [Fact]
    public void ResearchThroughput_ScalesWithTier()
    {
        var civ = new Civilization("civ", "Test");
        Assert.Equal(2, ResearchThroughput.SlotsFor(civ));

        civ.CurrentTier = TechTier.InformationAge;
        Assert.Equal(3, ResearchThroughput.SlotsFor(civ));

        civ.CurrentTier = TechTier.EarlyAI;
        Assert.Equal(4, ResearchThroughput.SlotsFor(civ));
    }
}
