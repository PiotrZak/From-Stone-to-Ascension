namespace TTS.Agents.Scenarios;

using TTS.Llm;

public sealed class CrisisScenario(OllamaClient ollama) : IScenario
{
    public string Id => "crisis";
    public string Title => "AI Alignment Crisis";
    public string Description => "Narrates a global crisis and proposes 3 structured player choices.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var player = tools.GetCivilizationState(world.Civilizations.First(c => c.IsPlayerControlled).Id);
        var crisis = world.ActiveEvents[0];

        var userPrompt = $"""
            A global crisis has struck the simulation.

            Event: {crisis.Name}
            Description: {crisis.Description}
            Severity: {crisis.Severity}/5
            Player civ: {player.Name} at TTS {(int)player.CurrentTier}
            Technological stability: {player.TechnologicalStability:F0}

            Write:
            1. A dramatic 2-3 sentence player-facing briefing
            2. Exactly three choices labeled A, B, C (e.g. regulate, accelerate, isolate)
            3. One sentence each on stability impact of each choice
            """;

        Console.WriteLine("Prompting Ollama crisis narrator...\n");
        var reply = await ollama.ChatAsync(
            "You write crisis events for a sci-fi civilization strategy game. Output clear structured choices.",
            userPrompt,
            cancellationToken);

        Console.WriteLine(reply);
    }
}
