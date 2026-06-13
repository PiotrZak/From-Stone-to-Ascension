namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>Maps technology categories and fusion tags to policy branch keys.</summary>
public static class TechBranchMapping
{
    public static string CategoryToBranch(TechCategory category) => category switch
    {
        TechCategory.Agriculture => "agriculture",
        TechCategory.Manufacturing => "manufacturing",
        TechCategory.Energy => "energy",
        TechCategory.Communication => "communication",
        TechCategory.Computing => "computing",
        TechCategory.Military => "military",
        TechCategory.Biology => "biology",
        TechCategory.Nanotechnology => "nano",
        TechCategory.ArtificialIntelligence => "ai",
        TechCategory.TemporalManipulation => "temporal",
        TechCategory.RealityEngineering => "reality",
        _ => "general"
    };

    public static TechnologyBranchInfo Describe(Technology technology) =>
        new(technology.Category, CategoryToBranch(technology.Category), technology.FusionTags);
}

public readonly record struct TechnologyBranchInfo(
    TechCategory Category,
    string Branch,
    IReadOnlyList<string> FusionTags);
