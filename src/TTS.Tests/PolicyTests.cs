using TTS.Core.Models;
using TTS.Core.Simulation;

namespace TTS.Tests;

public class PolicyTests
{
    [Fact]
    public void UpdatePolicy_AppliesPreset()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-policy-{Guid.NewGuid():N}.json");
        var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path);
        var player = host.World.Civilizations.First(c => c.IsPlayerControlled);

        host.UpdatePolicy(player.Id, "tech-rush");

        Assert.Equal(ResearchStance.TechRush, player.Policy.Research);
        Assert.Equal(RiskTolerance.High, player.Policy.Risk);
    }

    [Fact]
    public void GetCivDashboard_ReturnsRecommendedTech()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-dash-{Guid.NewGuid():N}.json");
        var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path);
        var player = host.World.Civilizations.First(c => c.IsPlayerControlled);
        var tools = host.CreateToolSurface();

        var analysis = tools.GetPolicyResearchAnalysis(player.Id);

        Assert.NotNull(analysis.Recommended);
    }
}
