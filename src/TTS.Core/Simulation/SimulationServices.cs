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
    public EconomySystem Economy { get; } = new();
    public WinLossSystem WinLoss { get; } = new();
    public DecisionGateSystem DecisionGates { get; } = new();
    public AwaySummaryBuilder AwaySummary { get; } = new();
    public TickScheduler Scheduler { get; } = new();
    public AutoPolicySystem AutoPolicy { get; }
    public ClassicalAiSystem ClassicalAi { get; }
    public ILlmTurnAgent? LlmTurnAgent { get; set; }
    public ResearchExecutor Research { get; }
    public List<TurnResearchDecision> TurnResearchDecisions { get; } = [];
    public TurnSnapshot? CurrentTurnSnapshot { get; private set; }
    public List<TurnSnapshot> TurnHistory { get; } = [];

    public SimulationServices()
    {
        AutoPolicy = new AutoPolicySystem(TechTree);
        Research = new ResearchExecutor(TechTree, ForbiddenTech);
        ClassicalAi = new ClassicalAiSystem(this);
    }

    public void BeginTurn(WorldState world)
    {
        TurnResearchDecisions.Clear();
        CurrentTurnSnapshot = new TurnSnapshot
        {
            Turn = world.Turn,
            SimulatedAt = world.SimulatedNow
        };

        foreach (var civilization in world.Civilizations)
        {
            CurrentTurnSnapshot.CivilizationsAtStart[civilization.Id] = new CivTurnStartSnapshot(
                civilization.CurrentTier,
                civilization.AverageStability,
                civilization.ResearchedTechnologyIds.Count,
                civilization.ResearchedTechnologyIds.ToList());
        }
    }

    public void RecordResearchDecision(TurnResearchDecision decision) => TurnResearchDecisions.Add(decision);

    public void RecordGateResolution(GateResolutionRecord record) =>
        CurrentTurnSnapshot?.GateResolutions.Add(record);

    public void RecordNewEvent(string eventName) =>
        CurrentTurnSnapshot?.NewEvents.Add(eventName);

    public void FinalizeTurn(WorldState world)
    {
        if (CurrentTurnSnapshot is null)
            return;

        foreach (var civilization in world.Civilizations)
        {
            if (!CurrentTurnSnapshot.CivilizationsAtStart.TryGetValue(civilization.Id, out var start))
                continue;

            var newTechs = civilization.ResearchedTechnologyIds
                .Except(start.ResearchedTechnologyIds)
                .Select(id => world.Technologies.FirstOrDefault(t => t.Id == id)?.Name ?? id)
                .ToList();

            if (newTechs.Count > 0)
                CurrentTurnSnapshot.ResearchedThisTurn[civilization.Id] = newTechs;

            if (civilization.CurrentTier > start.Tier && !CurrentTurnSnapshot.TierChanges.ContainsKey(civilization.Id))
                CurrentTurnSnapshot.TierChanges[civilization.Id] = new TierChangeRecord(start.Tier, civilization.CurrentTier);
        }

        foreach (var decision in TurnResearchDecisions)
        {
            CurrentTurnSnapshot.ResearchDecisions.Add(new TurnResearchDecisionSnapshot(
                decision.CivilizationId,
                decision.CivilizationName,
                decision.Runner,
                decision.Researched,
                decision.Message));
        }

        TurnHistory.Add(CurrentTurnSnapshot);
        CurrentTurnSnapshot = null;
    }

    public GameToolSurface CreateToolSurface(WorldState world) => new(world, this);

    public GameLoop CreateGameLoop(WorldState world) => new(world, this);
}
