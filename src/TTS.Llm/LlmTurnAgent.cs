namespace TTS.Llm;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Runs LLM agent turns via Microsoft Agent Framework with classical fallback.</summary>
public sealed class LlmTurnAgent : ILlmTurnAgent, IDisposable
{
    private readonly AgentProviderSettings _settings;
    private readonly AgentSessionLimits _limits;
    private readonly IAgentWorkflow _workflow;

    public LlmTurnAgent(
        AgentProviderSettings? settings = null,
        AgentSessionLimits? limits = null,
        IAgentWorkflow? workflow = null)
    {
        _settings = settings ?? AgentProviderSettings.FromEnvironment();
        _limits = limits ?? AgentSessionLimits.FromEnvironment();
        _workflow = workflow ?? AgentProviderFactory.CreateWorkflow(_settings)
            ?? new AgentToolWorkflow(_settings);
    }

    public bool IsEnabled =>
        _settings.AgentsEnabled
        && _settings.Provider.ToLowerInvariant() is "ollama" or "openai" or "gemini";

    public AgentTurnResult? TryRunTurn(
        Civilization civilization,
        WorldState world,
        IGameToolSurface tools,
        AgentTurnContext context,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || (int)civilization.CurrentTier < (int)TechTier.EarlyAI)
            return null;

        if (!AgentRateLimiter.Shared.TryAcquire(
                context.MatchId,
                context.TickCount,
                _limits.MaxLlmCallsPerMatchTick))
            return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_limits.TurnTimeout);

            var rivals = BuildRivals(world, civilization.Id);
            var result = _workflow
                .RunCivilizationTurnAsync(civilization, tools, rivals, _limits, cts.Token)
                .GetAwaiter()
                .GetResult();

            if (!result.Success)
                return null;

            return BuildTurnResult(tools, result);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<RivalSummary> BuildRivals(WorldState world, string civId) =>
        world.Civilizations
            .Where(c => c.Id != civId)
            .Select(c => new RivalSummary(c.Id, c.Name, (int)c.CurrentTier, c.AverageStability))
            .ToList();

    private static AgentTurnResult? BuildTurnResult(IGameToolSurface tools, AgentSessionResult result)
    {
        var diplomacy = result.DiplomaticActions.Count > 0
            ? $" Diplomacy: {string.Join("; ", result.DiplomaticActions)}."
            : "";

        if (result.ResearchedTechnologyId is { } techId)
        {
            ResearchCandidateEvaluation? evaluation = null;
            try
            {
                var tech = tools.GetTechnologyDetail(techId);
                evaluation = new ResearchCandidateEvaluation(
                    tech.Id,
                    tech.Name,
                    tech.Category,
                    tech.Branch,
                    tech.FusionTags,
                    tech.Tier,
                    tech.RiskLevel,
                    tech.IsForbidden,
                    AllowedByRisk: true,
                    BranchWeightScore: 0,
                    StanceBonus: 0,
                    TotalScore: 0);
            }
            catch
            {
                /* ignore */
            }

            return AgentTurnResult.Completed(
                $"LLM agent researched '{techId}'.{diplomacy}",
                techId,
                evaluation);
        }

        if (result.DiplomaticActions.Count > 0)
            return AgentTurnResult.Completed($"LLM diplomacy.{diplomacy.Trim()}");

        return string.IsNullOrWhiteSpace(result.Message)
            ? null
            : AgentTurnResult.Completed(result.Message);
    }

    public void Dispose()
    {
        if (_workflow is IDisposable disposable)
            disposable.Dispose();
    }
}
