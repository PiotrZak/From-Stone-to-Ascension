namespace TTS.Core;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>
/// Primary and secondary simulation loops from the game design document.
/// </summary>
public class GameLoop
{
    private readonly WorldState _world;
    private readonly StabilitySystem _stabilitySystem = new();
    private readonly TechTreeSystem _techTreeSystem = new();
    private readonly FactionSystem _factionSystem = new();
    private readonly GlobalEventSystem _globalEventSystem = new();
    private readonly KnowledgeDiffusionSystem _knowledgeDiffusionSystem = new();
    private readonly ForbiddenTechSystem _forbiddenTechSystem = new();
    private readonly WinLossSystem _winLossSystem = new();
    private readonly AgentOrchestrator _agentOrchestrator;

    public GameLoop(WorldState world)
    {
        _world = world;
        _agentOrchestrator = new AgentOrchestrator(new GameToolSurface(world));
    }

    public TurnResult RunTurn()
    {
        RunPrimaryLoop();
        RunSecondaryLoop();
        _world.Turn++;

        var outcomes = _world.Civilizations
            .Select(c => (Civilization: c, Outcome: _winLossSystem.Evaluate(c)))
            .ToList();

        return new TurnResult(_world.Turn - 1, outcomes);
    }

    private void RunPrimaryLoop()
    {
        foreach (var region in _world.Regions)
        {
            region.Resources = Math.Clamp(region.Resources + 0.5, 0, 100);
            region.Infrastructure = Math.Clamp(region.Infrastructure + 0.2, 0, 100);
        }

        foreach (var civilization in _world.Civilizations)
        {
            _stabilitySystem.ApplyTurnDecay(civilization);

            var available = _techTreeSystem.GetAvailableTechnologies(civilization, _world).ToList();
            if (available.Count > 0 && civilization.IsPlayerControlled)
            {
                var next = available[0];
                _techTreeSystem.Research(civilization, next, _forbiddenTechSystem);
            }
            else if (!civilization.IsPlayerControlled)
            {
                _agentOrchestrator.RunTurn(civilization, _world);
            }
        }
    }

    private void RunSecondaryLoop()
    {
        _knowledgeDiffusionSystem.Diffuse(_world);

        foreach (var civilization in _world.Civilizations)
            _factionSystem.ApplyTurnInfluence(civilization);

        var newEvent = _globalEventSystem.MaybeGenerateEvent(_world);
        if (newEvent is not null)
            _globalEventSystem.EmitEvent(_world, newEvent);

        foreach (var civilization in _world.Civilizations)
        {
            foreach (var activeEvent in _world.ActiveEvents)
                _stabilitySystem.ApplyEventImpact(civilization, activeEvent);
        }

        _globalEventSystem.TickEvents(_world);
    }
}

public readonly record struct TurnResult(int Turn, IReadOnlyList<(Civilization Civilization, GameOutcome Outcome)> Outcomes);
