namespace TTS.Api.Models;

using TTS.Contracts;

public static class LlmStatusMapping
{
    public static LlmLayerStatusDto ToDto(GrainLlmLayerStatus status) => new()
    {
        ProviderEnabled = status.ProviderEnabled,
        Provider = status.Provider,
        Model = status.Model,
        TurnAgentReady = status.TurnAgentReady,
        WorkflowReady = status.WorkflowReady,
        RivalTierGate = status.RivalTierGate,
        EligibleRivalCount = status.EligibleRivalCount,
        AnyRivalEligible = status.AnyRivalEligible,
        MaxTurnCallsPerTick = status.MaxTurnCallsPerTick,
        TurnCallsUsedThisTick = status.TurnCallsUsedThisTick,
        MaxAdvisorCallsPerTick = status.MaxAdvisorCallsPerTick,
        AdvisorCallsUsedThisTick = status.AdvisorCallsUsedThisTick,
        LastRivalRunner = status.LastRivalRunner,
        StatusMessage = status.StatusMessage
    };
}
