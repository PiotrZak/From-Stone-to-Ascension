namespace TTS.Llm;

public sealed class OllamaSettings
{
    public string BaseUrl { get; init; } =
        Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434";

    public string Model { get; init; } =
        Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";

    public static OllamaSettings FromEnvironment() => new();
}
