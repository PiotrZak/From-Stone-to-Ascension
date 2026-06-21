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
        var narrativeTier = type == GateType.TierAdvancement
            ? ParseTierFromTitle(title)
            : civilizationTier;

        var (system, user) = BuildPrompt(civilizationName, type, title, description, narrativeTier);
        return await _client.TryChatAsync(system, user, cancellationToken);
    }

    private static (string System, string User) BuildPrompt(
        string civilizationName,
        GateType type,
        string title,
        string description,
        int narrativeTier)
    {
        var era = TierNames.GetValueOrDefault(narrativeTier, $"TTS {narrativeTier}");
        var sciFi = IsSciFiTier(narrativeTier);

        var baseContext = $"""
            Civilization: {civilizationName}
            Era: TTS {narrativeTier} — {era}
            Situation: {title}
            Details: {description}
            """;

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
                ? "Describe rising crime pressure and strain on governance."
                : "Describe rising crime, poverty, or social disorder in realistic terms — cities, policing, inequality.",
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

    private static string StyleBlock(bool sciFi) =>
        sciFi
            ? """
              Write 2-3 sentences for a mobile strategy dashboard.
              Tone: plausible near-future sci-fi — AI, autonomy, bio/nano only as appropriate to this tier.
              No bullet points. No option labels. No markdown. No mythic or fantasy language.
              """
            : """
              Write 2-3 sentences for a mobile strategy dashboard.
              Tone: realistic historical briefing — governor's memo or newspaper dispatch.
              No sci-fi, no spaceships, no AI, no nanotech, no singularity, no mythic fables.
              No bullet points. No option labels. No markdown.
              """;

    private static bool IsSciFiTier(int tier) => tier >= SciFiTierThreshold;

    private static int ParseTierFromTitle(string title)
    {
        var parts = title.Split(' ');
        return parts.Length >= 2 && int.TryParse(parts[1], out var tier) ? tier : 1;
    }
}
