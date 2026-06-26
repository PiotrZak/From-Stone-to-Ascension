using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class HexMapTests
{
    [Fact]
    public void Generator_IsDeterministicForSameSeed()
    {
        var options = new HexMapGenerationOptions
        {
            Seed = 42,
            Width = 12,
            Height = 10,
            CivilizationCount = 2
        };

        var first = HexMapGenerator.Generate(options);
        var second = HexMapGenerator.Generate(options);

        Assert.Equal(first.Tiles.Count, second.Tiles.Count);
        for (var i = 0; i < first.Tiles.Count; i++)
        {
            Assert.Equal(first.Tiles[i].Biome, second.Tiles[i].Biome);
            Assert.Equal(first.Tiles[i].ResourceYield, second.Tiles[i].ResourceYield);
        }
    }

    [Fact]
    public void Bootstrap_AttachesMapAndCapitals()
    {
        var world = SampleWorldFactory.Create(MatchPresets.DevBlitz3m, withDemoGate: false, matchId: "match-test-hex");

        Assert.NotNull(world.Map);
        Assert.True(world.Map!.Tiles.Count > 0);
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
        var isolated = territory.TryClaim(world, "civ-player", neutral.Q, neutral.R);
        Assert.False(isolated.Success);

        var adjacent = world.Map.Tiles.First(t =>
            t.IsLand
            && t.ControllingCivilizationId is null
            && HexCoordKey.Neighbors(t.Q, t.R).Any(n =>
            {
                var tile = world.Map.GetTile(n.Q, n.R);
                return tile?.ControllingCivilizationId == "civ-player";
            }));

        var claim = territory.TryClaim(world, "civ-player", adjacent.Q, adjacent.R);
        Assert.True(claim.Success);
        Assert.Equal(adjacent.Key, claim.HexKey);
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
