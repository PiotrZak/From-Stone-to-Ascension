namespace TTS.Agents.Scenarios;

using TTS.Llm.Agents;

public sealed class AdvisorScenario(AdvisorAgent agent) : IScenario
{
    public AdvisorScenario() : this(new AdvisorAgent()) { }

    public string Id => "advisor";
    public string Title => "TTS 5 Advisor (LLM Tools)";
    public string Description => "Read-only tool loop: inspect civ → strategic briefing.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);

        Console.WriteLine("Running advisor tool agent...\n");
        var reply = await agent.GetBriefingAsync(player.Id, tools, cancellationToken);

        Console.WriteLine(reply ?? "(Advisor unavailable — is Ollama running?)");
    }
}
