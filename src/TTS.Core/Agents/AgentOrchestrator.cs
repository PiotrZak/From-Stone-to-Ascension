namespace TTS.Core.Agents;

using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

/// <summary>
/// Entry point for LLM-backed civilization turns at TTS 5+.
/// Falls back to classical auto-policy when LLM is disabled or fails.
/// </summary>
public class AgentOrchestrator
{
    private readonly IGameToolSurface _tools;
    private readonly ILlmTurnAgent? _llmAgent;
    private readonly SimulationServices? _services;

    public AgentOrchestrator(IGameToolSurface tools, ILlmTurnAgent? llmAgent = null, SimulationServices? services = null)
    {
        _tools = tools;
        _llmAgent = llmAgent;
        _services = services;
    }

    public AgentTurnResult RunTurn(Civilization civilization, WorldState world)
    {
        if ((int)civilization.CurrentTier < (int)TechTier.EarlyAI)
            return AgentTurnResult.Skipped("Classical AI handles turns below TTS 5.");

        if (_llmAgent?.IsEnabled == true)
        {
            var llm = _llmAgent.TryRunTurn(civilization, _tools, TimeSpan.FromSeconds(45));
            if (llm is { UsedAgent: true, TechnologyId: not null })
                return llm.Value;
        }

        if (_services is not null)
        {
            var classical = _services.ClassicalAi.RunTurn(civilization, world);
            if (classical.DidResearch && classical.TechnologyId is not null)
            {
                return AgentTurnResult.Completed(
                    classical.Message,
                    classical.TechnologyId,
                    classical.Evaluation);
            }

            return AgentTurnResult.Completed(classical.Message);
        }

        return RunLegacyStub(civilization);
    }

    private AgentTurnResult RunLegacyStub(Civilization civilization)
    {
        _ = _tools.GetCivilizationState(civilization.Id);
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
