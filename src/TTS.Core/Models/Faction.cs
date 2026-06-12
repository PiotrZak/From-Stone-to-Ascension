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

    /// <summary>Faction actively suppresses advancement or forbids certain tech.</summary>
    Conservative
}

/// <summary>
/// An internal political group within a civilization.
/// Factions compete for control of tech direction, can merge or split,
/// and may accelerate or suppress TTS progression.
/// </summary>
public class Faction
{
    public string Id { get; }
    public string Name { get; set; }
    public FactionType Type { get; set; }
    public FactionStance Stance { get; set; }

    /// <summary>Influence level within the parent civilization (0–100).</summary>
    public double Influence { get; set; }

    /// <summary>The minimum TTS level at which this faction appears.</summary>
    public TechTier MinTier { get; set; }

    /// <summary>Tracks whether this faction has merged into another.</summary>
    public bool IsDisbanded { get; set; }

    public Faction(string id, string name, FactionType type, FactionStance stance, double influence, TechTier minTier = TechTier.PreIndustrial)
    {
        Id = id;
        Name = name;
        Type = type;
        Stance = stance;
        Influence = Math.Clamp(influence, 0, 100);
        MinTier = minTier;
        IsDisbanded = false;
    }

    public override string ToString() => $"{Name} [{Type}/{Stance}] Influence: {Influence:F1}";
}
