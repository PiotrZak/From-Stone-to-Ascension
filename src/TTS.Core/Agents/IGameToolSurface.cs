namespace TTS.Core.Agents;

using TTS.Core.Models;
using TTS.Core.Systems;
using TTS.Core.Simulation;

/// <summary>
/// Contract for Microsoft Agent Framework tools. The simulation remains authoritative;
/// agents read state and propose actions through this surface.
/// </summary>
public interface IGameToolSurface
{
    CivilizationStateSnapshot GetCivilizationState(string civilizationId);
    IReadOnlyDictionary<string, double> GetFactionTensions(string civilizationId);
    IReadOnlyList<Technology> GetTechTreeLayer(TechTier tier);
    IReadOnlyList<Technology> GetAvailableTechnologies(string civilizationId);
    IReadOnlyList<GlobalEvent> GetGlobalEvents(bool activeOnly);
    CrimePerspectiveSummary GetCrimePerspective(string civilizationId);
    PolicyResearchAnalysis GetPolicyResearchAnalysis(string civilizationId);
    TechnologyDetailSnapshot GetTechnologyDetail(string technologyId);
    IReadOnlyDictionary<string, double> GetPolicyBranchWeights(string civilizationId);

    ActionResult SetResearchPriority(string civilizationId, string branch, double weight);
    ActionResult ProposeDiplomaticAction(string civilizationId, string action, string targetCivilizationId);
    ProposeResearchResult ProposeResearch(string civilizationId, string technologyId);
    ActionResult EmitGlobalEvent(GlobalEvent globalEvent);
    IReadOnlyList<DecisionGate> GetPendingDecisions(string civilizationId);
    GateResolutionResult ResolveDecision(string civilizationId, string gateId, string optionId);
    AwaySummary GetAwaySummary(int fromTurn, int toTurn);
}

public readonly record struct CivilizationStateSnapshot(
    string Id,
    string Name,
    TechTier CurrentTier,
    double PoliticalStability,
    double EconomicStability,
    double TechnologicalStability,
    IReadOnlyList<string> ResearchedTechnologyIds,
    IReadOnlyList<string> ControlledRegionIds);

public readonly record struct ActionResult(bool Accepted, string Message);

public readonly record struct ProposeResearchResult(bool Accepted, string Message, string? TechnologyId = null);
