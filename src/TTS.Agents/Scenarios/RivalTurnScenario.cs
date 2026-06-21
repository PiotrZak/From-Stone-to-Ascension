namespace TTS.Agents.Scenarios;

using TTS.Llm.Agents;

public sealed class RivalTurnScenario(CivilizationTurnAgent agent) : IScenario
{
    public RivalTurnScenario() : this(new CivilizationTurnAgent()) { }

    public string Id => "rival-turn";
    public string Title => "AI Civilization Turn (LLM Tools)";
    public string Description => "Iron Dominion uses tool loop: inspect state → propose_research.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var rival = world.Civilizations.First(c => !c.IsPlayerControlled);

        Console.WriteLine("Running LLM tool agent turn...\n");
        var result = await agent.RunAsync(rival, tools, cancellationToken);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Researched: {result.ResearchedTechnologyId ?? "none"}");
        Console.WriteLine($"Tools used: {string.Join(" → ", result.ToolsUsed)}");
        Console.WriteLine();
        Console.WriteLine(result.Message);
    }
}
