using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class MatchResultsTests
{
    [Fact]
    public void MatchLogBuilder_FormatsTickHistory()
    {
        var world = SampleWorldFactory.Create(MatchPresets.DevBlitz3m);
        var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, Path.Combine(Path.GetTempPath(), $"tts-log-{Guid.NewGuid():N}.json"));
        host.RunInstantTicks(2);

        var log = MatchLogBuilder.Build(host.World, host.Services.TurnHistory);

        Assert.Equal(2, log.Count);
        Assert.All(log, entry => Assert.NotEmpty(entry.Lines));
    }

    [Fact]
    public void MatchResultsBuilder_RanksByTierAndStability()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-results-{Guid.NewGuid():N}.json");
        var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path);
        host.RunInstantTicks(3);

        var results = host.GetResults();

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal(2, results[1].Rank);
    }

    [Fact]
    public void TryRunDueTick_EndsMatchAtMaxTicks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-end-{Guid.NewGuid():N}.json");
        var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path);
        var now = DateTimeOffset.UtcNow;
        host.StartMatch(now);

        for (var i = 0; i < MatchPresets.DevBlitz3m.MaxTicks; i++)
        {
            var result = host.TryRunDueTick(now.AddSeconds(i * 31));
            if (i < MatchPresets.DevBlitz3m.MaxTicks - 1)
                Assert.Equal(MatchTickOutcome.Completed, result.Outcome);
        }

        var final = host.TryRunDueTick(now.AddMinutes(10));
        Assert.Equal(MatchTickOutcome.MatchEnded, final.Outcome);
        Assert.Equal(MatchStatus.Ended, host.World.Match!.Status);
        Assert.Equal(MatchPresets.DevBlitz3m.MaxTicks, host.World.Match.TickCount);
    }
}
