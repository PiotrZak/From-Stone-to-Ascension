namespace TTS.Core.Models;

/// <summary>How a civilization prioritizes research branches.</summary>
public enum ResearchStance
{
    Balanced,
    TechRush,
    StabilityFirst,
    Expansionist
}

/// <summary>Willingness to pursue risky or forbidden technologies.</summary>
public enum RiskTolerance
{
    Low,
    Medium,
    High
}

/// <summary>Default diplomatic posture (used in later phases).</summary>
public enum DiplomacyStance
{
    Cooperative,
    Neutral,
    Aggressive
}

/// <summary>
/// Governance policy driving autonomous research and future auto-mode behavior.
/// </summary>
public class CivilizationPolicy
{
    public ResearchStance Research { get; set; } = ResearchStance.Balanced;
    public RiskTolerance Risk { get; set; } = RiskTolerance.Medium;
    public DiplomacyStance Diplomacy { get; set; } = DiplomacyStance.Neutral;
    public Dictionary<string, double> BranchWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static CivilizationPolicy Balanced() => new()
    {
        Research = ResearchStance.Balanced,
        Risk = RiskTolerance.Medium,
        Diplomacy = DiplomacyStance.Neutral,
        BranchWeights = UniformWeights(1.0)
    };

    public static CivilizationPolicy TechRush() => new()
    {
        Research = ResearchStance.TechRush,
        Risk = RiskTolerance.High,
        Diplomacy = DiplomacyStance.Neutral,
        BranchWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["computing"] = 1.5,
            ["ai"] = 2.0,
            ["energy"] = 1.2,
            ["manufacturing"] = 1.0,
            ["agriculture"] = 0.5
        }
    };

    public static CivilizationPolicy StabilityFirst() => new()
    {
        Research = ResearchStance.StabilityFirst,
        Risk = RiskTolerance.Low,
        Diplomacy = DiplomacyStance.Cooperative,
        BranchWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["agriculture"] = 1.5,
            ["manufacturing"] = 1.2,
            ["energy"] = 1.0,
            ["ai"] = 0.3
        }
    };

    public static CivilizationPolicy Expansionist() => new()
    {
        Research = ResearchStance.Expansionist,
        Risk = RiskTolerance.Medium,
        Diplomacy = DiplomacyStance.Neutral,
        BranchWeights = UniformWeights(1.0)
    };

    public static CivilizationPolicy Diplomatic() => new()
    {
        Research = ResearchStance.Balanced,
        Risk = RiskTolerance.Low,
        Diplomacy = DiplomacyStance.Cooperative,
        BranchWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["communication"] = 1.5,
            ["computing"] = 1.2,
            ["agriculture"] = 1.0
        }
    };

    private static Dictionary<string, double> UniformWeights(double weight) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["agriculture"] = weight,
            ["manufacturing"] = weight,
            ["energy"] = weight,
            ["communication"] = weight,
            ["computing"] = weight,
            ["ai"] = weight
        };
}
