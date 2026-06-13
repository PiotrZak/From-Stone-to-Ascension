namespace TTS.Agents.Scenarios;

public interface IScenario
{
    string Id { get; }
    string Title { get; }
    string Description { get; }
    Task RunAsync(CancellationToken cancellationToken = default);
}
