using TTS.Core.Models;
using TTS.Core.Simulation;

namespace TTS.Tests;

public class MatchHostTests
{
    [Fact]
    public void Persistence_RoundTripsWorldState()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-match-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.Sprint8h, path);
            var player = host.World.Civilizations.First(c => c.IsPlayerControlled);
            player.ResearchedTechnologyIds.Add("tech-agriculture");
            host.RunInstantTicks(1);
            host.Save();

            var loaded = MatchHost.Load(path);
            var loadedPlayer = loaded.World.Civilizations.First(c => c.IsPlayerControlled);

            Assert.Equal(host.World.Turn, loaded.World.Turn);
            Assert.Contains("tech-agriculture", loadedPlayer.ResearchedTechnologyIds);
            Assert.Equal(MatchPresets.Sprint8h.ModeId, loaded.World.Match!.Config.ModeId);
            Assert.NotEmpty(loaded.Services.TurnHistory);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryRunDueTick_FirstTickRunsImmediately()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-match-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.Sprint8h, path);
            host.StartMatch(DateTimeOffset.UtcNow);
            var now = DateTimeOffset.UtcNow;

            var result = host.TryRunDueTick(now);

            Assert.Equal(MatchTickOutcome.Completed, result.Outcome);
            Assert.Equal(1, host.World.Match!.TickCount);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryRunDueTick_SecondTickNotDueUntilInterval()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-match-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.Sprint8h, path);
            var now = DateTimeOffset.UtcNow;
            host.StartMatch(now);
            host.TryRunDueTick(now);

            var tooSoon = host.TryRunDueTick(now.AddMinutes(30));

            Assert.Equal(MatchTickOutcome.NotDue, tooSoon.Outcome);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void MatchPresets_ResolveKnownModes()
    {
        Assert.Equal("sprint-8h", MatchPresets.Resolve("sprint").ModeId);
        Assert.Equal("standard-36h", MatchPresets.Resolve("standard-36h").ModeId);
        Assert.Equal("dev-blitz-3m", MatchPresets.Resolve("dev-blitz").ModeId);
        Assert.Equal(TimeSpan.FromSeconds(30), MatchPresets.DevBlitz3m.TickInterval);
    }
}
