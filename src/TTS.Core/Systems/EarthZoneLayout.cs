namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>Stylized continent zones on normalized equirectangular lat/lon (nx, ny in 0..1).</summary>
public static class EarthZoneLayout
{
    public readonly record struct Zone(string RegionId, double Cx, double Cy, double Rx, double Ry, double Weight);

    public const double LandThreshold = 0.14;

    /// <summary>Macro continents for match gameplay (6 playable landmasses).</summary>
    public static readonly Zone[] ContinentZones =
    [
        new(WorldMacroRegion.NorthAmerica, 0.16, 0.28, 0.12, 0.20, 1.05),
        new(WorldMacroRegion.SouthAmerica, 0.24, 0.70, 0.08, 0.18, 1.0),
        new(WorldMacroRegion.Europe, 0.48, 0.24, 0.07, 0.10, 1.0),
        new(WorldMacroRegion.Africa, 0.52, 0.52, 0.09, 0.22, 1.05),
        new(WorldMacroRegion.Asia, 0.72, 0.32, 0.16, 0.24, 1.0),
        new(WorldMacroRegion.Australia, 0.84, 0.74, 0.07, 0.10, 1.0)
    ];

    public static (double Nx, double Ny) LatLonToNormalized(double latDeg, double lonDeg)
    {
        var lat = latDeg * Math.PI / 180.0;
        var lon = lonDeg * Math.PI / 180.0;
        var nx = (lon + Math.PI) / (2 * Math.PI);
        var ny = (Math.PI / 2 - lat) / Math.PI;
        return (nx, ny);
    }

    public static (double Nx, double Ny) NormalToNormalized(SphereVec3 normal)
    {
        var lat = Math.Asin(Math.Clamp(normal.Y, -1, 1));
        var lon = Math.Atan2(normal.X, normal.Z);
        var nx = (lon + Math.PI) / (2 * Math.PI);
        var ny = (Math.PI / 2 - lat) / Math.PI;
        return (nx, ny);
    }

    public static SphereVec3 LatLonToUnitSphere(double latDeg, double lonDeg)
    {
        var lat = latDeg * Math.PI / 180.0;
        var lon = lonDeg * Math.PI / 180.0;
        var cosLat = Math.Cos(lat);
        return new SphereVec3(cosLat * Math.Sin(lon), Math.Sin(lat), cosLat * Math.Cos(lon)).Normalized();
    }

    public static string ClassifyRegion(double nx, double ny, IReadOnlyList<Zone> zones, double threshold = LandThreshold)
    {
        var bestId = WorldMacroRegion.Ocean;
        var bestScore = 0.0;

        foreach (var zone in zones)
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

        return bestScore >= threshold ? bestId : WorldMacroRegion.Ocean;
    }
}
