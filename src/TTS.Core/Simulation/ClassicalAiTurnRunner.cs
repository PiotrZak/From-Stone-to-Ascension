namespace TTS.Core.Simulation;

using TTS.Core.Models;

public sealed class ClassicalAiTurnRunner : ICivilizationTurnRunner
{
    private readonly SimulationServices _services;

    public ClassicalAiTurnRunner(SimulationServices services) => _services = services;

    public bool CanHandle(Civilization civilization, WorldState world) =>
        civilization.IsPlayerControlled || civilization.CurrentTier < TechTier.EarlyAI;

    public CivilizationTurnResult Run(Civilization civilization, WorldState world)
    {
        var result = _services.ClassicalAi.RunTurn(civilization, world);
        return result.DidResearch
            ? CivilizationTurnResult.Completed(result.Message, result.TechnologyId)
            : CivilizationTurnResult.Skipped(result.Message);
    }
}
