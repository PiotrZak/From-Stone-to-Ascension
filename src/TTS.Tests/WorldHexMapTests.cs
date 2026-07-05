using TTS.Core.Models;
using TTS.Core.Systems;
using Xunit;

namespace TTS.Tests;

public class WorldHexMapTests
{
    [Fact]
    public void Generate_IsDeterministic()
    {
        var options = new WorldHexMapGenerationOptions
        {
            Seed = 42,
            CivilizationCount = 2,
            Width = 42,
            Height = 21
        };

        var first = WorldHexMapGenerator.Generate(options);
        var second = WorldHexMapGenerator.Generate(options);

        Assert.Equal(first.Tiles.Count, second.Tiles.Count);
        for (var i = 0; i < first.Tiles.Count; i++)
        {
            Assert.Equal(first.Tiles[i].Biome, second.Tiles[i].Biome);
            Assert.Equal(first.Tiles[i].WorldRegionId, second.Tiles[i].WorldRegionId);
            Assert.Equal(first.Tiles[i].ResourceYield, second.Tiles[i].ResourceYield);
        }
    }

    [Fact]
    public void Generate_ContainsAllContinents()
    {
        var map = WorldHexMapGenerator.Generate(new WorldHexMapGenerationOptions
        {
            Seed = 7,
            CivilizationCount = 4,
            Width = 56,
            Height = 28
        });

        var landRegions = map.Tiles
            .Where(t => t.IsLand)
            .Select(t => t.WorldRegionId)
            .Distinct()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var continent in WorldMacroRegion.Continents)
            Assert.Contains(continent, landRegions);

        Assert.DoesNotContain(WorldMacroRegion.Britain, landRegions);
        Assert.DoesNotContain(WorldMacroRegion.MiddleEast, landRegions);
    }

    [Fact]
    public void PlaceSpawns_PrefersDistinctMacroRegions()
    {
        var map = WorldHexMapGenerator.Generate(new WorldHexMapGenerationOptions
        {
            Seed = 99,
            CivilizationCount = 4,
            Width = 56,
            Height = 28
        });

        var spawns = WorldHexMapGenerator.PlaceSpawns(map, 4, 99);
        Assert.Equal(4, spawns.Count);

        var regions = spawns
            .Select(s => map.GetTile(s.Q, s.R)?.WorldRegionId)
            .Where(r => r is not null)
            .Distinct()
            .ToList();

        Assert.True(regions.Count >= 3, $"Expected diverse spawns, got: {string.Join(", ", regions)}");
    }

    [Fact]
    public void Asia_HasStrongResourceYieldsOnDesertTiles()
    {
        var map = WorldHexMapGenerator.Generate(new WorldHexMapGenerationOptions
        {
            Seed = 11,
            CivilizationCount = 2,
            Width = 56,
            Height = 28
        });

        var asiaDesertAvg = map.Tiles
            .Where(t => t.WorldRegionId == WorldMacroRegion.Asia && t.Biome == Biome.Desert)
            .Average(t => t.ResourceYield);

        Assert.True(asiaDesertAvg > 40);
    }

    [Fact]
    public void Generate_OceanTilesHaveMaritimeYields()
    {
        var map = WorldHexMapGenerator.Generate(new WorldHexMapGenerationOptions
        {
            Seed = 3,
            CivilizationCount = 2,
            Width = 56,
            Height = 28
        });

        var oceanTiles = map.Tiles.Where(t => t.Biome == Biome.Ocean).ToList();
        Assert.NotEmpty(oceanTiles);
        Assert.All(oceanTiles, t => Assert.InRange(t.ResourceYield, 6, 32));
    }

    [Fact]
    public void Generate_CoastalLandTilesBorderOcean()
    {
        var map = WorldHexMapGenerator.Generate(new WorldHexMapGenerationOptions
        {
            Seed = 5,
            CivilizationCount = 2,
            Width = 56,
            Height = 28
        });

        var coastTiles = map.Tiles.Where(t => t.Biome == Biome.Coast).ToList();
        Assert.NotEmpty(coastTiles);
        Assert.All(coastTiles, t =>
            Assert.True(CountOceanNeighbors(map, t.Q, t.R) > 0, $"Coast tile {t.Key} should touch ocean"));
    }

    [Fact]
    public void Generate_CoastalTilesOutyieldInlandPlains()
    {
        var map = WorldHexMapGenerator.Generate(new WorldHexMapGenerationOptions
        {
            Seed = 13,
            CivilizationCount = 2,
            Width = 56,
            Height = 28
        });

        var coastAvg = map.Tiles.Where(t => t.Biome == Biome.Coast).Average(t => t.ResourceYield);
        var inlandAvg = map.Tiles
            .Where(t => t.IsLand
                && t.Biome is Biome.Plains or Biome.Forest
                && CountOceanNeighbors(map, t.Q, t.R) == 0)
            .Average(t => t.ResourceYield);

        Assert.True(coastAvg > inlandAvg, $"coast {coastAvg:F1} vs inland {inlandAvg:F1}");
    }

    private static int CountOceanNeighbors(HexMap map, int q, int r)
    {
        var count = 0;
        foreach (var neighbor in HexCoordKey.Neighbors(q, r))
        {
            if (map.GetTile(neighbor.Q, neighbor.R) is { Biome: Biome.Ocean })
                count++;
        }

        return count;
    }
}
