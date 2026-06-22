namespace TTS.Llm;

/// <summary>Timeouts and caps so LLM agents cannot stall simulation ticks.</summary>
public sealed class AgentSessionLimits
{
    public TimeSpan TurnTimeout { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan AdvisorTimeout { get; init; } = TimeSpan.FromSeconds(25);
    public int MaxToolRounds { get; init; } = 5;
    public int MaxToolCallsPerSession { get; init; } = 12;
    public int MaxTurnCallsPerMatchTick { get; init; } = 4;
    public int MaxAdvisorCallsPerMatchTick { get; init; } = 1;

    public static AgentSessionLimits FromEnvironment()
    {
        var turnMax = ParseInt("TTS_LLM_MAX_TURN_CALLS_PER_TICK", -1);
        if (turnMax <= 0)
            turnMax = ParseInt("TTS_LLM_MAX_CALLS_PER_TICK", 4);

        return new()
        {
            TurnTimeout = TimeSpan.FromSeconds(ParseInt("TTS_LLM_TURN_TIMEOUT_SEC", 20)),
            AdvisorTimeout = TimeSpan.FromSeconds(ParseInt("TTS_LLM_ADVISOR_TIMEOUT_SEC", 25)),
            MaxToolRounds = ParseInt("TTS_LLM_MAX_TOOL_ROUNDS", 5),
            MaxToolCallsPerSession = ParseInt("TTS_LLM_MAX_TOOL_CALLS", 12),
            MaxTurnCallsPerMatchTick = turnMax,
            MaxAdvisorCallsPerMatchTick = ParseInt("TTS_LLM_MAX_ADVISOR_CALLS_PER_TICK", 1),
        };
    }

    private static int ParseInt(string name, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }
}
