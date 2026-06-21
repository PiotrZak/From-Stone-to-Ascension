namespace TTS.Llm;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TTS.Core.Agents;
using TTS.Llm.Tools;

public sealed class OllamaClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly OllamaSettings _settings;

    public OllamaClient(OllamaSettings? settings = null, HttpClient? httpClient = null)
    {
        _settings = settings ?? OllamaSettings.FromEnvironment();
        _http = httpClient ?? new HttpClient { BaseAddress = new Uri(_settings.BaseUrl) };
    }

    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<TagsResponse>("/api/tags", cancellationToken);
        return response?.Models?.Select(m => m.Name).ToList() ?? [];
    }

    public async Task<string> ChatAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var reply = await TryChatAsync(systemPrompt, userPrompt, cancellationToken);
        return reply ?? throw new InvalidOperationException(
            "Ollama is unavailable or returned an empty response. Run: ollama serve && ollama pull llama3.2");
    }

    public async Task<string?> TryChatAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var response = await ChatWithToolsAsync(
            [new OllamaMessage("system", systemPrompt), new OllamaMessage("user", userPrompt)],
            [],
            cancellationToken);

        return response?.Content?.Trim();
    }

    public async Task<OllamaChatResponse?> ChatWithToolsAsync(
        IReadOnlyList<OllamaMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await IsReachableAsync(cancellationToken))
                return null;

            var model = await ResolveModelAsync(cancellationToken);
            if (model is null)
                return null;

            var payload = new ToolChatRequest
            {
                Model = model,
                Messages = messages.Select(ToPayloadMessage).ToList(),
                Stream = false,
                Tools = tools.Count > 0 ? tools.Select(ToToolSpec).ToList() : null
            };

            using var response = await _http.PostAsJsonAsync("/api/chat", payload, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<ToolChatResponse>(JsonOptions, cancellationToken);
            if (body?.Message is null)
                return null;

            var toolCalls = body.Message.ToolCalls?
                .Select(tc => new OllamaToolCall(
                    tc.Function?.Name ?? "",
                    tc.Function?.Arguments is string s ? s : tc.Function?.Arguments?.ToString()))
                .Where(tc => !string.IsNullOrEmpty(tc.Name))
                .ToList() ?? [];

            return new OllamaChatResponse(body.Message.Content?.Trim(), toolCalls);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> ResolveModelAsync(CancellationToken cancellationToken)
    {
        var models = await ListModelsAsync(cancellationToken);
        if (models.Count == 0)
            return null;

        return models.Contains(_settings.Model, StringComparer.OrdinalIgnoreCase)
            ? _settings.Model
            : models[0];
    }

    private static ToolChatMessage ToPayloadMessage(OllamaMessage msg)
    {
        var toolCalls = msg.ToolCalls?.Count > 0
            ? msg.ToolCalls.Select(tc => new ToolCallPayload
            {
                Function = new ToolFunctionPayload { Name = tc.Name, Arguments = tc.ArgumentsJson ?? "{}" }
            }).ToList()
            : null;

        return new ToolChatMessage
        {
            Role = msg.Role,
            Content = msg.Content,
            ToolCalls = toolCalls
        };
    }

    private static ToolSpec ToToolSpec(LlmToolDefinition def) => new()
    {
        Type = "function",
        Function = new ToolFunctionSpec
        {
            Name = def.Tool.ToApiName(),
            Description = def.Description,
            Parameters = def.ParametersSchema
        }
    };

    public void Dispose() => _http.Dispose();

    private sealed class ToolChatRequest
    {
        public required string Model { get; init; }
        public required List<ToolChatMessage> Messages { get; init; }
        public bool Stream { get; init; }
        public List<ToolSpec>? Tools { get; init; }
    }

    private sealed class ToolChatMessage
    {
        public required string Role { get; init; }
        public string? Content { get; init; }
        public List<ToolCallPayload>? ToolCalls { get; init; }
    }

    private sealed class ToolCallPayload
    {
        public ToolFunctionPayload? Function { get; init; }
    }

    private sealed class ToolFunctionPayload
    {
        public string? Name { get; init; }
        public object? Arguments { get; init; }
    }

    private sealed class ToolSpec
    {
        public required string Type { get; init; }
        public required ToolFunctionSpec Function { get; init; }
    }

    private sealed class ToolFunctionSpec
    {
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required JsonElement Parameters { get; init; }
    }

    private sealed class ToolChatResponse
    {
        public ToolChatMessagePayload? Message { get; init; }
    }

    private sealed class ToolChatMessagePayload
    {
        public string? Content { get; init; }
        public List<ToolCallPayload>? ToolCalls { get; init; }
    }

    private sealed class TagsResponse
    {
        public List<ModelTag>? Models { get; init; }
    }

    private sealed class ModelTag
    {
        public string Name { get; init; } = "";
    }
}
