namespace TTS.Llm.Tools;

using System.Text.Json;
using TTS.Core.Agents;

public sealed record LlmToolDefinition(
    GameTool Tool,
    string Description,
    bool IsReadOnly,
    JsonElement ParametersSchema)
{
    public string Name => Tool.ToApiName();
}

public sealed record LlmToolCallResult(
    bool Success,
    string Json,
    string? Error = null);
