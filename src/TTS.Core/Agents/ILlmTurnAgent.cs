namespace TTS.Core.Agents;

using TTS.Core.Models;

/// <summary>
/// Optional LLM-backed turn agent (TTS 5+). When disabled or on failure, orchestrator falls back to classical AI.
/// </summary>
public interface ILlmTurnAgent
{
    bool IsEnabled { get; }

    AgentTurnResult? TryRunTurn(
        Civilization civilization,
        IGameToolSurface tools,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
