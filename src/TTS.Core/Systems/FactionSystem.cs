namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Simulates internal faction influence on research direction and stability.
/// </summary>
public class FactionSystem
{
    public double GetAccelerationPressure(Civilization civilization)
    {
        if (civilization.Factions.Count == 0)
            return 0;

        return civilization.Factions.Sum(f => f.Stance switch
        {
            FactionStance.Accelerationist => f.Influence * 0.01,
            FactionStance.Preservationist => f.Influence * -0.01,
            _ => 0
        });
    }

    public void ApplyTurnInfluence(Civilization civilization)
    {
        var pressure = GetAccelerationPressure(civilization);

        if (pressure > 0.5)
            civilization.TechnologicalStability = Math.Clamp(civilization.TechnologicalStability - pressure, 0, 100);
        else if (pressure < -0.5)
            civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability - Math.Abs(pressure), 0, 100);
    }

    public IReadOnlyDictionary<string, double> GetFactionTensions(Civilization civilization)
    {
        return civilization.Factions.ToDictionary(
            f => f.Id,
            f => f.Stance switch
            {
                FactionStance.Accelerationist => f.Influence * 0.6,
                FactionStance.Preservationist => f.Influence * 0.8,
                _ => f.Influence * 0.3
            });
    }
}
