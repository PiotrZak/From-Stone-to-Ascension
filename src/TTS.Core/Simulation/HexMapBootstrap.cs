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
        var worldOptions = WorldHexMapGenerationOptions.ForMatch(match.Config, seed);
        var map = WorldHexMapGenerator.Generate(worldOptions);
        world.Map = map;

        var civs = world.Civilizations.ToList();
        var spawns = WorldHexMapGenerator.PlaceSpawns(map, civs.Count, seed);
        var regions = world.Regions.ToList();

        for (var i = 0; i < civs.Count && i < spawns.Count; i++)
        {
            var civ = civs[i];
            var spawn = spawns[i];
            var region = i < regions.Count ? regions[i] : null;
            var claimed = ClaimCluster(map, spawn.Id, civ.Id, region?.Id, clusterSize: 4);

            if (region is not null)
            {
                region.CapitalHexKey = spawn.Id;
                region.HexKeys.Clear();
                region.HexKeys.AddRange(claimed);
                region.Resources = Math.Clamp(claimed.Average(k =>
                {
                    var tile = map.GetTile(k);
                    return tile?.ResourceYield ?? region.Resources;
                }), 20, 95);
            }
        }

        map.RebuildIndex();
    }

    private static List<string> ClaimCluster(
        HexMap map,
        string startTileId,
        string civilizationId,
        string? regionId,
        int clusterSize)
    {
        var claimed = new List<string>();
        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        queue.Enqueue(startTileId);

        while (queue.Count > 0 && claimed.Count < clusterSize)
        {
            var tileId = queue.Dequeue();
            if (!visited.Add(tileId))
                continue;

            var tile = map.GetTile(tileId);
            if (tile is null || !tile.IsLand)
                continue;

            tile.ControllingCivilizationId = civilizationId;
            tile.RegionId = regionId;
            claimed.Add(tileId);

            foreach (var neighbourId in tile.NeighbourIds)
            {
                if (!visited.Contains(neighbourId))
                    queue.Enqueue(neighbourId);
            }
        }

        return claimed;
    }
}
