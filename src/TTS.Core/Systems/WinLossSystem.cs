namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Evaluates victory and failure conditions for civilizations.
/// </summary>
public class WinLossSystem
{
    private readonly StabilitySystem _stabilitySystem = new();

    public GameOutcome Evaluate(Civilization civilization, MatchConfig? matchConfig = null)
    {
        if (_stabilitySystem.IsCollapsed(civilization))
            return GameOutcome.Defeat("Civilization collapsed due to instability.");

        if (matchConfig is not null
            && civilization.CurrentTier >= matchConfig.VictoryTier
            && civilization.AverageStability >= matchConfig.VictoryStabilityMin)
        {
            return GameOutcome.Victory(
                $"Reached TTS {(int)matchConfig.VictoryTier} with stability {civilization.AverageStability:F0}.");
        }

        return GameOutcome.InProgress();
    }
}

public readonly record struct GameOutcome(bool IsVictory, bool IsDefeat, string Message)
{
    public bool IsInProgress => !IsVictory && !IsDefeat;

    public static GameOutcome Victory(string message) => new(true, false, message);
    public static GameOutcome Defeat(string message) => new(false, true, message);
    public static GameOutcome InProgress() => new(false, false, "Simulation in progress.");
}
