using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class PrototypePatternTests
{
    [Fact]
    public void TechTreeViewBuilder_ReusesCachedStructure()
    {
        var world = SampleWorldFactory.Create(MatchPresets.ClassicStone);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var services = new SimulationServices();

        var first = TechTreeViewBuilder.Build(player, world, services.TechTree);
        var second = TechTreeViewBuilder.Build(player, world, services.TechTree);

        Assert.Equal(first.Count, second.Count);
        Assert.Same(first[0].Prerequisites, second[0].Prerequisites);
    }

    [Fact]
    public void DecisionGate_ReusesOptionTemplates()
    {
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, withDemoGate: true);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var gate = player.PendingDecisions.Single();

        Assert.Same(GateOptionTemplates.DemoCrimePressure, gate.Options);
    }

    [Fact]
    public void WorldBlueprint_CreatesStandardArena()
    {
        var world = new WorldState();
        var (player, rival) = WorldBlueprint.ApplyStandardArena(world);

        Assert.Equal("Aurora Collective", player.Name);
        Assert.Equal("Iron Dominion", rival.Name);
        Assert.Equal(2, world.Civilizations.Count);
        Assert.Equal(2, world.Regions.Count);
        Assert.True(world.Technologies.Count > 0);
        Assert.Equal(2, world.KnowledgeNetworks.Count);
        Assert.NotSame(player.Policy, rival.Policy);
    }
}
