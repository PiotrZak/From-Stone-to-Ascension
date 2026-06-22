namespace TTS.Core.Agents;

/// <summary>Per-turn context for rate-limited LLM agent calls.</summary>
public readonly record struct AgentTurnContext(string MatchId, int TickCount);
