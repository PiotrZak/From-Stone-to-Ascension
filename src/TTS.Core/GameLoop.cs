namespace TTS.Core;

using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

/// <summary>
/// Primary and secondary simulation loops from the game design document.
/// </summary>
public class GameLoop
{
    private readonly WorldState _world;
    private readonly SimulationServices _services;
    private readonly IReadOnlyList<ITurnPhase> _phases;

    public GameLoop(WorldState world) : this(world, new SimulationServices())
    {
    }

    public GameLoop(WorldState world, SimulationServices services)
    {
        _world = world;
        _services = services;
        var tools = services.CreateToolSurface(world);
        _phases = TurnPhasePipeline.CreateDefault(services, tools);
    }

    public SimulationServices Services => _services;

    public TurnResult RunTurn()
    {
        _services.BeginTurn();

        foreach (var phase in _phases)
            phase.Execute(_world, _services);

        _world.Turn++;

        var outcomes = _world.Civilizations
            .Select(c => (Civilization: c, Outcome: _services.WinLoss.Evaluate(c)))
            .ToList();

        return new TurnResult(_world.Turn - 1, outcomes, _services.TurnResearchDecisions.ToList());
    }
}

public readonly record struct TurnResult(
    int Turn,
    IReadOnlyList<(Civilization Civilization, GameOutcome Outcome)> Outcomes,
    IReadOnlyList<TurnResearchDecision> ResearchDecisions);
