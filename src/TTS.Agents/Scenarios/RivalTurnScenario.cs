namespace TTS.Agents.Scenarios;

using TTS.Llm;

public sealed class RivalTurnScenario(OllamaClient ollama) : IScenario
{
    public string Id => "rival-turn";
    public string Title => "AI Civilization Turn";
    public string Description => "Iron Dominion (TechRush policy) picks next research at TTS 5.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var rival = world.Civilizations.First(c => !c.IsPlayerControlled);
        var available = tools.GetAvailableTechnologies(rival.Id);

        var userPrompt = $"""
            You control the AI civilization "{rival.Name}" in a tech strategy game.
            Policy: {rival.Policy.Research} (TechRush — favors risky advanced tech)
            Current tier: TTS {(int)rival.CurrentTier}
            Stability: {rival.AverageStability:F0}
            Already researched: {string.Join(", ", rival.ResearchedTechnologyIds)}

            Available technologies to research next:
            {string.Join("\n", available.Select(t => $"- {t.Id}: {t.Name} (risk {t.RiskLevel}, forbidden={t.IsForbidden})"))}

            Pick ONE technology id from the list above. Reply with:
            CHOICE: <tech-id>
            REASON: <one sentence>
            """;

        Console.WriteLine("Prompting Ollama rival AI...\n");
        var reply = await ollama.ChatAsync(
            "You are an AI civilization governor. Follow the policy stance. Pick only from the given list.",
            userPrompt,
            cancellationToken);

        Console.WriteLine(reply);
        Console.WriteLine();
        Console.WriteLine("(In-game, TTS.Core validates and applies the choice via ProposeResearch.)");
    }
}
