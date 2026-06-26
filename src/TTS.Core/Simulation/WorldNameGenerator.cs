namespace TTS.Core.Simulation;

/// <summary>Seeded fantasy names for civilizations, cities, and factions.</summary>
public static class WorldNameGenerator
{
    private static readonly string[] CivPrefixes =
    [
        "Aurora", "Iron", "Solar", "Crimson", "Northwind", "Obsidian", "Verdant", "Silver",
        "Azure", "Golden", "Storm", "Ember", "Frost", "Cobalt", "Radiant", "Shadow"
    ];

    private static readonly string[] CivSuffixes =
    [
        "Collective", "Dominion", "Pact", "League", "Concord", "Syndicate", "Assembly", "Reach",
        "Commonwealth", "Union", "Compact", "Federation", "Order", "Consortium"
    ];

    private static readonly string[] CityRoots =
    [
        "Meridian", "Redstone", "Northwind", "Green Basin", "Iron Coast", "Sunfall", "Deepwater",
        "Highland", "Clearwater", "Stonegate", "Brightford", "Kestrel", "Harborview", "Westmark"
    ];

    private static readonly string[] CitySuffixes =
    [
        "Bay", "Harbor", "Reach", "Basin", "Heights", "Crossing", "Delta", "Springs", "Gate", "Quarter"
    ];

    private static readonly string[] FactionNames =
    [
        "Central Council", "Helix Industries", "Silent Lattice", "Open Market Guild", "Guardian Circle",
        "River Compact", "Skyward Institute", "Deep Root Society", "Signal Chorus", "Iron Ledger"
    ];

    public static string CivilizationName(int seed, int index)
    {
        var rng = CreateRng(MatchSeeds.Mix(seed, index * 997));
        return $"{Pick(CivPrefixes, rng)} {Pick(CivSuffixes, rng)}";
    }

    public static string RegionName(int seed, int index, string? anchorState = null)
    {
        var rng = CreateRng(MatchSeeds.Mix(seed, index * 503 + (anchorState?.GetHashCode(StringComparison.Ordinal) ?? 0)));
        var name = $"{Pick(CityRoots, rng)} {Pick(CitySuffixes, rng)}";
        return name;
    }

    public static string FactionName(int seed, int civIndex, int factionIndex)
    {
        var rng = CreateRng(MatchSeeds.Mix(seed, civIndex * 131 + factionIndex * 17));
        return Pick(FactionNames, rng);
    }

    private static Random CreateRng(int seed) => new(seed);

    private static string Pick(IReadOnlyList<string> values, Random rng) =>
        values[rng.Next(values.Count)];
}
