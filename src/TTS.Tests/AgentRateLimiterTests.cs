using TTS.Llm;

namespace TTS.Tests;

public class AgentRateLimiterTests
{
    [Fact]
    public void TryAcquire_RespectsPerTickCap()
    {
        var limiter = new AgentRateLimiter();
        const string match = "match-test";
        const int tick = 3;
        const int max = 2;

        Assert.True(limiter.TryAcquire(match, tick, max, AgentRateLimitScopes.Turn));
        Assert.True(limiter.TryAcquire(match, tick, max, AgentRateLimitScopes.Turn));
        Assert.False(limiter.TryAcquire(match, tick, max, AgentRateLimitScopes.Turn));
        Assert.Equal(2, limiter.GetCallCount(match, tick, AgentRateLimitScopes.Turn));
    }

    [Fact]
    public void TryAcquire_ScopesAreIndependent()
    {
        var limiter = new AgentRateLimiter();
        const string match = "match-test";
        const int tick = 1;

        Assert.True(limiter.TryAcquire(match, tick, 1, AgentRateLimitScopes.Turn));
        Assert.True(limiter.TryAcquire(match, tick, 1, AgentRateLimitScopes.Advisor));
        Assert.False(limiter.TryAcquire(match, tick, 1, AgentRateLimitScopes.Turn));
    }

    [Fact]
    public void TryAcquire_ResetsOnNewTick()
    {
        var limiter = new AgentRateLimiter();
        const string match = "match-test";
        const int max = 1;

        Assert.True(limiter.TryAcquire(match, 1, max, AgentRateLimitScopes.Turn));
        Assert.False(limiter.TryAcquire(match, 1, max, AgentRateLimitScopes.Turn));
        Assert.True(limiter.TryAcquire(match, 2, max, AgentRateLimitScopes.Turn));
    }
}
