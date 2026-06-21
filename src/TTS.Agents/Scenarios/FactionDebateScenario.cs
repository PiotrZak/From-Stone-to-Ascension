namespace TTS.Agents.Scenarios;

using TTS.Llm;

public sealed class FactionDebateScenario(OllamaClient ollama) : IScenario
{
    public string Id => "faction-debate";
    public string Title => "Faction Debate";
    public string Description => "Government vs corporation argue over forbidden recursive AI research.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, _, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var factions = player.Factions;

        var userPrompt = $"""
            Internal faction debate in civilization "{player.Name}" (TTS {(int)player.CurrentTier}).

            Factions:
            {string.Join("\n", factions.Select(f => $"- {f.Name} ({f.Type}, {f.Stance}, influence {f.Influence})"))}

            Issue: Should the civ pursue "Self-aware Recursive AI" (forbidden, high risk, high reward)?

            Write a short debate:
            - 2 lines from the accelerationist faction
            - 2 lines from the cautious faction
            - 1 line summarizing the tension for the player-governor
            """;

        Console.WriteLine("Prompting Ollama faction debate...\n");
        var reply = await ollama.ChatAsync(
            "You write political faction dialogue for a sci-fi strategy game. Keep each voice distinct.",
            userPrompt,
            cancellationToken);

        Console.WriteLine(reply);
    }
}
