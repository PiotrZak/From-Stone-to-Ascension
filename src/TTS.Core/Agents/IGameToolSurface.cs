namespace TTS.Core.Agents;

using TTS.Core.Models;

/// <summary>
/// Contract for Microsoft Agent Framework tools. The simulation remains authoritative;
/// agents read state and propose actions through this surface.
/// </summary>
public interface IGameToolSurface
{
    CivilizationStateSnapshot GetCivilizationState(string civilizationId);
    IReadOnlyDictionary<string, double> GetFactionTensions(string civilizationId);
    IReadOnlyList<Technology> GetTechTreeLayer(TechTier tier);
    IReadOnlyList<GlobalEvent> GetGlobalEvents(bool activeOnly);

    ActionResult SetResearchPriority(string civilizationId, string branch, double weight);
    ActionResult ProposeDiplomaticAction(string civilizationId, string action, string targetCivilizationId);
    ActionResult EmitGlobalEvent(GlobalEvent globalEvent);
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
