namespace TTS.Llm;

/// <summary>LLM provider selection — see agent-framework-integration.md §3.1.</summary>
public sealed class AgentProviderSettings
{
    /// <summary>ollama | openai | gemini | none</summary>
    public string Provider { get; init; } =
        Environment.GetEnvironmentVariable("TTS_LLM_PROVIDER") ?? "ollama";

    public bool AgentsEnabled =>
        !string.Equals(Provider, "none", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(Provider);

    public static AgentProviderSettings FromEnvironment() => new();
}
