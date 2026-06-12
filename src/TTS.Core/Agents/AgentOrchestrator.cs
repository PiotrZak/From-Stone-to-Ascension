namespace TTS.Core.Agents;

using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>
/// Entry point for MAF-backed civilization turns. At TTS 5+ this orchestrator
/// can be wired to Microsoft Agent Framework workflows; below TTS 5 it no-ops.
/// </summary>
public class AgentOrchestrator
{
    private readonly IGameToolSurface _tools;
    private readonly TechTreeSystem _techTreeSystem = new();
    private readonly ForbiddenTechSystem _forbiddenTechSystem = new();

    public AgentOrchestrator(IGameToolSurface tools)
    {
        _tools = tools;
    }

    public AgentTurnResult RunTurn(Civilization civilization, WorldState world)
    {
        if ((int)civilization.CurrentTier < (int)TechTier.EarlyAI)
            return AgentTurnResult.Skipped("Classical AI handles turns below TTS 5.");

        _ = _tools.GetCivilizationState(civilization.Id);
        _ = _tools.GetFactionTensions(civilization.Id);

        var candidate = _techTreeSystem
            .GetAvailableTechnologies(civilization, world)
            .OrderByDescending(t => t.RiskLevel)
            .FirstOrDefault();

        if (candidate is null)
            return AgentTurnResult.Completed("No research candidates available.");

        var result = _techTreeSystem.Research(civilization, candidate, _forbiddenTechSystem);
        return result.Success
            ? AgentTurnResult.Completed($"Agent researched '{candidate.Name}'.")
            : AgentTurnResult.Completed(result.Message);
    }
}

public readonly record struct AgentTurnResult(bool UsedAgent, string Message)
{
    public static AgentTurnResult Skipped(string message) => new(false, message);
    public static AgentTurnResult Completed(string message) => new(true, message);
}
