namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Evaluates victory and failure conditions for civilizations.
/// </summary>
public class WinLossSystem
{
    private readonly StabilitySystem _stabilitySystem = new();

    public GameOutcome Evaluate(Civilization civilization)
    {
        if (_stabilitySystem.IsCollapsed(civilization))
            return GameOutcome.Defeat("Civilization collapsed due to instability.");

        if (civilization.CurrentTier >= TechTier.BioNano && civilization.AverageStability >= 60)
            return GameOutcome.Victory("Achieved a stable Bio/Nano civilization.");

        if (civilization.CurrentTier >= TechTier.PostSingularity && civilization.TechnologicalStability >= 50)
            return GameOutcome.Victory("Survived the post-singularity transition.");

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
