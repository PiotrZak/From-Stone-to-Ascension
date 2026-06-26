using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class GateAdvisorLogicTests
{
    [Fact]
    public void BuildFocus_RecommendsInvest_ForCrimeGateUnderStabilityPolicy()
    {
        var path = Path.GetTempFileName();
        var host = MatchHost.CreateNew(MatchPresets.Sprint8h, path, withDemoGate: true);
        var player = host.World.Civilizations.First(c => c.IsPlayerControlled);
        player.Policy = CivilizationPolicy.StabilityFirst();

        var focus = GateAdvisorLogic.BuildFocus(player, host.CreateToolSurface());

        Assert.NotNull(focus);
        Assert.Equal(GateType.CrimePressure, focus!.Type);
        Assert.Equal("invest", focus.RecommendedOptionId);
        Assert.Contains(focus.Options, o => o.Stance == "recommended" && o.OptionId == "invest");
    }

    [Fact]
    public void BuildClassical_PrioritizesGateInHeadlineWhenPending()
    {
        var path = Path.GetTempFileName();
        var host = MatchHost.CreateNew(MatchPresets.Sprint8h, path, withDemoGate: true);
        var player = host.World.Civilizations.First(c => c.IsPlayerControlled);

        var briefing = StrategicAdvisorBuilder.BuildClassical(player, host.CreateToolSurface());

        Assert.NotNull(briefing.GateFocus);
        Assert.Contains("Resolve gate", briefing.Headline);
        Assert.Contains("invest", briefing.GateFocus!.RecommendedOptionId);
    }
}
