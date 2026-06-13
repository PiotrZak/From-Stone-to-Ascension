namespace TTS.Core.Simulation;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

public sealed class AgentTurnRunner : ICivilizationTurnRunner
{
    private readonly AgentOrchestrator _orchestrator;

    public AgentTurnRunner(AgentOrchestrator orchestrator) => _orchestrator = orchestrator;

    public string RunnerId => "agent";

    public bool CanHandle(Civilization civilization, WorldState world) =>
        !civilization.IsPlayerControlled && civilization.CurrentTier >= TechTier.EarlyAI;

    public CivilizationTurnResult Run(Civilization civilization, WorldState world)
    {
        var result = _orchestrator.RunTurn(civilization, world);
        return result.UsedAgent
            ? CivilizationTurnResult.Completed(result.Message, result.TechnologyId, result.Evaluation)
            : CivilizationTurnResult.Skipped(result.Message);
    }
}
