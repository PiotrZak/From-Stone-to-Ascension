namespace TTS.Agents.Scenarios;

using TTS.Llm;

public sealed class PingScenario(OllamaClient ollama) : IScenario
{
    public string Id => "ping";
    public string Title => "Ollama Health Check";
    public string Description => "Verifies Ollama is running and lists installed models.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var settings = OllamaSettings.FromEnvironment();
        Console.WriteLine($"Ollama URL:  {settings.BaseUrl}");
        Console.WriteLine($"Model pref:  {settings.Model}");
        Console.WriteLine();

        if (!await ollama.IsReachableAsync(cancellationToken))
        {
            Console.WriteLine("FAIL: Cannot reach Ollama. Is `ollama serve` running?");
            return;
        }

        Console.WriteLine("OK: Ollama is reachable.");

        var models = await ollama.ListModelsAsync(cancellationToken);
        if (models.Count == 0)
        {
            Console.WriteLine("WARN: No models installed. Run: ollama pull llama3.2");
            return;
        }

        Console.WriteLine($"Models ({models.Count}):");
        foreach (var model in models)
            Console.WriteLine($"  - {model}");

        Console.WriteLine();
        var reply = await ollama.ChatAsync(
            "Reply in one short sentence.",
            "Confirm you are running locally for the TTS game project.",
            cancellationToken);
        Console.WriteLine($"Chat test: {reply}");
    }
}
