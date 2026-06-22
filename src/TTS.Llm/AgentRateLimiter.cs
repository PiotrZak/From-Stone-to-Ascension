namespace TTS.Llm;

using System.Collections.Concurrent;

/// <summary>Per-match, per-tick cap on LLM invocations so ticks never wait on unbounded agent calls.</summary>
public sealed class AgentRateLimiter
{
    public static AgentRateLimiter Shared { get; } = new();

    private readonly ConcurrentDictionary<string, int> _calls = new();
    private int _pruneCounter;

    public bool TryAcquire(string matchId, int tickCount, int maxPerTick)
    {
        if (maxPerTick <= 0)
            return false;

        var key = $"{matchId}:{tickCount}";
        while (true)
        {
            var current = _calls.AddOrUpdate(key, 1, static (_, old) => old + 1);
            if (current <= maxPerTick)
            {
                MaybePrune();
                return true;
            }

            _calls.AddOrUpdate(key, 0, static (_, old) => Math.Max(0, old - 1));
            return false;
        }
    }

    public int GetCallCount(string matchId, int tickCount) =>
        _calls.TryGetValue($"{matchId}:{tickCount}", out var n) ? n : 0;

    private void MaybePrune()
    {
        if (Interlocked.Increment(ref _pruneCounter) % 50 != 0)
            return;

        if (_calls.Count <= 200)
            return;

        foreach (var key in _calls.Keys.Take(100).ToList())
            _calls.TryRemove(key, out _);
    }
}
