namespace TTS.Llm;

using System.Text.Json;
using System.Text.Json.Serialization;
using TTS.Core.Agents;
using TTS.Llm.Tools;

/// <summary>Multi-turn LLM session with tool calling — Emergence-style agent loop over game tools.</summary>
public sealed class LlmAgentSession
{
    private const int MaxToolRounds = 8;

    private readonly OllamaClient _client;
    private readonly GameToolRegistry _registry;
    private readonly string _systemPrompt;

    public LlmAgentSession(
        OllamaClient client,
        GameToolRegistry registry,
        string systemPrompt)
    {
        _client = client;
        _registry = registry;
        _systemPrompt = systemPrompt;
    }

    public async Task<LlmAgentSessionResult> RunAsync(
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var tools = _registry.GetDefinitions();
        var messages = new List<OllamaMessage>
        {
            new("system", _systemPrompt),
            new("user", userPrompt)
        };

        string? lastResearchId = null;
        var toolLog = new List<string>();

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var response = await _client.ChatWithToolsAsync(messages, tools, cancellationToken);
            if (response is null)
                return LlmAgentSessionResult.Unavailable("LLM unavailable.");

            if (response.ToolCalls.Count == 0)
            {
                return new LlmAgentSessionResult(
                    true,
                    response.Content ?? "",
                    lastResearchId,
                    toolLog);
            }

            messages.Add(new OllamaMessage("assistant", response.Content ?? "", response.ToolCalls));

            foreach (var call in response.ToolCalls)
            {
                toolLog.Add(call.Name);
                var args = ParseArgs(call.ArgumentsJson);
                var result = GameToolExtensions.TryParse(call.Name, out var tool)
                    ? _registry.Execute(tool, args)
                    : _registry.Execute(call.Name, args);
                if (GameToolExtensions.TryParse(call.Name, out var parsed)
                    && parsed == GameTool.ProposeResearch
                    && result.Success)
                {
                    using var doc = JsonDocument.Parse(result.Json);
                    if (doc.RootElement.TryGetProperty("accepted", out var ok) && ok.GetBoolean()
                        && doc.RootElement.TryGetProperty("technology_id", out var tid))
                        lastResearchId = tid.GetString();
                }

                messages.Add(OllamaMessage.Tool(call.Name, result.Json, result.Error));
            }
        }

        return new LlmAgentSessionResult(
            true,
            "Agent reached tool round limit.",
            lastResearchId,
            toolLog);
    }

    private static JsonElement ParseArgs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return JsonDocument.Parse("{}").RootElement.Clone();

        try
        {
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }
    }
}

public sealed record LlmAgentSessionResult(
    bool Success,
    string Message,
    string? ResearchedTechnologyId,
    IReadOnlyList<string> ToolsUsed)
{
    public static LlmAgentSessionResult Unavailable(string message) =>
        new(false, message, null, []);
}

public sealed record OllamaMessage(
    string Role,
    string Content,
    IReadOnlyList<OllamaToolCall>? ToolCalls = null)
{
    public static OllamaMessage Tool(string name, string resultJson, string? error = null) =>
        new("tool", error is null ? resultJson : $$"""{"error":"{{error}}","result":{{resultJson}}}""");
}

public sealed record OllamaToolCall(string Name, string? ArgumentsJson);

public sealed record OllamaChatResponse(string? Content, IReadOnlyList<OllamaToolCall> ToolCalls);
