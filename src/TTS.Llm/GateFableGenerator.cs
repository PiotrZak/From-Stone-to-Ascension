namespace TTS.Llm;

using TTS.Core.Models;

public sealed class GateFableGenerator(OllamaClient? client = null)
{
    private const int SciFiTierThreshold = (int)TechTier.EarlyAI;

    private readonly OllamaClient _client = client ?? new OllamaClient();

    private static readonly Dictionary<int, string> TierNames = new()
    {
        [1] = "Pre-Industrial",
        [2] = "Industrial Age",
        [3] = "Early Electronics",
        [4] = "Information Age",
        [5] = "Early AI Age",
        [6] = "Bio-Nano Era",
        [7] = "Temporal Horizon",
        [8] = "Post-Singularity"
    };

    public async Task<string?> GenerateAsync(
        string civilizationName,
        GateType type,
        string title,
        string description,
        int civilizationTier,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldEnrich(type, civilizationTier))
            return null;

        var narrativeTier = type == GateType.TierAdvancement
            ? ParseTierFromTitle(title)
            : civilizationTier;

        var (system, user) = BuildPrompt(civilizationName, type, title, description, narrativeTier);
        return await _client.TryChatAsync(system, user, cancellationToken);
    }

    /// <summary>Gate types that require a minimum TTS tier before Ollama replaces hardcoded text.</summary>
    public static bool ShouldEnrich(GateType type, int civilizationTier) => type switch
    {
        GateType.CrimePressure => civilizationTier >= (int)TechTier.InformationAge,
        GateType.AiAlignment => civilizationTier >= (int)TechTier.EarlyAI,
        GateType.ForbiddenTech => civilizationTier >= (int)TechTier.EarlyElectronics,
        _ => true
    };

    private static (string System, string User) BuildPrompt(
        string civilizationName,
        GateType type,
        string title,
        string description,
        int narrativeTier)
    {
        var era = TierNames.GetValueOrDefault(narrativeTier, $"TTS {narrativeTier}");
        var sciFi = IsSciFiTier(narrativeTier);

        var baseContext = BuildContext(civilizationName, type, title, description, narrativeTier, era);

        var style = StyleBlock(sciFi);
        var setting = sciFi
            ? "a speculative sci-fi civilization strategy game (TTS 5+)"
            : "a realistic historical civilization strategy game (pre-AI eras)";

        var system = type switch
        {
            GateType.TierAdvancement => sciFi
                ? "You write era-transition briefings for a near-future sci-fi strategy game."
                : "You write era-transition briefings for a realistic historical strategy game.",
            GateType.GlobalCrisis => $"You write crisis briefings for {setting}.",
            GateType.ForbiddenTech => sciFi
                ? "You write briefings about dangerous forbidden research in a sci-fi strategy game."
                : "You write briefings about controversial or premature research in a historical strategy game.",
            GateType.FactionCrisis => $"You write internal political crisis briefings for {setting}.",
            GateType.CrimePressure => $"You write crime and public-order briefings for {setting}.",
            GateType.AiAlignment => "You write AI governance crisis briefings for a sci-fi strategy game.",
            _ => $"You write decision-gate briefings for {setting}."
        };

        var task = type switch
        {
            GateType.TierAdvancement => sciFi
                ? "Describe how this civilization enters a new technological era. Focus on societal disruption and governance choices."
                : "Describe how this civilization enters a new historical era. Use grounded, real-world tone — mills, cities, wires, networks, not spaceships or myth.",
            GateType.GlobalCrisis => sciFi
                ? "Write a crisis briefing with plausible near-future stakes."
                : "Write a crisis briefing grounded in real historical pressures — shortages, unrest, industrial shocks.",
            GateType.ForbiddenTech => sciFi
                ? "Frame the temptation and danger of this forbidden research line."
                : "Frame the ethical and stability risk of pursuing this technology too early.",
            GateType.FactionCrisis => "Describe faction unrest and pressure on leadership.",
            GateType.CrimePressure => sciFi
                ? "Describe rising crime pressure and strain on governance in the near-future city named in the title."
                : "Describe public disorder in the named city using period-appropriate language (policing, poverty, unrest). Do not name US states or real 21st-century jurisdictions.",
            GateType.AiAlignment => "Describe autonomous systems challenging governance and alignment.",
            _ => "Summarize the decision facing leadership."
        };

        var user = type == GateType.TierAdvancement
            ? BuildTierPrompt(civilizationName, title, description, narrativeTier, era, sciFi) + "\n\n" + task + "\n" + style
            : baseContext + "\n\n" + task + "\n" + style;

        return (system, user);
    }

    private static string BuildTierPrompt(
        string civilizationName,
        string title,
        string description,
        int tier,
        string era,
        bool sciFi)
    {
        var tone = sciFi
            ? "This is a transition into a speculative near-future era."
            : "This is a transition into a historical era — keep it realistic and earthbound.";

        return $"""
            Civilization: {civilizationName}
            New era: TTS {tier} — {era}
            Context: {description}
            {tone}
            """;
    }

    private static string BuildContext(
        string civilizationName,
        GateType type,
        string title,
        string description,
        int narrativeTier,
        string era)
    {
        if (type == GateType.CrimePressure)
        {
            return $"""
                Civilization: {civilizationName}
                Era: TTS {narrativeTier} — {era}
                Situation: {title}
                Use only the in-game city name from the title. Do not reference California, US states, or CSV statistics.
                """;
        }

        return $"""
            Civilization: {civilizationName}
            Era: TTS {narrativeTier} — {era}
            Situation: {title}
            Details: {description}
            """;
    }

    private static string StyleBlock(bool sciFi) =>
        sciFi
            ? """
              Write exactly 2-3 sentences as an in-world briefing for the governor.
              Tone: plausible near-future sci-fi — AI, autonomy, bio/nano only as appropriate to this tier.
              No bullet points, headers, labels, or statistics blocks. No markdown.
              """
            : """
              Write exactly 2-3 sentences as an in-world briefing for the governor.
              Tone: realistic for the stated historical era — farms, workshops, mills, railways, telegraph, not smartphones or modern US crime reports.
              No bullet points, headers, labels, or statistics blocks. No markdown.
              Do not name US states, California, or 21st-century places unless era is TTS 4+.
              """;

    private static bool IsSciFiTier(int tier) => tier >= SciFiTierThreshold;

    private static int ParseTierFromTitle(string title)
    {
        var parts = title.Split(' ');
        return parts.Length >= 2 && int.TryParse(parts[1], out var tier) ? tier : 1;
    }
}
