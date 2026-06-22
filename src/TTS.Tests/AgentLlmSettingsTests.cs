using TTS.Llm;

namespace TTS.Tests;

public class AgentLlmSettingsTests
{
    [Fact]
    public void FromEnvironment_Ollama_AppendsV1ToBaseUrl()
    {
        var settings = AgentLlmSettings.FromEnvironment("ollama");
        Assert.EndsWith("/v1", settings.BaseUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ollama", settings.ApiKey);
        Assert.True(settings.IsConfigured);
    }

    [Fact]
    public void CreateWorkflow_ReturnsAgentToolWorkflow()
    {
        var settings = new AgentProviderSettings { Provider = "ollama" };
        var workflow = AgentProviderFactory.CreateWorkflow(settings);

        Assert.IsType<AgentToolWorkflow>(workflow);
    }
}
