using TTS.Core.Models;
using TTS.Core.Systems;

namespace TTS.Tests;

public class WinLossTests
{
    [Fact]
    public void Evaluate_UsesMatchVictoryThreshold()
    {
        var config = MatchPresets.DevBlitz3m;
        var civ = new Civilization("civ-test", "Test")
        {
            CurrentTier = config.VictoryTier,
            PoliticalStability = 60,
            EconomicStability = 60,
            TechnologicalStability = 60
        };

        var outcome = new WinLossSystem().Evaluate(civ, config);

        Assert.True(outcome.IsVictory);
    }

    [Fact]
    public void Evaluate_BelowStabilityThreshold_IsInProgress()
    {
        var config = MatchPresets.DevBlitz3m;
        var civ = new Civilization("civ-test", "Test")
        {
            CurrentTier = config.VictoryTier,
            PoliticalStability = 30,
            EconomicStability = 30,
            TechnologicalStability = 30
        };

        var outcome = new WinLossSystem().Evaluate(civ, config);

        Assert.True(outcome.IsInProgress);
    }

    [Fact]
    public void Evaluate_Collapse_IsDefeat()
    {
        var civ = new Civilization("civ-test", "Test")
        {
            PoliticalStability = 0,
            EconomicStability = 0,
            TechnologicalStability = 0
        };

        var outcome = new WinLossSystem().Evaluate(civ, MatchPresets.DevBlitz3m);

        Assert.True(outcome.IsDefeat);
    }
}
