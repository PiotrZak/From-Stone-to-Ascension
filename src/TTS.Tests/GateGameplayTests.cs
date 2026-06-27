using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class GateGameplayTests
{
    [Fact]
    public void CrimeGate_Invest_ReducesRegionCrimePressure()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, withDemoGate: true);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var gate = player.ActiveGate!;
        Assert.Equal(GateType.CrimePressure, gate.Type);
        Assert.NotNull(gate.ContextRegionId);

        var region = world.Regions.First(r => r.Id == gate.ContextRegionId);
        Assert.NotNull(region.CrimeProfile);
        var beforeInfrastructure = region.Infrastructure;
        var techBefore = player.TechnologicalStability;
        region.CrimeProfile!.CrimePressureOffset = 0;

        var result = services.DecisionGates.Resolve(world, player, gate.Id, "invest", autoResolved: false, services);

        Assert.True(result.Success);
        Assert.Equal(-12, region.CrimeProfile.CrimePressureOffset);
        Assert.Equal(beforeInfrastructure + 3, region.Infrastructure);
        Assert.True(player.TechnologicalStability > techBefore);
    }

    [Fact]
    public void FactionGate_Appease_IncreasesFactionInfluence()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.ClassicStone, withDemoGate: true);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var gate = player.ActiveGate!;
        var faction = player.Factions.First(f => f.Id == gate.ContextFactionId);
        var before = faction.Influence;

        services.DecisionGates.Resolve(world, player, gate.Id, "appease", autoResolved: false, services);

        Assert.True(faction.Influence > before);
    }

    [Fact]
    public void ForbiddenTech_Delay_AllowsGateToReopen()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, withDemoGate: false);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        player.ResearchedTechnologyIds.Add("tech-ml");
        player.CurrentTier = TechTier.InformationAge;

        var gate = new DecisionGate(
            "gate-test-forbidden",
            player.Id,
            GateType.ForbiddenTech,
            "Forbidden AI",
            "Test",
            GateOptionTemplates.ForbiddenTech,
            "ban",
            world.SimulatedNow,
            world.SimulatedNow.AddHours(2),
            contextTechnologyId: "tech-recursive-ai");
        player.PendingDecisions.Add(gate);
        player.OfferedGateKeys.Add("forbidden:tech-recursive-ai");

        services.DecisionGates.Resolve(world, player, gate.Id, "delay", autoResolved: false, services);

        Assert.DoesNotContain("forbidden:tech-recursive-ai", player.OfferedGateKeys);
    }

    [Fact]
    public void ScanAfterTurn_StopsOpeningWhenThreeGatesPending()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, withDemoGate: false);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var now = world.SimulatedNow;

        for (var i = 0; i < 3; i++)
        {
            player.PendingDecisions.Add(new DecisionGate(
                $"gate-manual-{i}",
                player.Id,
                GateType.FactionCrisis,
                $"Manual gate {i}",
                "Test queue cap",
                GateOptionTemplates.FactionCrisis,
                "appease",
                now,
                now.AddHours(1)));
        }

        services.BeginTurn(world);
        services.DecisionGates.ScanAfterTurn(world, services, services.CurrentTurnSnapshot!);

        Assert.Equal(3, services.DecisionGates.GetPendingGates(player).Count);
    }
}
