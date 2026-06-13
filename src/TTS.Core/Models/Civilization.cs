namespace TTS.Core.Models;

/// <summary>
/// A civilization competing or cooperating in the world simulation.
/// </summary>
public class Civilization
{
    public string Id { get; }
    public string Name { get; set; }
    public bool IsPlayerControlled { get; set; }
    public TechTier CurrentTier { get; set; }

    /// <summary>Political stability (0–100).</summary>
    public double PoliticalStability { get; set; }

    /// <summary>Economic stability (0–100).</summary>
    public double EconomicStability { get; set; }

    /// <summary>Technological stability (0–100).</summary>
    public double TechnologicalStability { get; set; }

    public HashSet<string> ResearchedTechnologyIds { get; } = new(StringComparer.Ordinal);
    public List<string> ControlledRegionIds { get; } = [];
    public List<Faction> Factions { get; } = [];
    public CivilizationPolicy Policy { get; set; } = CivilizationPolicy.Balanced();

    public Civilization(string id, string name, bool isPlayerControlled = false)
    {
        Id = id;
        Name = name;
        IsPlayerControlled = isPlayerControlled;
        CurrentTier = TechTier.PreIndustrial;
        PoliticalStability = 70;
        EconomicStability = 70;
        TechnologicalStability = 70;
    }

    public double AverageStability =>
        (PoliticalStability + EconomicStability + TechnologicalStability) / 3.0;
}
