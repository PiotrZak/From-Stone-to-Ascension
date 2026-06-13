namespace TTS.Core.Simulation;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Composition root — single shared instance of all simulation systems.</summary>
public sealed class SimulationServices
{
    public TechTreeSystem TechTree { get; } = new();
    public ForbiddenTechSystem ForbiddenTech { get; } = new();
    public StabilitySystem Stability { get; } = new();
    public FactionSystem Faction { get; } = new();
    public GlobalEventSystem GlobalEvents { get; } = new();
    public KnowledgeDiffusionSystem KnowledgeDiffusion { get; } = new();
    public CrimeSystem Crime { get; } = new();
    public WinLossSystem WinLoss { get; } = new();
    public AutoPolicySystem AutoPolicy { get; }
    public ClassicalAiSystem ClassicalAi { get; }
    public ResearchExecutor Research { get; }

    public SimulationServices()
    {
        AutoPolicy = new AutoPolicySystem(TechTree);
        Research = new ResearchExecutor(TechTree, ForbiddenTech);
        ClassicalAi = new ClassicalAiSystem(this);
    }

    public GameToolSurface CreateToolSurface(WorldState world) => new(world, this);

    public GameLoop CreateGameLoop(WorldState world) => new(world, this);
}
