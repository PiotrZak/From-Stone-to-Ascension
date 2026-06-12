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

    /// <summary>Pollution level introduced at TTS 2+ (0–100).</summary>
    public double Pollution { get; set; }

    /// <summary>The civilization that currently controls this region.</summary>
    public Civilization? Controller { get; set; }

    public Region(string id, string name, long population, double resources)
    {
        Id = id;
        Name = name;
        Population = population;
        Resources = Math.Clamp(resources, 0, 100);
        Infrastructure = 10;
        Pollution = 0;
    }

    public override string ToString() => $"{Name} (Pop: {Population:N0}, Res: {Resources:F1})";
}
