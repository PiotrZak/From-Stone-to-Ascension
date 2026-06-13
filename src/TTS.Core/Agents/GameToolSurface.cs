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

        var result = _services.Research.Execute(civ, technology);
        return new ProposeResearchResult(result.Success, result.Message, result.TechnologyId);
    }

    public ActionResult EmitGlobalEvent(GlobalEvent globalEvent)
    {
        _services.GlobalEvents.EmitEvent(_world, globalEvent);
        return new ActionResult(true, $"Global event '{globalEvent.Name}' applied.");
    }

    private Civilization RequireCivilization(string civilizationId) =>
        _world.Civilizations.FirstOrDefault(c => c.Id == civilizationId)
        ?? throw new KeyNotFoundException($"Civilization '{civilizationId}' not found.");
}
