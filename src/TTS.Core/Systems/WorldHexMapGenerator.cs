namespace TTS.Core.Systems;

using TTS.Core.Models;

public sealed class WorldHexMapGenerationOptions
{
    public required int Seed { get; init; }
    public required int CivilizationCount { get; init; }
    /// <summary>Icosahedron subdivision depth (dual tile count grows ~10×4^n).</summary>
    public int Frequency { get; init; } = 4;
    public double PlanetRadius { get; init; } = 100;

    public static WorldHexMapGenerationOptions ForMatch(MatchConfig config, int seed) =>
        config.ModeId is "dev-blitz-3m" or "dev-blitz" or "dev"
            ? new()
            {
                Seed = seed,
                CivilizationCount = Math.Clamp(config.MaxPlayers, 2, 8),
                Frequency = 3,
                PlanetRadius = 100
            }
            : new()
            {
                Seed = seed,
                CivilizationCount = Math.Clamp(config.MaxPlayers, 2, 8),
                Frequency = 4,
                PlanetRadius = 100
            };
}

/// <summary>Goldberg polyhedron world — terrain painted from spherical lat/lon.</summary>
public static class WorldHexMapGenerator
{
    private static readonly EarthZoneLayout.Zone[] Zones = EarthZoneLayout.ContinentZones;

    private const double LandThreshold = EarthZoneLayout.LandThreshold;

    public static HexMap Generate(WorldHexMapGenerationOptions options)
    {
        var meshes = GoldbergPolyhedronGenerator.Generate(options.Frequency, options.PlanetRadius);
        var map = new HexMap
        {
            PlanetRadius = options.PlanetRadius,
            Frequency = options.Frequency,
            Seed = options.Seed
        };

        foreach (var mesh in meshes)
        {
            var (nx, ny) = LatLonToNormalizedMap(mesh.Normal);
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

            map.Tiles.Add(new HexTile(mesh.Id)
            {
                Biome = biome,
                Elevation = elevation,
                WorldRegionId = regionId,
                CenterX = mesh.Center.X,
                CenterY = mesh.Center.Y,
                CenterZ = mesh.Center.Z,
                NormalX = mesh.Normal.X,
                NormalY = mesh.Normal.Y,
                NormalZ = mesh.Normal.Z,
                PolygonVertices = mesh.PolygonVertices
                    .Select(v => (v.X, v.Y, v.Z))
                    .ToList(),
                NeighbourIds = mesh.NeighbourIds.ToList()
            });
        }

        map.RebuildIndex();
        ApplyMaritimeCoasts(map, options.Seed);
        map.RebuildIndex();
        return map;
    }

    private static (double Nx, double Ny) LatLonToNormalizedMap(SphereVec3 normal) =>
        EarthZoneLayout.NormalToNormalized(normal);

    private static void ApplyMaritimeCoasts(HexMap map, int seed)
    {
        foreach (var tile in map.Tiles)
        {
            var oceanNeighbors = CountOceanNeighbors(map, tile);

            if (tile.IsLand && oceanNeighbors > 0 && tile.Biome is Biome.Plains or Biome.Forest or Biome.Desert or Biome.Wetlands)
                tile.Biome = Biome.Coast;

            tile.ResourceYield = WorldRegionResourceProfiles.ComputeYield(
                tile.WorldRegionId,
                tile.Biome,
                tile.Elevation,
                seed,
                TileCoordHash(tile.Id),
                TileCoordHash(tile.Id) ^ unchecked((int)0x9E3779B9),
                oceanNeighbors);
        }
    }

    private static int TileCoordHash(string tileId)
    {
        var hash = 0;
        foreach (var ch in tileId)
            hash = (hash * 31) + ch;
        return Math.Abs(hash);
    }

    private static int CountOceanNeighbors(HexMap map, HexTile tile)
    {
        var count = 0;
        foreach (var neighbourId in tile.NeighbourIds)
        {
            if (map.GetTile(neighbourId) is { Biome: Biome.Ocean })
                count++;
        }

        return count;
    }

    public static IReadOnlyList<HexTile> PlaceSpawns(HexMap map, int civilizationCount, int seed)
    {
        var spawns = new List<HexTile>();
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
                if (spawns.All(s => map.GraphDistance(s.Id, tile.Id) >= 5))
                {
                    pick = tile;
                    break;
                }
            }

            pick ??= candidates.FirstOrDefault(t =>
                spawns.All(s => map.GraphDistance(s.Id, t.Id) >= 3));

            if (pick is not null)
                spawns.Add(pick);
        }

        return spawns;
    }

    private static string ClassifyRegion(double nx, double ny) =>
        EarthZoneLayout.ClassifyRegion(nx, ny, Zones, LandThreshold);

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
