namespace TTS.Agents.Scenarios;

using TTS.Llm;

public sealed class RivalTurnScenario(IAgentWorkflow workflow) : IScenario
{
    public RivalTurnScenario() : this(AgentProviderFactory.CreateWorkflow() ?? new AgentToolWorkflow()) { }

    public string Id => "rival-turn";
    public string Title => "AI Civilization Turn (LLM Tools)";
    public string Description => "Iron Dominion: diplomacy + research via multi-tool agent session.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var rival = world.Civilizations.First(c => !c.IsPlayerControlled);
        var rivals = LlmTurnAgent.BuildRivals(world, rival.Id);
        var limits = AgentSessionLimits.FromEnvironment();

        Console.WriteLine("Running multi-tool agent turn (diplomacy + research)...\n");
        var result = await workflow.RunCivilizationTurnAsync(rival, tools, rivals, limits, cancellationToken);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Researched: {result.ResearchedTechnologyId ?? "none"}");
        if (result.DiplomaticActions.Count > 0)
            Console.WriteLine($"Diplomacy: {string.Join("; ", result.DiplomaticActions)}");
        Console.WriteLine($"Tools: {string.Join(" → ", result.ToolsUsed)}");
        Console.WriteLine();
        Console.WriteLine(result.Message);
    }
}
