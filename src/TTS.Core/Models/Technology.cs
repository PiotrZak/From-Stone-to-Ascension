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
/// A single node in a tier's technology sub-tree.
/// </summary>
public class Technology
{
    public string Id { get; }
    public string Name { get; set; }
    public TechTier Tier { get; set; }
    public TechCategory Category { get; set; }

    /// <summary>IDs of prerequisite technologies (1–3 expected).</summary>
    public IReadOnlyList<string> Prerequisites { get; }

    /// <summary>Instability risk when researched (0–100).</summary>
    public int RiskLevel { get; set; }

    /// <summary>Forbidden tech can be unlocked early but destabilizes society.</summary>
    public bool IsForbidden { get; set; }

    /// <summary>Tags used for fusion node generation across tiers.</summary>
    public IReadOnlyList<string> FusionTags { get; }

    /// <summary>Core / branch / forbidden / fusion within the tier sub-tree.</summary>
    public TechNodeRole Role { get; set; }

    public Technology(
        string id,
        string name,
        TechTier tier,
        TechCategory category,
        IEnumerable<string>? prerequisites = null,
        int riskLevel = 0,
        bool isForbidden = false,
        IEnumerable<string>? fusionTags = null,
        TechNodeRole role = TechNodeRole.Branch)
    {
        Id = id;
        Name = name;
        Tier = tier;
        Category = category;
        Prerequisites = (prerequisites ?? []).ToList();
        RiskLevel = riskLevel;
        IsForbidden = isForbidden;
        FusionTags = (fusionTags ?? []).ToList();
        Role = isForbidden ? TechNodeRole.Forbidden : role;
    }
}
