namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Mock resource indices (0–100) inspired by public macro indicators:
/// FAOSTAT agriculture &amp; fisheries, World Bank GDP/industry, EIA energy, USGS minerals,
/// and stylized maritime trade/offshore output for ocean tiles.
/// </summary>
public static class WorldRegionResourceProfiles
{
    public readonly record struct MacroResourceProfile(
        double Agriculture,
        double Minerals,
        double Energy,
        double Industry,
        double Technology);

    private static readonly Dictionary<string, MacroResourceProfile> Profiles = new(StringComparer.Ordinal)
    {
        [WorldMacroRegion.NorthAmerica] = new(82, 58, 68, 78, 90),
        [WorldMacroRegion.SouthAmerica] = new(76, 72, 55, 48, 42),
        [WorldMacroRegion.Europe] = new(62, 45, 40, 74, 80),
        [WorldMacroRegion.Britain] = new(55, 38, 42, 80, 86),
        [WorldMacroRegion.Africa] = new(58, 78, 52, 38, 35),
        [WorldMacroRegion.MiddleEast] = new(35, 55, 92, 58, 48),
        [WorldMacroRegion.Asia] = new(70, 60, 58, 82, 85),
        [WorldMacroRegion.Australia] = new(64, 88, 70, 62, 58),
        // Fisheries, shipping lanes, offshore energy (stylized maritime macro profile).
        [WorldMacroRegion.Ocean] = new(48, 18, 62, 58, 42)
    };

    public static MacroResourceProfile Get(string? regionId) =>
        regionId is not null && Profiles.TryGetValue(regionId, out var profile)
            ? profile
            : Profiles[WorldMacroRegion.Ocean];

    public static double ComputeYield(
        string? regionId,
        Biome biome,
        double elevation,
        int seed,
        int q,
        int r,
        int oceanNeighborCount = 0)
    {
        if (biome == Biome.Ocean || regionId is WorldMacroRegion.Ocean)
            return ComputeOceanYield(seed, q, r);

        if (regionId is null)
            return 0;

        var land = Get(regionId);
        var sea = Profiles[WorldMacroRegion.Ocean];
        var (ag, min, en, ind, tech) = (land.Agriculture, land.Minerals, land.Energy, land.Industry, land.Technology);

        var composite = biome switch
        {
            Biome.Plains => ag * 0.55 + ind * 0.25 + tech * 0.20,
            Biome.Forest => ag * 0.45 + min * 0.20 + ind * 0.15 + tech * 0.20,
            Biome.Coast => ag * 0.15 + ind * 0.26 + tech * 0.20
                + sea.Agriculture * 0.16 + sea.Industry * 0.14 + sea.Energy * 0.09,
            Biome.Hills => min * 0.45 + ind * 0.30 + ag * 0.25,
            Biome.Mountains => min * 0.55 + en * 0.30 + ind * 0.15,
            Biome.Desert => en * 0.55 + min * 0.30 + ind * 0.15,
            Biome.Tundra => min * 0.35 + en * 0.35 + ag * 0.15 + ind * 0.15,
            Biome.Wetlands => ag * 0.40 + min * 0.25 + ind * 0.20 + tech * 0.15,
            _ => (ag + min + en + ind + tech) / 5.0
        };

        composite += MaritimeAccessBonus(sea, oceanNeighborCount);

        var jitter = (ValueNoise.Sample(seed + 911, q * 0.41, r * 0.41) - 0.5) * 8;
        return Math.Clamp(composite * (0.75 + 0.25 * elevation) + jitter, 8, 98);
    }

    private static double ComputeOceanYield(int seed, int q, int r)
    {
        var sea = Profiles[WorldMacroRegion.Ocean];
        var composite = sea.Agriculture * 0.35 + sea.Industry * 0.30 + sea.Energy * 0.25 + sea.Technology * 0.10;
        var jitter = (ValueNoise.Sample(seed + 431, q * 0.53, r * 0.53) - 0.5) * 6;
        return Math.Clamp(composite * 0.42 + jitter, 6, 32);
    }

    private static double MaritimeAccessBonus(MacroResourceProfile sea, int oceanNeighborCount)
    {
        if (oceanNeighborCount <= 0)
            return 0;

        var exposure = Math.Min(oceanNeighborCount, 3) / 3.0;
        var maritime = sea.Agriculture * 0.35 + sea.Industry * 0.35 + sea.Energy * 0.20 + sea.Technology * 0.10;
        return maritime * 0.24 * exposure;
    }
}
