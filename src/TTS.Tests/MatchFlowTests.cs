using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class MatchFlowTests
{
    [Fact]
    public void Lobby_Start_Tick_ResolveGate_WithHexMap()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-flow-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(
                MatchPresets.DevBlitz3m,
                path,
                withDemoGate: true,
                matchId: "match-flow-e2e");

            Assert.Equal(MatchStatus.Lobby, host.World.Match!.Status);
            Assert.NotNull(host.World.Map);

            var now = DateTimeOffset.UtcNow;
            host.StartMatch(now);
            var tick = host.TryRunDueTick(now);
            Assert.Equal(MatchTickOutcome.Completed, tick.Outcome);
            Assert.Equal(1, host.World.Match.TickCount);

            var player = host.World.Civilizations.First(c => c.Id == "civ-player");
            var gate = player.PendingDecisions.FirstOrDefault(g => !g.IsResolved);
            if (gate is not null)
            {
                var resolved = host.ResolveDecision(player.Id, gate.Id, gate.DefaultOptionId);
                Assert.True(resolved.Success);
            }

            host.Save();
            var reloaded = MatchHost.Load(path);
            Assert.NotNull(reloaded.World.Map);
            Assert.True(reloaded.World.Map!.Tiles.Count > 0);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
            var tmp = path + ".tmp";
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
    }

    [Fact]
    public void ClaimTerritory_PersistsAcrossReload()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-claim-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path, matchId: "match-claim-persist");
            var adjacent = host.World.Map!.Tiles.First(t =>
                t.IsLand
                && t.ControllingCivilizationId is null
                && HexCoordKey.Neighbors(t.Q, t.R).Any(n =>
                    host.World.Map.GetTile(n.Q, n.R)?.ControllingCivilizationId == "civ-player"));

            var claim = host.ClaimTerritory("civ-player", adjacent.Q, adjacent.R);
            Assert.True(claim.Success);
            host.Save();

            var reloaded = MatchHost.Load(path);
            var tile = reloaded.World.Map!.GetTile(adjacent.Q, adjacent.R);
            Assert.Equal("civ-player", tile!.ControllingCivilizationId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
            var tmp = path + ".tmp";
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
    }
}
