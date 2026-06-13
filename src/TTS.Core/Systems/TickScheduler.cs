namespace TTS.Core.Systems;

using TTS.Core.Models;

public sealed class TickScheduler
{
    public bool ShouldTick(MatchState match, DateTimeOffset now) =>
        match.Status == MatchStatus.Running
        && match.TickCount < match.Config.MaxTicks
        && now >= match.NextTickAt;

    public bool AdvanceIfDue(WorldState world, DateTimeOffset now)
    {
        if (world.Match is not { } match || !ShouldTick(match, now))
            return false;

        match.LastTickAt = now;
        match.TickCount++;
        match.NextTickAt = now + match.Config.TickInterval;
        world.SimulatedNow = now;

        if (match.TickCount >= match.Config.MaxTicks)
        {
            match.Status = MatchStatus.Ended;
            match.EndedAt = now;
        }

        return true;
    }

    public void StartMatch(MatchState match, DateTimeOffset now)
    {
        match.Status = MatchStatus.Running;
        match.StartedAt = now;
        match.LastTickAt = now;
        match.NextTickAt = now + match.Config.TickInterval;
    }
}
