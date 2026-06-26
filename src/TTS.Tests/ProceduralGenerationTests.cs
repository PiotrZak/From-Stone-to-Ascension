using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class ProceduralGenerationTests
{
    [Fact]
    public void ProceduralGenerator_IsDeterministicForSameSeed()
    {
        var options = new WorldGenerationOptions
        {
            Seed = 4242,
            CivilizationCount = 2,
            UseCrimeDataAnchors = true
        };

        var first = Generate(options);
        var second = Generate(options);

        Assert.Equal(first.Player.Name, second.Player.Name);
        Assert.Equal(first.Rival.Name, second.Rival.Name);
        Assert.Equal(first.PlayerRegion.Name, second.PlayerRegion.Name);
        Assert.Equal(first.RivalRegion.Name, second.RivalRegion.Name);
        Assert.Equal(first.PlayerRegion.CrimeProfile?.SourceState, second.PlayerRegion.CrimeProfile?.SourceState);
    }

    [Fact]
    public void ProceduralGenerator_VariesBySeed()
    {
        var a = Generate(new WorldGenerationOptions { Seed = 1, CivilizationCount = 2 });
        var b = Generate(new WorldGenerationOptions { Seed = 9999, CivilizationCount = 2 });

        Assert.NotEqual(a.Player.Name, b.Player.Name);
    }

    [Fact]
    public void SampleWorldFactory_UsesProceduralByDefault()
    {
        var world = SampleWorldFactory.Create(MatchPresets.DevBlitz3m, matchId: "match-proc-default");
        var player = world.Civilizations.First(c => c.Id == WorldBlueprint.PlayerCivId);

        Assert.NotEqual("Aurora Collective", player.Name);
        Assert.True(player.Factions.Count >= 1);
        Assert.True(world.KnowledgeNetworks.Count >= 2);
        Assert.True(world.Match!.WorldSeed > 0);
    }

    [Fact]
    public void StandardArena_PreservesFixedDemoNames()
    {
        var world = SampleWorldFactory.Create(MatchPresets.DevBlitz3m, matchId: "match-standard", useStandardArena: true);
        var player = world.Civilizations.First(c => c.Id == WorldBlueprint.PlayerCivId);
        var rival = world.Civilizations.First(c => c.Id == WorldBlueprint.RivalCivId);

        Assert.Equal("Aurora Collective", player.Name);
        Assert.Equal("Iron Dominion", rival.Name);
    }

    [Fact]
    public void CustomSeed_OverridesMatchIdHash()
    {
        var world = SampleWorldFactory.Create(
            MatchPresets.DevBlitz3m,
            matchId: "match-custom-seed",
            worldSeed: 777);

        Assert.Equal(777, world.Match!.WorldSeed);
    }

    [Fact]
    public void SeededSimulationServices_ProducesDeterministicEventRolls()
    {
        var world = SampleWorldFactory.Create(MatchPresets.DevBlitz3m, matchId: "match-events", worldSeed: 55);
        world.Civilizations.First().CurrentTier = TechTier.EarlyAI;

        var servicesA = new SimulationServices(55);
        var servicesB = new SimulationServices(55);

        var eventA = servicesA.GlobalEvents.MaybeGenerateEvent(world);
        var eventB = servicesB.GlobalEvents.MaybeGenerateEvent(world);

        Assert.Equal(eventA?.Name, eventB?.Name);
    }

    private static (Civilization Player, Civilization Rival, Region PlayerRegion, Region RivalRegion) Generate(
        WorldGenerationOptions options)
    {
        var world = new WorldState();
        var (player, rival) = new ProceduralWorldGenerator().Generate(world, options);
        var playerRegion = world.Regions.First(r => r.ControllingCivilizationId == player.Id);
        var rivalRegion = world.Regions.First(r => r.ControllingCivilizationId == rival.Id);
        return (player, rival, playerRegion, rivalRegion);
    }
}
