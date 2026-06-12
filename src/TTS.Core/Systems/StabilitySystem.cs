namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Manages political, economic, and technological stability per civilization.
/// </summary>
public class StabilitySystem
{
    public void ApplyTurnDecay(Civilization civilization, double decayRate = 0.5)
    {
        civilization.PoliticalStability = Clamp(civilization.PoliticalStability - decayRate);
        civilization.EconomicStability = Clamp(civilization.EconomicStability - decayRate);
        civilization.TechnologicalStability = Clamp(civilization.TechnologicalStability - decayRate);
    }

    public void ApplyInstability(Civilization civilization, int riskLevel)
    {
        var penalty = riskLevel * 0.15;
        civilization.PoliticalStability = Clamp(civilization.PoliticalStability - penalty);
        civilization.EconomicStability = Clamp(civilization.EconomicStability - penalty * 0.5);
        civilization.TechnologicalStability = Clamp(civilization.TechnologicalStability - penalty);
    }

    public void ApplyEventImpact(Civilization civilization, GlobalEvent globalEvent)
    {
        var impact = globalEvent.Severity * 2.0;
        civilization.PoliticalStability = Clamp(civilization.PoliticalStability - impact);
        civilization.EconomicStability = Clamp(civilization.EconomicStability - impact * 0.75);
        civilization.TechnologicalStability = Clamp(civilization.TechnologicalStability - impact * 0.5);
    }

    public bool IsCollapsed(Civilization civilization) => civilization.AverageStability <= 10;

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);
}
