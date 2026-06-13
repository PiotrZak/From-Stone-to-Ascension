namespace TTS.Core.Simulation;

using TTS.Core.Agents;
using TTS.Core.Models;

public sealed class RegionGrowthPhase : ITurnPhase
{
    public string Name => "RegionGrowth";

    public void Execute(WorldState world, SimulationServices services)
    {
        foreach (var region in world.Regions)
        {
            region.Resources = Math.Clamp(region.Resources + 0.5, 0, 100);
            region.Infrastructure = Math.Clamp(region.Infrastructure + 0.2, 0, 100);
        }
    }
}

public sealed class StabilityDecayPhase : ITurnPhase
{
    public string Name => "StabilityDecay";

    public void Execute(WorldState world, SimulationServices services)
    {
        foreach (var civilization in world.Civilizations)
            services.Stability.ApplyTurnDecay(civilization);
    }
}

public sealed class CivilizationTurnPhase : ITurnPhase
{
    private readonly IReadOnlyList<ICivilizationTurnRunner> _runners;

    public CivilizationTurnPhase(IReadOnlyList<ICivilizationTurnRunner> runners) => _runners = runners;

    public string Name => "CivilizationTurn";

    public void Execute(WorldState world, SimulationServices services)
    {
        foreach (var civilization in world.Civilizations)
        {
            foreach (var runner in _runners)
            {
                if (!runner.CanHandle(civilization, world))
                    continue;

                runner.Run(civilization, world);
                break;
            }
        }
    }
}

public sealed class KnowledgeDiffusionPhase : ITurnPhase
{
    public string Name => "KnowledgeDiffusion";

    public void Execute(WorldState world, SimulationServices services) =>
        services.KnowledgeDiffusion.Diffuse(world);
}

public sealed class FactionInfluencePhase : ITurnPhase
{
    public string Name => "FactionInfluence";

    public void Execute(WorldState world, SimulationServices services)
    {
        foreach (var civilization in world.Civilizations)
            services.Faction.ApplyTurnInfluence(civilization);
    }
}

public sealed class CrimePressurePhase : ITurnPhase
{
    public string Name => "CrimePressure";

    public void Execute(WorldState world, SimulationServices services)
    {
        foreach (var civilization in world.Civilizations)
            services.Crime.ApplyTurnPressure(civilization, world);
    }
}

public sealed class GlobalEventGenerationPhase : ITurnPhase
{
    public string Name => "GlobalEventGeneration";

    public void Execute(WorldState world, SimulationServices services)
    {
        var newEvent = services.GlobalEvents.MaybeGenerateEvent(world);
        if (newEvent is not null)
            services.GlobalEvents.EmitEvent(world, newEvent);
    }
}

public sealed class EventImpactPhase : ITurnPhase
{
    public string Name => "EventImpact";

    public void Execute(WorldState world, SimulationServices services)
    {
        foreach (var civilization in world.Civilizations)
        {
            foreach (var activeEvent in world.ActiveEvents)
                services.Stability.ApplyEventImpact(civilization, activeEvent);
        }
    }
}

public sealed class EventTickPhase : ITurnPhase
{
    public string Name => "EventTick";

    public void Execute(WorldState world, SimulationServices services) =>
        services.GlobalEvents.TickEvents(world);
}

public static class TurnPhasePipeline
{
    public static IReadOnlyList<ITurnPhase> CreateDefault(SimulationServices services, IGameToolSurface tools)
    {
        var orchestrator = new AgentOrchestrator(tools);
        var runners = new ICivilizationTurnRunner[]
        {
            new AgentTurnRunner(orchestrator),
            new ClassicalAiTurnRunner(services)
        };

        return
        [
            new RegionGrowthPhase(),
            new StabilityDecayPhase(),
            new CivilizationTurnPhase(runners),
            new KnowledgeDiffusionPhase(),
            new FactionInfluencePhase(),
            new CrimePressurePhase(),
            new GlobalEventGenerationPhase(),
            new EventImpactPhase(),
            new EventTickPhase()
        ];
    }
}
