using TTS.Core.Models;
using TTS.Core.Simulation;

namespace TTS.Tests;

public class LobbyTests
{
    [Fact]
    public void NewMatch_StartsInLobby()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-lobby-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path);
            Assert.Equal(MatchStatus.Lobby, host.World.Match!.Status);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryRunDueTick_InLobby_IsNotDue()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-lobby-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path);
            var result = host.TryRunDueTick(DateTimeOffset.UtcNow);

            Assert.Equal(MatchTickOutcome.NotDue, result.Outcome);
            Assert.Equal(0, host.World.Match!.TickCount);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void StartMatch_TransitionsToRunning()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-lobby-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path);
            var now = DateTimeOffset.UtcNow;
            host.StartMatch(now);

            Assert.Equal(MatchStatus.Running, host.World.Match!.Status);
            var tick = host.TryRunDueTick(now);
            Assert.Equal(MatchTickOutcome.Completed, tick.Outcome);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void StartMatch_WhenNotLobby_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-lobby-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path);
            var now = DateTimeOffset.UtcNow;
            host.StartMatch(now);

            Assert.Throws<InvalidOperationException>(() => host.StartMatch(now));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
