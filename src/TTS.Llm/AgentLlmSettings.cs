namespace TTS.Llm;

/// <summary>OpenAI-compatible LLM endpoint settings for Microsoft Agent Framework.</summary>
public sealed class AgentLlmSettings
{
    public string BaseUrl { get; init; } = "http://localhost:11434/v1";
    public string Model { get; init; } = "llama3.2";
    public string ApiKey { get; init; } = "ollama";

    public static AgentLlmSettings FromEnvironment(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "openai" => new AgentLlmSettings
            {
                BaseUrl = "https://api.openai.com/v1",
                Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini",
                ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "",
            },
            "gemini" => new AgentLlmSettings
            {
                BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/",
                Model = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.0-flash",
                ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "",
            },
            _ => new AgentLlmSettings
            {
                BaseUrl = NormalizeOllamaV1Url(
                    Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434"),
                Model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2",
                ApiKey = "ollama",
            },
        };
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Model)
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(ApiKey);

    private static string NormalizeOllamaV1Url(string baseUrl)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? trimmed : $"{trimmed}/v1";
    }
}
