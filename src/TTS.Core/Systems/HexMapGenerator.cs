namespace TTS.Core.Systems;

using TTS.Core.Models;

public sealed class HexMapGenerationOptions
{
    public required int Seed { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int CivilizationCount { get; init; }

    public static HexMapGenerationOptions ForMatch(MatchConfig config, int seed) =>
        new()
        {
            Seed = seed,
            Width = config.ModeId is "dev-blitz-3m" or "dev-blitz" or "dev" ? 18 : config.MaxPlayers <= 4 ? 24 : 30,
            Height = config.ModeId is "dev-blitz-3m" or "dev-blitz" or "dev" ? 14 : config.MaxPlayers <= 4 ? 18 : 22,
            CivilizationCount = Math.Clamp(config.MaxPlayers, 2, 8)
        };
}

public static class HexMapGenerator
{
    public static HexMap Generate(HexMapGenerationOptions options)
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
                var elevation = ValueNoise.Fbm(options.Seed, nx * 3.5, ny * 3.5);
                var moisture = ValueNoise.Fbm(options.Seed + 17_371, nx * 3.5 + 12.3, ny * 3.5 + 4.7);

                var biome = ClassifyBiome(elevation, moisture);
                var yield = ComputeYield(biome, elevation, options.Seed, q, r);

                map.Tiles.Add(new HexTile(q, r)
                {
                    Biome = biome,
                    Elevation = elevation,
                    ResourceYield = yield
                });
            }
        }

        map.RebuildIndex();
        return map;
    }

    public static IReadOnlyList<HexCoord> PlaceSpawns(HexMap map, int civilizationCount, int seed)
    {
        var land = map.Tiles
            .Where(t => t.IsLand && t.Biome is not Biome.Mountains)
            .OrderBy(t => t.ResourceYield)
            .ToList();

        if (land.Count == 0)
            return [];

        var mid = land.Count / 2;
        var candidates = land.Skip(Math.Max(0, mid - land.Count / 4)).Take(Math.Max(civilizationCount * 8, civilizationCount)).ToList();
        var spawns = new List<HexCoord>();
        var rng = new Random(seed ^ 0x5F3759DF);

        while (spawns.Count < civilizationCount && candidates.Count > 0)
        {
            var idx = rng.Next(candidates.Count);
            var tile = candidates[idx];
            candidates.RemoveAt(idx);

            if (spawns.Any(s => HexDistance(s.Q, s.R, tile.Q, tile.R) < 4))
                continue;

            spawns.Add(new HexCoord(tile.Q, tile.R));
        }

        if (spawns.Count < civilizationCount && land.Count >= civilizationCount)
        {
            foreach (var tile in land.OrderByDescending(t => t.ResourceYield))
            {
                if (spawns.Count >= civilizationCount)
                    break;
                if (spawns.Any(s => s.Q == tile.Q && s.R == tile.R))
                    continue;
                spawns.Add(new HexCoord(tile.Q, tile.R));
            }
        }

        return spawns;
    }

    internal static int HexDistance(int q1, int r1, int q2, int r2)
    {
        var s1 = -q1 - r1;
        var s2 = -q2 - r2;
        return (Math.Abs(q1 - q2) + Math.Abs(r1 - r2) + Math.Abs(s1 - s2)) / 2;
    }

    private static Biome ClassifyBiome(double elevation, double moisture)
    {
        if (elevation < 0.35)
            return Biome.Ocean;
        if (elevation < 0.42)
            return Biome.Coast;
        if (elevation > 0.82)
            return Biome.Mountains;
        if (elevation > 0.68)
            return Biome.Hills;
        if (moisture < 0.28)
            return Biome.Desert;
        if (moisture > 0.72 && elevation < 0.55)
            return Biome.Wetlands;
        if (moisture > 0.55)
            return Biome.Forest;
        if (elevation < 0.48)
            return moisture < 0.4 ? Biome.Tundra : Biome.Plains;
        return Biome.Plains;
    }

    private static double ComputeYield(Biome biome, double elevation, int seed, int q, int r)
    {
        if (biome == Biome.Ocean)
            return 0;

        var baseYield = biome switch
        {
            Biome.Coast => 45,
            Biome.Plains => 55,
            Biome.Forest => 50,
            Biome.Hills => 48,
            Biome.Mountains => 35,
            Biome.Desert => 30,
            Biome.Tundra => 28,
            Biome.Wetlands => 42,
            _ => 40
        };

        var jitter = (ValueNoise.Sample(seed + 911, q * 0.7, r * 0.7) - 0.5) * 10;
        return Math.Clamp(baseYield * (0.7 + 0.3 * elevation) + jitter, 5, 95);
    }
}
