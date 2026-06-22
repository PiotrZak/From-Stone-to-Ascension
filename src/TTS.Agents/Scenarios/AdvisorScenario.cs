namespace TTS.Agents.Scenarios;

using TTS.Llm;

public sealed class AdvisorScenario(IAgentWorkflow workflow) : IScenario
{
    public AdvisorScenario() : this(AgentProviderFactory.CreateWorkflow() ?? new AgentToolWorkflow()) { }

    public string Id => "advisor";
    public string Title => "TTS 5 Advisor (LLM Tools)";
    public string Description => "Read-only tool loop: inspect civ → strategic briefing.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var limits = AgentSessionLimits.FromEnvironment();

        Console.WriteLine("Running advisor tool agent...\n");
        var reply = await workflow.RunAdvisorBriefingAsync(player.Id, tools, limits, cancellationToken);

        Console.WriteLine(reply ?? "(Advisor unavailable — is Ollama running?)");
    }
}
