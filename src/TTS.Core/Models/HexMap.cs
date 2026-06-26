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
    public int Q { get; }
    public int R { get; }
    public Biome Biome { get; set; }
    public double Elevation { get; set; }
    public double ResourceYield { get; set; }
    public string? ControllingCivilizationId { get; set; }
    public string? RegionId { get; set; }
    public bool IsLand => Biome != Biome.Ocean;

    public HexTile(int q, int r) { Q = q; R = r; }

    public string Key => HexCoordKey.Format(Q, R);
}

public sealed class HexMap
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Seed { get; init; }
    public List<HexTile> Tiles { get; init; } = [];

    private Dictionary<string, HexTile>? _index;

    public HexTile? GetTile(int q, int r)
    {
        _index ??= Tiles.ToDictionary(t => t.Key, StringComparer.Ordinal);
        return _index.TryGetValue(HexCoordKey.Format(q, r), out var tile) ? tile : null;
    }

    public void RebuildIndex() => _index = Tiles.ToDictionary(t => t.Key, StringComparer.Ordinal);
}
