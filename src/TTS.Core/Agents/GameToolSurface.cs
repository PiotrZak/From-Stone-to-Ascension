namespace TTS.Core.Agents;

using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

/// <summary>
/// Default implementation of agent-facing tools over <see cref="WorldState"/>.
/// MAF agents (TTS 5+) call these methods; classical AI uses systems directly below TTS 5.
/// </summary>
public class GameToolSurface : IGameToolSurface
{
    private readonly WorldState _world;
    private readonly SimulationServices _services;

    public GameToolSurface(WorldState world) : this(world, new SimulationServices())
    {
    }

    public GameToolSurface(WorldState world, SimulationServices services)
    {
        _world = world;
        _services = services;
    }

    public CivilizationStateSnapshot GetCivilizationState(string civilizationId)
    {
        var civ = RequireCivilization(civilizationId);
        return new CivilizationStateSnapshot(
            civ.Id,
            civ.Name,
            civ.CurrentTier,
            civ.PoliticalStability,
            civ.EconomicStability,
            civ.TechnologicalStability,
            civ.ResearchedTechnologyIds.ToList(),
            civ.ControlledRegionIds.ToList());
    }

    public IReadOnlyDictionary<string, double> GetFactionTensions(string civilizationId)
    {
        var civ = RequireCivilization(civilizationId);
        return _services.Faction.GetFactionTensions(civ);
    }

    public IReadOnlyList<Technology> GetTechTreeLayer(TechTier tier) =>
        _world.Technologies.Where(t => t.Tier == tier).ToList();

    public IReadOnlyList<Technology> GetAvailableTechnologies(string civilizationId)
    {
        var civ = RequireCivilization(civilizationId);
        return _services.TechTree.GetAvailableTechnologies(civ, _world).ToList();
    }

    public IReadOnlyList<GlobalEvent> GetGlobalEvents(bool activeOnly) =>
        _world.ActiveEvents.ToList();

    public CrimePerspectiveSummary GetCrimePerspective(string civilizationId)
    {
        var civ = RequireCivilization(civilizationId);
        return _services.Crime.GetPerspective(civ, _world);
    }

    public PolicyResearchAnalysis GetPolicyResearchAnalysis(string civilizationId)
    {
        var civ = RequireCivilization(civilizationId);
        return _services.AutoPolicy.Analyze(civ, _world, civ.Policy);
    }

    public TechnologyDetailSnapshot GetTechnologyDetail(string technologyId)
    {
        var technology = _world.Technologies.FirstOrDefault(t => t.Id == technologyId)
            ?? throw new KeyNotFoundException($"Technology '{technologyId}' not found.");
        var branch = TechBranchMapping.Describe(technology);
        return new TechnologyDetailSnapshot(
            technology.Id,
            technology.Name,
            technology.Tier,
            branch.Category,
            branch.Branch,
            branch.FusionTags,
            technology.Prerequisites,
            technology.RiskLevel,
            technology.IsForbidden);
    }

    public IReadOnlyDictionary<string, double> GetPolicyBranchWeights(string civilizationId)
    {
        var civ = RequireCivilization(civilizationId);
        return civ.Policy.BranchWeights;
    }

    public ActionResult SetResearchPriority(string civilizationId, string branch, double weight)
    {
        if (weight is < 0 or > 1)
            return new ActionResult(false, "Research priority weight must be between 0 and 1.");

        var civ = RequireCivilization(civilizationId);
        if ((int)civ.CurrentTier < (int)TechTier.EarlyAI)
            return new ActionResult(false, "Agent research priority is available from TTS 5 onward.");

        return new ActionResult(true, $"Recorded research priority for '{branch}' at {weight:P0}.");
    }

    public ActionResult ProposeDiplomaticAction(string civilizationId, string action, string targetCivilizationId)
    {
        _ = RequireCivilization(civilizationId);
        _ = RequireCivilization(targetCivilizationId);
        return new ActionResult(true, $"Diplomatic proposal '{action}' queued for review.");
    }

    public ProposeResearchResult ProposeResearch(string civilizationId, string technologyId)
    {
        var civ = RequireCivilization(civilizationId);
        if (!_world.Technologies.Any(t => t.Id == technologyId))
            return new ProposeResearchResult(false, $"Technology '{technologyId}' not found.");

        var technology = _world.Technologies.First(t => t.Id == technologyId);
        if (!_services.TechTree.CanResearch(civ, technology))
            return new ProposeResearchResult(false, "Prerequisites not met or technology already researched.");

        var result = _services.Research.Execute(civ, technology, _world);
        return new ProposeResearchResult(result.Success, result.Message, result.TechnologyId);
    }

    public ActionResult EmitGlobalEvent(GlobalEvent globalEvent)
    {
        _services.GlobalEvents.EmitEvent(_world, globalEvent);
        _services.RecordNewEvent(globalEvent.Name);
        return new ActionResult(true, $"Global event '{globalEvent.Name}' applied.");
    }

    public IReadOnlyList<DecisionGate> GetPendingDecisions(string civilizationId)
    {
        var civ = RequireCivilization(civilizationId);
        return _services.DecisionGates.GetPendingGates(civ);
    }

    public GateResolutionResult ResolveDecision(string civilizationId, string gateId, string optionId)
    {
        var civ = RequireCivilization(civilizationId);
        return _services.DecisionGates.Resolve(_world, civ, gateId, optionId, autoResolved: false, _services);
    }

    public AwaySummary GetAwaySummary(int fromTurn, int toTurn) =>
        _services.AwaySummary.Build(_world, _services.TurnHistory, fromTurn, toTurn);

    private Civilization RequireCivilization(string civilizationId) =>
        _world.Civilizations.FirstOrDefault(c => c.Id == civilizationId)
        ?? throw new KeyNotFoundException($"Civilization '{civilizationId}' not found.");
}
