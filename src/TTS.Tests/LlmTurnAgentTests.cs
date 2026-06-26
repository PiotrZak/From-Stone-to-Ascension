using TTS.Core;
using TTS.Core.Simulation;
using TTS.Llm;

namespace TTS.Tests;

public class LlmTurnAgentTests
{
    [Fact]
    public void BuildRivals_ExcludesSelf()
    {
        var world = SampleWorldFactory.Create();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var rivals = LlmTurnAgent.BuildRivals(world, player.Id);

        Assert.Single(rivals);
        Assert.DoesNotContain(rivals, r => r.Id == player.Id);
        Assert.Contains(rivals, r => r.Id == "civ-rival");
    }

    [Fact]
    public void AgentSessionLimits_HasSaneDefaults()
    {
        var limits = AgentSessionLimits.FromEnvironment();
        Assert.True(limits.TurnTimeout.TotalSeconds >= 5);
        Assert.True(limits.MaxToolRounds >= 1);
        Assert.True(limits.MaxTurnCallsPerMatchTick >= 1);
        Assert.True(limits.MaxAdvisorCallsPerMatchTick >= 1);
    }
}
