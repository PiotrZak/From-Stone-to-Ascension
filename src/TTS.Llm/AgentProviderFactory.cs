namespace TTS.Llm;

using TTS.Core.Agents;

public static class AgentProviderFactory
{
    public static ILlmTurnAgent? CreateTurnAgent()
    {
        var settings = AgentProviderSettings.FromEnvironment();
        if (!settings.AgentsEnabled)
            return null;

        return settings.Provider.ToLowerInvariant() switch
        {
            "ollama" or "openai" or "gemini" => new LlmTurnAgent(settings),
            _ => null
        };
    }

    public static IAgentWorkflow? CreateWorkflow() =>
        CreateWorkflow(AgentProviderSettings.FromEnvironment());

    public static IAgentWorkflow? CreateWorkflow(AgentProviderSettings settings)
    {
        if (!settings.AgentsEnabled)
            return null;

        if (settings.Provider.ToLowerInvariant() is not ("ollama" or "openai" or "gemini"))
            return null;

        var workflow = new AgentToolWorkflow(settings);
        return workflow.IsConfigured ? workflow : null;
    }
}
