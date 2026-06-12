namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Handles early unlock of forbidden technologies and their instability cost.
/// </summary>
public class ForbiddenTechSystem
{
    private readonly StabilitySystem _stabilitySystem = new();

    public bool IsEarlyUnlock(Civilization civilization, Technology technology) =>
        technology.IsForbidden && (int)technology.Tier > (int)civilization.CurrentTier;

    public void ApplyForbiddenResearch(Civilization civilization, Technology technology)
    {
        var extraRisk = IsEarlyUnlock(civilization, technology)
            ? technology.RiskLevel + 20
            : technology.RiskLevel;

        _stabilitySystem.ApplyInstability(civilization, extraRisk);
    }

    public bool TriggersParadoxRisk(Civilization civilization, Technology technology) =>
        technology.Category == TechCategory.TemporalManipulation && civilization.TechnologicalStability < 40;
}
