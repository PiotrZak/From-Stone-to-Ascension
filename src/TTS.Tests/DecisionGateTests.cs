using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class DecisionGateTests
{
    [Fact]
    public void BlockingGate_PausesResearch()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(withDemoGate: true);
        var loop = services.CreateGameLoop(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var startingTechCount = player.ResearchedTechnologyIds.Count;

        var result = loop.RunTurn();

        Assert.Contains(result.ActiveGates, g => g.CivilizationId == player.Id);
        Assert.Equal(startingTechCount, player.ResearchedTechnologyIds.Count);
        Assert.Contains(result.ResearchDecisions, d =>
            d.CivilizationId == player.Id && d.Message.Contains("decision gate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveDecision_UnblocksAndAppliesOption()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(withDemoGate: true);
        var tools = services.CreateToolSurface(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var gate = player.ActiveGate!;
        var stabilityBefore = player.TechnologicalStability;

        var resolution = tools.ResolveDecision(player.Id, gate.Id, "invest");

        Assert.True(resolution.Success);
        Assert.True(gate.IsResolved);
        Assert.False(services.DecisionGates.HasBlockingGate(player));
        Assert.True(player.TechnologicalStability > stabilityBefore);
    }

    [Fact]
    public void ExpiredGate_AppliesDefaultOption()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(withDemoGate: true);
        var loop = services.CreateGameLoop(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var gate = player.ActiveGate!;
        var stabilityBefore = player.TechnologicalStability;

        world.SimulatedNow = gate.ExpiresAt.AddMinutes(1);
        loop.RunTurn(world.SimulatedNow);

        Assert.True(gate.IsResolved);
        Assert.True(gate.WasAutoResolved);
        Assert.Equal("invest", gate.ResolvedOptionId);
        Assert.True(player.TechnologicalStability > stabilityBefore);
    }

    [Fact]
    public void BanForbiddenTech_BlocksFutureResearch()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(withDemoGate: true);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        player.ResearchedTechnologyIds.Add("tech-ml");
        player.CurrentTier = TechTier.InformationAge;

        var gate = new DecisionGate(
            "gate-test-forbidden",
            player.Id,
            GateType.ForbiddenTech,
            "Forbidden AI",
            "Test",
            [
                new DecisionOption("pursue", "Pursue", ""),
                new DecisionOption("ban", "Ban", ""),
                new DecisionOption("delay", "Delay", "")
            ],
            "ban",
            world.SimulatedNow,
            world.SimulatedNow.AddHours(2),
            contextTechnologyId: "tech-recursive-ai");
        player.PendingDecisions.Add(gate);

        services.DecisionGates.Resolve(world, player, gate.Id, "ban", autoResolved: false, services);

        var recursive = world.Technologies.First(t => t.Id == "tech-recursive-ai");
        Assert.False(services.TechTree.CanResearch(player, recursive));
    }

    [Fact]
    public void AwaySummary_FormatsTurnDigest()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(withDemoGate: true);
        var loop = services.CreateGameLoop(world);
        var tools = services.CreateToolSurface(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);

        loop.RunTurn();
        tools.ResolveDecision(player.Id, "gate-demo-start", "invest");

        for (var i = 0; i < 3; i++)
            loop.RunTurn();

        var summary = tools.GetAwaySummary(1, 3);

        Assert.True(summary.Ticks.Count >= 2);
        Assert.Contains("While you were away", summary.Format(world));
    }

    [Fact]
    public void TickScheduler_RespectsInterval()
    {
        var config = MatchPresets.Sprint8h;
        var match = new MatchState("test", config, DateTimeOffset.UtcNow);
        var scheduler = new TickScheduler();
        scheduler.StartMatch(match, match.StartedAt);

        Assert.False(scheduler.ShouldTick(match, match.StartedAt.AddMinutes(30)));
        Assert.True(scheduler.ShouldTick(match, match.NextTickAt));
    }
}
