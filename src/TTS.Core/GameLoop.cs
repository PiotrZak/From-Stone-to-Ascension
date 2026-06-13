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

    public TurnResult RunTurn(DateTimeOffset? simulatedNow = null)
    {
        if (simulatedNow.HasValue)
            _world.SimulatedNow = simulatedNow.Value;
        else if (_world.Match is not null)
            _world.SimulatedNow += _world.Match.Config.TickInterval;

        _services.BeginTurn(_world);
        _services.DecisionGates.ExpireGates(_world, _services);

        foreach (var phase in _phases)
            phase.Execute(_world, _services);

        if (_services.CurrentTurnSnapshot is not null)
            _services.DecisionGates.ScanAfterTurn(_world, _services, _services.CurrentTurnSnapshot);

        _services.FinalizeTurn(_world);
        _world.Turn++;

        if (_world.Match is { Status: MatchStatus.Running } match)
            match.TickCount = _world.Turn - 1;

        var outcomes = _world.Civilizations
            .Select(c => (Civilization: c, Outcome: _services.WinLoss.Evaluate(c)))
            .ToList();

        return new TurnResult(
            _world.Turn - 1,
            outcomes,
            _services.TurnResearchDecisions.ToList(),
            CollectActiveGates());
    }

    private IReadOnlyList<DecisionGate> CollectActiveGates() =>
        _world.Civilizations
            .SelectMany(c => c.PendingDecisions.Where(g => !g.IsResolved))
            .ToList();
}

public readonly record struct TurnResult(
    int Turn,
    IReadOnlyList<(Civilization Civilization, GameOutcome Outcome)> Outcomes,
    IReadOnlyList<TurnResearchDecision> ResearchDecisions,
    IReadOnlyList<DecisionGate> ActiveGates);
