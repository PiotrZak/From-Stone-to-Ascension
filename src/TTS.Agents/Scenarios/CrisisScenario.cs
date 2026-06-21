namespace TTS.Agents.Scenarios;

using TTS.Llm.Agents;

public sealed class CrisisScenario(CrisisWorkflowAgent agent) : IScenario
{
    public CrisisScenario() : this(new CrisisWorkflowAgent()) { }

    public string Id => "crisis";
    public string Title => "AI Alignment Crisis";
    public string Description => "Crisis workflow narrates event and proposes structured choices.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateEarlyAiCrisis();
        var player = tools.GetCivilizationState(world.Civilizations.First(c => c.IsPlayerControlled).Id);
        var crisis = world.ActiveEvents[0];

        Console.WriteLine("Running crisis workflow...\n");
        var reply = await agent.NarrateAsync(player.Name, crisis, player, cancellationToken);

        Console.WriteLine(reply ?? "(Crisis narrator unavailable — is Ollama running?)");
    }
}
