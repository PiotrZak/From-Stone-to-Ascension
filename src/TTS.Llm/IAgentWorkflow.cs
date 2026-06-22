namespace TTS.Llm;

using TTS.Core.Agents;
using TTS.Core.Models;

/// <summary>Agent workflow via Microsoft Agent Framework.</summary>
public interface IAgentWorkflow
{
    Task<AgentSessionResult> RunCivilizationTurnAsync(
        Civilization civilization,
        IGameToolSurface tools,
        IReadOnlyList<RivalSummary> rivals,
        AgentSessionLimits limits,
        CancellationToken cancellationToken = default);

    Task<string?> RunAdvisorBriefingAsync(
        string civilizationId,
        IGameToolSurface tools,
        AgentSessionLimits limits,
        CancellationToken cancellationToken = default);
}

public readonly record struct RivalSummary(string Id, string Name, int Tier, double Stability);
