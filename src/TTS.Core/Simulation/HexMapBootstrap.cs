namespace TTS.Core.Simulation;

using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Generates hex geography and links tiles to standard arena regions.</summary>
public static class HexMapBootstrap
{
    public static void Attach(WorldState world)
    {
        var match = world.Match ?? throw new InvalidOperationException("World has no match.");
        var seed = match.WorldSeed;
        var options = HexMapGenerationOptions.ForMatch(match.Config, seed);
        var map = HexMapGenerator.Generate(options);
        world.Map = map;

        var civs = world.Civilizations.ToList();
        var spawns = HexMapGenerator.PlaceSpawns(map, civs.Count, seed);
        var regions = world.Regions.ToList();

        for (var i = 0; i < civs.Count && i < spawns.Count; i++)
        {
            var civ = civs[i];
            var spawn = spawns[i];
            var region = i < regions.Count ? regions[i] : null;
            var claimed = ClaimCluster(map, spawn.Q, spawn.R, civ.Id, region?.Id, clusterSize: 4);

            if (region is not null)
            {
                region.CapitalHexKey = spawn.Key;
                region.HexKeys.Clear();
                region.HexKeys.AddRange(claimed);
                region.Resources = Math.Clamp(claimed.Average(k =>
                {
                    var tile = map.GetTile(HexCoord.Parse(k).Q, HexCoord.Parse(k).R);
                    return tile?.ResourceYield ?? region.Resources;
                }), 20, 95);
            }
        }

        map.RebuildIndex();
    }

    private static List<string> ClaimCluster(
        HexMap map,
        int q,
        int r,
        string civilizationId,
        string? regionId,
        int clusterSize)
    {
        var claimed = new List<string>();
        var queue = new Queue<HexCoord>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        queue.Enqueue(new HexCoord(q, r));

        while (queue.Count > 0 && claimed.Count < clusterSize)
        {
            var coord = queue.Dequeue();
            if (!visited.Add(coord.Key))
                continue;

            var tile = map.GetTile(coord.Q, coord.R);
            if (tile is null || !tile.IsLand)
                continue;

            tile.ControllingCivilizationId = civilizationId;
            tile.RegionId = regionId;
            claimed.Add(coord.Key);

            foreach (var neighbor in HexCoordKey.Neighbors(coord.Q, coord.R))
            {
                if (!visited.Contains(neighbor.Key))
                    queue.Enqueue(neighbor);
            }
        }

        return claimed;
    }
}
