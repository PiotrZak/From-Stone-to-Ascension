namespace TTS.Core.Models;

/// <summary>Type classification for factions within a civilization.</summary>
public enum FactionType
{
    Government,
    Corporation,
    ReligiousGroup,
    AiCollective,
    UndergroundResistance
}

/// <summary>How a faction influences TTS progression.</summary>
public enum FactionStance
{
    /// <summary>Faction actively pushes for faster tech advancement.</summary>
    Accelerationist,

    /// <summary>Faction is neutral and reacts to events.</summary>
    Neutral,

    /// <summary>Faction resists or suppresses TTS advancement.</summary>
    Preservationist
}

/// <summary>
/// Internal political group within a civilization that competes for tech direction.
/// </summary>
public class Faction
{
    public string Id { get; }
    public string Name { get; set; }
    public string CivilizationId { get; }
    public FactionType Type { get; set; }
    public FactionStance Stance { get; set; }

    /// <summary>Political influence within the civilization (0–100).</summary>
    public double Influence { get; set; }

    public Faction(string id, string name, string civilizationId, FactionType type, FactionStance stance)
    {
        Id = id;
        Name = name;
        CivilizationId = civilizationId;
        Type = type;
        Stance = stance;
        Influence = 25;
    }
}
