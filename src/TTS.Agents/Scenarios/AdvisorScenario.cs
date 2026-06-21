namespace TTS.Agents.Scenarios;

using TTS.Llm;
using TTS.Core.Agents;

public sealed class AdvisorScenario(OllamaClient ollama) : IScenario
{
    public string Id => "advisor";
    public string Title => "TTS 5 Advisor";
    public string Description => "In-world advisor summarizes civ state and recommends next policy move.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var snapshot = tools.GetCivilizationState(player.Id);
        var tensions = tools.GetFactionTensions(player.Id);
        var events = tools.GetGlobalEvents(activeOnly: true);

        var userPrompt = $"""
            Civilization: {snapshot.Name}
            Tier: TTS {(int)snapshot.CurrentTier} (Early AI Age)
            Stability — political: {snapshot.PoliticalStability:F0}, economic: {snapshot.EconomicStability:F0}, technological: {snapshot.TechnologicalStability:F0}
            Researched techs: {string.Join(", ", snapshot.ResearchedTechnologyIds)}
            Policy stance: {player.Policy.Research}
            Faction tensions: {string.Join(", ", tensions.Select(kv => $"{kv.Key}={kv.Value:F1}"))}
            Active events: {(events.Count > 0 ? string.Join(", ", events.Select(e => e.Name)) : "none")}

            Give a brief advisor briefing (3-5 sentences) and one concrete recommendation for research or stability.
            """;

        Console.WriteLine("Prompting Ollama advisor...\n");
        var reply = await ollama.ChatAsync(
            "You are a strategic advisor in a civilization tech simulation game (TTS). Be concise and actionable.",
            userPrompt,
            cancellationToken);

        Console.WriteLine(reply);
    }
}
