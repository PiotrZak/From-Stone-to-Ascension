namespace TTS.Core.Models;

/// <summary>How knowledge spreads between civilizations.</summary>
public enum DiffusionChannel
{
    Trade,
    Espionage,
    OpenScience,
    AiNetwork
}

/// <summary>
/// Tracks technology knowledge flow between civilizations.
/// Technology can be known, classified, corrupted, or lost.
/// </summary>
public class KnowledgeNetwork
{
    public string SourceCivilizationId { get; }
    public string TargetCivilizationId { get; }
    public DiffusionChannel Channel { get; set; }

    /// <summary>Technology IDs known to the target via this link.</summary>
    public HashSet<string> KnownTechnologyIds { get; } = new(StringComparer.Ordinal);

    /// <summary>Strength of the knowledge link (0–100).</summary>
    public double Strength { get; set; }

    public KnowledgeNetwork(string sourceCivilizationId, string targetCivilizationId, DiffusionChannel channel)
    {
        SourceCivilizationId = sourceCivilizationId;
        TargetCivilizationId = targetCivilizationId;
        Channel = channel;
        Strength = 30;
    }
}
