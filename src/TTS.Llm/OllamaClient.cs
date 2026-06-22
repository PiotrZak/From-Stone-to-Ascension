namespace TTS.Llm;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Simple Ollama HTTP client for narrative text (fables, scenarios) — not used for agent tool loops.</summary>
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
        try
        {
            if (!await IsReachableAsync(cancellationToken))
                return null;

            var model = await ResolveModelAsync(cancellationToken);
            if (model is null)
                return null;

            var payload = new ChatRequest
            {
                Model = model,
                Stream = false,
                Messages =
                [
                    new ChatMessage { Role = "system", Content = systemPrompt },
                    new ChatMessage { Role = "user", Content = userPrompt }
                ]
            };

            using var response = await _http.PostAsJsonAsync("/api/chat", payload, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions, cancellationToken);
            return body?.Message?.Content?.Trim();
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

    public void Dispose() => _http.Dispose();

    private sealed class ChatRequest
    {
        public required string Model { get; init; }
        public required List<ChatMessage> Messages { get; init; }
        public bool Stream { get; init; }
    }

    private sealed class ChatMessage
    {
        public required string Role { get; init; }
        public required string Content { get; init; }
    }

    private sealed class ChatResponse
    {
        public ChatMessagePayload? Message { get; init; }
    }

    private sealed class ChatMessagePayload
    {
        public string? Content { get; init; }
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
