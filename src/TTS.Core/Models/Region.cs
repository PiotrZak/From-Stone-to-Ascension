namespace TTS.Core.Models;

/// <summary>
/// Represents a territory within the world simulation.
/// Regions are the basic geographical units controlled or contested by civilizations.
/// </summary>
public class Region
{
    public string Id { get; }
    public string Name { get; set; }

    /// <summary>Population of this region.</summary>
    public long Population { get; set; }

    /// <summary>Available natural resources (0–100).</summary>
    public double Resources { get; set; }

    /// <summary>Infrastructure development level (0–100).</summary>
    public double Infrastructure { get; set; }

    /// <summary>Controlling civilization, or null if unclaimed.</summary>
    public string? ControllingCivilizationId { get; set; }

    /// <summary>TTS 4+ socioeconomic crime data mapped from state_crime_income_merged.csv.</summary>
    public RegionalCrimeProfile? CrimeProfile { get; set; }

    public Region(string id, string name)
    {
        Id = id;
        Name = name;
        Population = 10_000;
        Resources = 50;
        Infrastructure = 10;
    }
}
