namespace TTS.Llm;

public sealed record AgentSessionResult(
    bool Success,
    string Message,
    string? ResearchedTechnologyId,
    IReadOnlyList<string> ToolsUsed,
    IReadOnlyList<string> DiplomaticActions)
{
    public static AgentSessionResult Unavailable(string message) =>
        new(false, message, null, [], []);
}
