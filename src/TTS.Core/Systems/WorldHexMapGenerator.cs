namespace TTS.Core.Systems;

using TTS.Core.Models;

public sealed class WorldHexMapGenerationOptions
{
    public required int Seed { get; init; }
    public required int CivilizationCount { get; init; }
    public int Width { get; init; } = 56;
    public int Height { get; init; } = 28;

    public static WorldHexMapGenerationOptions ForMatch(MatchConfig config, int seed) =>
        config.ModeId is "dev-blitz-3m" or "dev-blitz" or "dev"
            ? new()
            {
                Seed = seed,
                CivilizationCount = Math.Clamp(config.MaxPlayers, 2, 8),
                Width = 42,
                Height = 21
            }
            : new()
            {
                Seed = seed,
                CivilizationCount = Math.Clamp(config.MaxPlayers, 2, 8),
                Width = 56,
                Height = 28
            };
}

/// <summary>Stylized Earth hex map — six continents + ocean, data-inspired yields.</summary>
public static class WorldHexMapGenerator
{
    private readonly record struct Zone(string RegionId, double Cx, double Cy, double Rx, double Ry, double Weight);

    // Approximate world positions (x = longitude, y = latitude), 2:1 aspect.
    private static readonly Zone[] Zones =
    [
        new(WorldMacroRegion.NorthAmerica, 0.17, 0.27, 0.13, 0.21, 1.05),
        new(WorldMacroRegion.SouthAmerica, 0.23, 0.71, 0.085, 0.19, 1.0),
        new(WorldMacroRegion.Europe, 0.455, 0.21, 0.075, 0.11, 1.0),
        new(WorldMacroRegion.Africa, 0.495, 0.55, 0.095, 0.21, 1.0),
        new(WorldMacroRegion.Asia, 0.70, 0.30, 0.17, 0.23, 1.0),
        new(WorldMacroRegion.Australia, 0.83, 0.77, 0.075, 0.10, 1.0)
    ];

    private const double LandThreshold = 0.14;

    public static HexMap Generate(WorldHexMapGenerationOptions options)
    {
        var map = new HexMap
        {
            Width = options.Width,
            Height = options.Height,
            Seed = options.Seed
        };

        for (var r = 0; r < options.Height; r++)
        {
            for (var q = 0; q < options.Width; q++)
            {
                var nx = q / (double)Math.Max(1, options.Width - 1);
                var ny = r / (double)Math.Max(1, options.Height - 1);
                var regionId = ClassifyRegion(nx, ny);
                var isLand = regionId != WorldMacroRegion.Ocean;

                var elevationNoise = ValueNoise.Fbm(options.Seed, nx * 3.0, ny * 3.0);
                var moistureNoise = ValueNoise.Fbm(options.Seed + 17_371, nx * 3.0 + 3.1, ny * 3.0 + 1.7);

                var elevation = isLand
                    ? 0.48 + elevationNoise * 0.32
                    : 0.12 + elevationNoise * 0.08;

                var biome = isLand
                    ? ClassifyBiome(regionId, elevation, moistureNoise)
                    : Biome.Ocean;

                map.Tiles.Add(new HexTile(q, r)
                {
                    Biome = biome,
                    Elevation = elevation,
                    WorldRegionId = regionId
                });
            }
        }

        map.RebuildIndex();
        ApplyMaritimeCoasts(map, options.Seed);
        map.RebuildIndex();
        return map;
    }

    private static void ApplyMaritimeCoasts(HexMap map, int seed)
    {
        foreach (var tile in map.Tiles)
        {
            var oceanNeighbors = CountOceanNeighbors(map, tile.Q, tile.R);

            if (tile.IsLand && oceanNeighbors > 0 && tile.Biome is Biome.Plains or Biome.Forest or Biome.Desert or Biome.Wetlands)
                tile.Biome = Biome.Coast;

            tile.ResourceYield = WorldRegionResourceProfiles.ComputeYield(
                tile.WorldRegionId,
                tile.Biome,
                tile.Elevation,
                seed,
                tile.Q,
                tile.R,
                oceanNeighbors);
        }
    }

    private static int CountOceanNeighbors(HexMap map, int q, int r)
    {
        var count = 0;
        foreach (var neighbor in HexCoordKey.Neighbors(q, r))
        {
            var tile = map.GetTile(neighbor.Q, neighbor.R);
            if (tile is { Biome: Biome.Ocean })
                count++;
        }

        return count;
    }

    public static IReadOnlyList<HexCoord> PlaceSpawns(HexMap map, int civilizationCount, int seed)
    {
        var spawns = new List<HexCoord>();
        var rng = new Random(seed ^ 0x5F3759DF);

        for (var i = 0; i < civilizationCount; i++)
        {
            var preferred = WorldMacroRegion.SpawnOrder[i % WorldMacroRegion.SpawnOrder.Length];
            var candidates = map.Tiles
                .Where(t => t.IsLand
                    && t.Biome is not Biome.Mountains
                    && t.WorldRegionId == preferred)
                .OrderByDescending(t => t.ResourceYield)
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = map.Tiles
                    .Where(t => t.IsLand && t.Biome is not Biome.Mountains)
                    .OrderByDescending(t => t.ResourceYield)
                    .ToList();
            }

            HexTile? pick = null;
            for (var attempt = 0; attempt < 80 && candidates.Count > 0; attempt++)
            {
                var idx = attempt < 12 ? attempt % candidates.Count : rng.Next(candidates.Count);
                var tile = candidates[Math.Min(idx, candidates.Count - 1)];
                if (spawns.All(s => HexMapGenerator.HexDistance(s.Q, s.R, tile.Q, tile.R) >= 5))
                {
                    pick = tile;
                    break;
                }
            }

            pick ??= candidates.FirstOrDefault(t =>
                spawns.All(s => HexMapGenerator.HexDistance(s.Q, s.R, t.Q, t.R) >= 3));

            if (pick is not null)
                spawns.Add(new HexCoord(pick.Q, pick.R));
        }

        return spawns;
    }

    private static string ClassifyRegion(double nx, double ny)
    {
        var bestId = WorldMacroRegion.Ocean;
        var bestScore = 0.0;

        foreach (var zone in Zones)
        {
            var dx = (nx - zone.Cx) / zone.Rx;
            var dy = (ny - zone.Cy) / zone.Ry;
            var score = zone.Weight * Math.Exp(-(dx * dx + dy * dy));
            if (score > bestScore)
            {
                bestScore = score;
                bestId = zone.RegionId;
            }
        }

        return bestScore >= LandThreshold ? bestId : WorldMacroRegion.Ocean;
    }

    private static Biome ClassifyBiome(string regionId, double elevation, double moisture)
    {
        if (elevation > 0.78)
            return Biome.Mountains;

        return regionId switch
        {
            WorldMacroRegion.NorthAmerica => moisture < 0.38 ? Biome.Plains : Biome.Forest,
            WorldMacroRegion.SouthAmerica => moisture > 0.42 ? Biome.Forest : Biome.Plains,
            WorldMacroRegion.Europe => moisture > 0.48 ? Biome.Forest : Biome.Plains,
            WorldMacroRegion.Africa => moisture < 0.34 ? Biome.Desert
                : moisture > 0.55 ? Biome.Wetlands : Biome.Plains,
            WorldMacroRegion.Asia => moisture < 0.28 ? Biome.Desert
                : moisture > 0.5 ? Biome.Forest : Biome.Plains,
            WorldMacroRegion.Australia => moisture < 0.42 ? Biome.Desert : Biome.Plains,
            _ => Biome.Plains
        };
    }
}
