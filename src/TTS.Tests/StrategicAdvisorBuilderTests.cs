using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class StrategicAdvisorBuilderTests
{
    [Fact]
    public void BuildClassical_IncludesRecommendedTechAndHeadline()
    {
        var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, Path.GetTempFileName());
        var player = host.World.Civilizations.First(c => c.Id == "civ-player");
        player.CurrentTier = TechTier.InformationAge;

        var tools = host.CreateToolSurface();
        var briefing = StrategicAdvisorBuilder.BuildClassical(player, tools);

        Assert.False(string.IsNullOrWhiteSpace(briefing.Headline));
        Assert.NotEmpty(briefing.Highlights);
        Assert.False(string.IsNullOrWhiteSpace(briefing.Briefing));
        Assert.Equal("classical", briefing.Source);
    }

    [Fact]
    public void FromLlmText_ParsesHeadlineAndBullets()
    {
        var text = """
            Cybercrime spike threatens coastal hubs
            • Crime pressure elevated in two regions
            • Recommend: shift to stability-first policy
            Recommend: prioritize Cybersecurity Mesh next.
            """;

        var analysis = new PolicyResearchAnalysis(
            ResearchStance.Balanced,
            RiskTolerance.Medium,
            new Dictionary<string, double> { ["Computing"] = 0.4 },
            [],
            null);

        var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, Path.GetTempFileName());
        var player = host.World.Civilizations.First(c => c.Id == "civ-player");
        var tools = host.CreateToolSurface();

        var briefing = StrategicAdvisorBuilder.FromLlmText(text, player, tools, analysis);

        Assert.Contains("Cybercrime", briefing.Headline);
        Assert.Contains(briefing.Highlights, h => h.Contains("Crime pressure"));
        Assert.Equal("llm-tools", briefing.Source);
    }
}
