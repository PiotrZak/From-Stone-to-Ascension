namespace TTS.Core.Agents;

using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>
/// Entry point for MAF-backed civilization turns. At TTS 5+ this orchestrator
/// can be wired to Microsoft Agent Framework workflows; below TTS 5 it no-ops.
/// </summary>
public class AgentOrchestrator
{
    private readonly IGameToolSurface _tools;

    public AgentOrchestrator(IGameToolSurface tools) => _tools = tools;

    public AgentTurnResult RunTurn(Civilization civilization, WorldState world)
    {
        if ((int)civilization.CurrentTier < (int)TechTier.EarlyAI)
            return AgentTurnResult.Skipped("Classical AI handles turns below TTS 5.");

        _ = _tools.GetCivilizationState(civilization.Id);
        _ = _tools.GetFactionTensions(civilization.Id);

        var candidate = _tools.GetAvailableTechnologies(civilization.Id)
            .OrderByDescending(t => t.RiskLevel)
            .FirstOrDefault();

        if (candidate is null)
            return AgentTurnResult.Completed("No research candidates available.");

        var branch = TechBranchMapping.CategoryToBranch(candidate.Category);
        var evaluation = new ResearchCandidateEvaluation(
            candidate.Id,
            candidate.Name,
            candidate.Category,
            branch,
            candidate.FusionTags,
            candidate.Tier,
            candidate.RiskLevel,
            candidate.IsForbidden,
            AllowedByRisk: true,
            BranchWeightScore: 0,
            StanceBonus: candidate.RiskLevel,
            TotalScore: candidate.RiskLevel);

        var result = _tools.ProposeResearch(civilization.Id, candidate.Id);
        return result.Accepted
            ? AgentTurnResult.Completed($"Agent researched '{candidate.Name}'.", candidate.Id, evaluation)
            : AgentTurnResult.Completed(result.Message, candidate.Id, evaluation);
    }
}

public readonly record struct AgentTurnResult(
    bool UsedAgent,
    string Message,
    string? TechnologyId = null,
    ResearchCandidateEvaluation? Evaluation = null)
{
    public static AgentTurnResult Skipped(string message) => new(false, message);
    public static AgentTurnResult Completed(string message, string? technologyId = null, ResearchCandidateEvaluation? evaluation = null) =>
        new(true, message, technologyId, evaluation);
}
