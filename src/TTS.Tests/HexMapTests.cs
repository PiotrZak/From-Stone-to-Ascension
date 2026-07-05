using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class HexMapTests
{
    [Fact]
    public void Bootstrap_AttachesMapAndCapitals()
    {
        var world = SampleWorldFactory.Create(MatchPresets.DevBlitz3m, withDemoGate: false, matchId: "match-test-hex");

        Assert.NotNull(world.Map);
        Assert.True(world.Map!.Tiles.Count > 0);
        Assert.Contains(world.Map.Tiles, t => t.WorldRegionId is not null && t.WorldRegionId != WorldMacroRegion.Ocean);
        Assert.All(world.Regions.Where(r => r.ControllingCivilizationId is not null),
            r => Assert.False(string.IsNullOrEmpty(r.CapitalHexKey)));
        Assert.Contains(world.Map.Tiles, t => t.ControllingCivilizationId == "civ-player");
        Assert.Contains(world.Map.Tiles, t => t.ControllingCivilizationId == "civ-rival");
    }

    [Fact]
    public void TerritorySystem_ClaimRequiresAdjacency()
    {
        var world = SampleWorldFactory.Create(MatchPresets.DevBlitz3m, matchId: "match-claim-test");
        var territory = new TerritorySystem();

        var neutral = world.Map!.Tiles.First(t => t.IsLand && t.ControllingCivilizationId is null);
        var isolated = territory.TryClaim(world, "civ-player", neutral.Id);
        Assert.False(isolated.Success);

        var adjacent = world.Map.Tiles.First(t =>
            t.IsLand
            && t.ControllingCivilizationId is null
            && t.NeighbourIds.Any(nid =>
                world.Map.GetTile(nid)?.ControllingCivilizationId == "civ-player"));

        var claim = territory.TryClaim(world, "civ-player", adjacent.Id);
        Assert.True(claim.Success);
        Assert.Equal(adjacent.Id, claim.HexKey);
    }

    [Fact]
    public void Persistence_RoundTripsHexMap()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tts-hex-{Guid.NewGuid():N}.json");
        try
        {
            var host = MatchHost.CreateNew(MatchPresets.DevBlitz3m, path, matchId: "match-persist-hex");
            host.Save();

            var loaded = MatchHost.Load(path);
            Assert.NotNull(loaded.World.Map);
            Assert.Equal(host.World.Map!.Tiles.Count, loaded.World.Map!.Tiles.Count);
            Assert.Equal(
                host.World.Regions.First(r => r.ControllingCivilizationId == "civ-player").CapitalHexKey,
                loaded.World.Regions.First(r => r.ControllingCivilizationId == "civ-player").CapitalHexKey);
            Assert.Equal(host.World.Map.Tiles[0].PolygonVertices.Count, loaded.World.Map.Tiles[0].PolygonVertices.Count);
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
