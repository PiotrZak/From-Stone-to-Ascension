namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>How many technologies a civilization can complete per simulation tick.</summary>
public static class ResearchThroughput
{
    public static int SlotsFor(Civilization civilization) =>
        civilization.CurrentTier switch
        {
            TechTier.PreIndustrial or TechTier.Industrial => 2,
            TechTier.EarlyElectronics or TechTier.InformationAge => 3,
            _ => 4
        };
}
