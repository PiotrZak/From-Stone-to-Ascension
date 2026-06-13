namespace TTS.Core.Simulation;

using TTS.Core.Agents;
using TTS.Core.Models;

public sealed class AgentTurnRunner : ICivilizationTurnRunner
{
    private readonly AgentOrchestrator _orchestrator;

    public AgentTurnRunner(AgentOrchestrator orchestrator) => _orchestrator = orchestrator;

    public bool CanHandle(Civilization civilization, WorldState world) =>
        !civilization.IsPlayerControlled && civilization.CurrentTier >= TechTier.EarlyAI;

    public CivilizationTurnResult Run(Civilization civilization, WorldState world)
    {
        var result = _orchestrator.RunTurn(civilization, world);
        return result.UsedAgent
            ? CivilizationTurnResult.Completed(result.Message)
            : CivilizationTurnResult.Skipped(result.Message);
    }
}
