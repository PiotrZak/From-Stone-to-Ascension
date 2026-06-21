namespace TTS.Core.Models;

public static class PolicyPresets
{
    public static CivilizationPolicy Resolve(string presetId) => presetId.ToLowerInvariant() switch
    {
        "balanced" => CivilizationPolicy.Balanced(),
        "tech-rush" or "techrush" => CivilizationPolicy.TechRush(),
        "stability-first" or "stabilityfirst" => CivilizationPolicy.StabilityFirst(),
        "expansionist" => CivilizationPolicy.Expansionist(),
        "diplomatic" => CivilizationPolicy.Diplomatic(),
        _ => throw new ArgumentException($"Unknown policy preset '{presetId}'.")
    };

    public static IReadOnlyList<(string Id, string Label)> All =>
    [
        ("balanced", "Balanced"),
        ("tech-rush", "Tech Rush"),
        ("stability-first", "Stability First"),
        ("expansionist", "Expansionist"),
        ("diplomatic", "Diplomatic")
    ];
}
