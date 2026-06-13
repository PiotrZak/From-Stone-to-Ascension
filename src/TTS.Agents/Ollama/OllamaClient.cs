namespace TTS.Agents.Ollama;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class OllamaClient : IDisposable
{
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

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var models = await ListModelsAsync(cancellationToken);
        if (models.Count == 0)
        {
            throw new InvalidOperationException(
                "No Ollama models installed. Run: ollama pull llama3.2");
        }

        var model = models.Contains(_settings.Model, StringComparer.OrdinalIgnoreCase)
            ? _settings.Model
            : models[0];

        var request = new ChatRequest(
            model,
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt)
            ],
            Stream: false);

        using var response = await _http.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
        return payload?.Message?.Content?.Trim()
            ?? throw new InvalidOperationException("Ollama returned an empty response.");
    }

    public void Dispose() => _http.Dispose();

    private sealed record ChatRequest(
        string Model,
        ChatMessage[] Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record ChatMessage(string Role, string Content);

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
