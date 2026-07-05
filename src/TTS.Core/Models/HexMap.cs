namespace TTS.Core.Models;

public enum Biome
{
    Ocean,
    Coast,
    Plains,
    Forest,
    Hills,
    Mountains,
    Desert,
    Tundra,
    Wetlands
}

public readonly record struct HexCoord(int Q, int R)
{
    public string Key => HexCoordKey.Format(Q, R);

    public static HexCoord Parse(string key)
    {
        var parts = key.Split(',');
        return new HexCoord(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}

public static class HexCoordKey
{
    public static string Format(int q, int r) => $"{q},{r}";

    public static IEnumerable<HexCoord> Neighbors(int q, int r) =>
    [
        new(q + 1, r),
        new(q + 1, r - 1),
        new(q, r - 1),
        new(q - 1, r),
        new(q - 1, r + 1),
        new(q, r + 1)
    ];
}

public sealed class HexTile
{
    public string Id { get; }
    public Biome Biome { get; set; }
    public double Elevation { get; set; }
    public double ResourceYield { get; set; }
    public string? ControllingCivilizationId { get; set; }
    public string? RegionId { get; set; }
    public string? WorldRegionId { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double CenterZ { get; set; }
    public double NormalX { get; set; }
    public double NormalY { get; set; }
    public double NormalZ { get; set; }
    public List<(double X, double Y, double Z)> PolygonVertices { get; init; } = [];
    public List<string> NeighbourIds { get; init; } = [];

    public bool IsLand => Biome != Biome.Ocean;
    public string Key => Id;
    public bool IsPentagon => PolygonVertices.Count == 5;

    public HexTile(string id) => Id = id;
}

public sealed class HexMap
{
    public double PlanetRadius { get; init; }
    public int Frequency { get; init; }
    public int Seed { get; init; }
    public List<HexTile> Tiles { get; init; } = [];

    private Dictionary<string, HexTile>? _index;

    public HexTile? GetTile(string id)
    {
        _index ??= Tiles.ToDictionary(t => t.Id, StringComparer.Ordinal);
        return _index.TryGetValue(id, out var tile) ? tile : null;
    }

    public void RebuildIndex() => _index = Tiles.ToDictionary(t => t.Id, StringComparer.Ordinal);

    public int GraphDistance(string fromId, string toId)
    {
        if (fromId == toId)
            return 0;

        _index ??= Tiles.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var queue = new Queue<(string Id, int Dist)>();
        var visited = new HashSet<string>(StringComparer.Ordinal) { fromId };
        queue.Enqueue((fromId, 0));

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            if (!_index.TryGetValue(current, out var tile))
                continue;

            foreach (var neighbourId in tile.NeighbourIds)
            {
                if (neighbourId == toId)
                    return dist + 1;

                if (visited.Add(neighbourId))
                    queue.Enqueue((neighbourId, dist + 1));
            }
        }

        return int.MaxValue;
    }
}
