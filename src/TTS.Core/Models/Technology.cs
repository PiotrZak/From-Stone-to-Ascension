namespace TTS.Core.Models;

/// <summary>Category of a technology node within a tier's sub-tree.</summary>
public enum TechCategory
{
    Agriculture,
    Manufacturing,
    Energy,
    Communication,
    Computing,
    Military,
    Biology,
    Nanotechnology,
    ArtificialIntelligence,
    TemporalManipulation,
    RealityEngineering
}

/// <summary>
/// A single research node within a TTS tier's technology sub-tree.
/// Each tier has its own sub-tree; advancing tiers unlocks new categories.
/// </summary>
public class Technology
{
    public string Id { get; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>The tier that must be active to research this technology.</summary>
    public TechTier RequiredTier { get; set; }

    public TechCategory Category { get; set; }

    /// <summary>Research cost in abstract "research points".</summary>
    public int ResearchCost { get; set; }

    /// <summary>IDs of technologies that must be researched first.</summary>
    public IReadOnlyList<string> Prerequisites { get; set; }

    /// <summary>Whether this technology is considered "forbidden" (unlockable early at risk).</summary>
    public bool IsForbidden { get; set; }

    /// <summary>Extra instability added to the civilization when this is researched.</summary>
    public double InstabilityPenalty { get; set; }

    /// <summary>Whether this technology has been researched by the owning civilization.</summary>
    public bool IsResearched { get; set; }

    public Technology(
        string id,
        string name,
        string description,
        TechTier requiredTier,
        TechCategory category,
        int researchCost,
        IReadOnlyList<string>? prerequisites = null,
        bool isForbidden = false,
        double instabilityPenalty = 0)
    {
        Id = id;
        Name = name;
        Description = description;
        RequiredTier = requiredTier;
        Category = category;
        ResearchCost = researchCost;
        Prerequisites = prerequisites ?? Array.Empty<string>();
        IsForbidden = isForbidden;
        InstabilityPenalty = instabilityPenalty;
        IsResearched = false;
    }

    public override string ToString() =>
        $"[TTS{(int)RequiredTier}] {Name}{(IsForbidden ? " ⚠️FORBIDDEN" : string.Empty)}";
}
