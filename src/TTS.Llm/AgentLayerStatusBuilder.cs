namespace TTS.Llm;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;

public sealed record AgentLayerStatus(
    bool ProviderEnabled,
    string Provider,
    string Model,
    bool TurnAgentReady,
    bool WorkflowReady,
    int RivalTierGate,
    int EligibleRivalCount,
    bool AnyRivalEligible,
    int MaxTurnCallsPerTick,
    int TurnCallsUsedThisTick,
    int MaxAdvisorCallsPerTick,
    int AdvisorCallsUsedThisTick,
    string? LastRivalRunner,
    string StatusMessage);

public static class AgentLayerStatusBuilder
{
    public const int RivalAgentTierGate = (int)TechTier.EarlyAI;

    public static AgentLayerStatus BuildForMatch(
        string matchId,
        int tickCount,
        WorldState world,
        IReadOnlyList<TurnSnapshot> history,
        ILlmTurnAgent? turnAgent,
        IAgentWorkflow? workflow)
    {
        var settings = AgentProviderSettings.FromEnvironment();
        var llm = AgentLlmSettings.FromEnvironment(settings.Provider);
        var limits = AgentSessionLimits.FromEnvironment();

        var eligibleRivals = world.Civilizations
            .Count(c => !c.IsPlayerControlled && (int)c.CurrentTier >= RivalAgentTierGate);

        var lastRunner = history
            .OrderByDescending(h => h.Turn)
            .SelectMany(h => h.ResearchDecisions)
            .FirstOrDefault(d => d.Runner is "agent" or "classical-ai")
            .Runner;

        var turnAgentReady = turnAgent?.IsEnabled == true && workflow is AgentToolWorkflow toolWorkflow && toolWorkflow.IsConfigured;
        var message = BuildMessage(settings, turnAgentReady, eligibleRivals, lastRunner);

        return new AgentLayerStatus(
            settings.AgentsEnabled,
            settings.Provider,
            llm.Model,
            turnAgentReady,
            workflow is AgentToolWorkflow configured && configured.IsConfigured,
            RivalAgentTierGate,
            eligibleRivals,
            eligibleRivals > 0,
            limits.MaxTurnCallsPerMatchTick,
            AgentRateLimiter.Shared.GetCallCount(matchId, tickCount, AgentRateLimitScopes.Turn),
            limits.MaxAdvisorCallsPerMatchTick,
            AgentRateLimiter.Shared.GetCallCount(matchId, tickCount, AgentRateLimitScopes.Advisor),
            lastRunner,
            message);
    }

    public static AgentLayerStatus BuildGlobal() =>
        BuildForMatch("global", 0, new WorldState(), [], AgentProviderFactory.CreateTurnAgent(), AgentProviderFactory.CreateWorkflow());

    private static string BuildMessage(
        AgentProviderSettings settings,
        bool turnAgentReady,
        int eligibleRivals,
        string? lastRunner)
    {
        if (!settings.AgentsEnabled)
            return "LLM disabled — set TTS_LLM_PROVIDER=ollama in the silo process.";

        if (!turnAgentReady)
            return "LLM provider configured but agent workflow is not ready — check Ollama and model.";

        if (eligibleRivals == 0)
            return $"Rivals use classical AI until TTS {RivalAgentTierGate} (Early AI). Advisor LLM at TTS 5+.";

        return lastRunner switch
        {
            "agent" => "Rival turns: LLM agent active (last tick used LLM).",
            "classical-ai" => "Rivals eligible for LLM — last tick used classical fallback (rate limit, timeout, or failure).",
            _ => $"Rival LLM ready — {eligibleRivals} rival(s) at TTS {RivalAgentTierGate}+."
        };
    }
}
