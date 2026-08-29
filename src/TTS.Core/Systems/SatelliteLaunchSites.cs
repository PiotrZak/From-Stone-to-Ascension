namespace TTS.Core.Systems;

/// <summary>Known launch-site anchors for the satellite globe ground layer.</summary>
public static class SatelliteLaunchSites
{
    public sealed record Site(string Id, string Name, string Region, double LatDeg, double LonDeg);

    public static readonly Site[] All =
    [
        new("cape-canaveral", "Cape Canaveral", "Americas", 28.39, -80.61),
        new("vandenberg", "Vandenberg AFB", "Americas", 34.74, -120.57),
        new("guiana", "Guiana Space Center", "South America", 5.23, -52.77),
        new("baikonur", "Baikonur Cosmodrome", "Asia", 45.97, 63.31),
        new("plesetsk", "Plesetsk Cosmodrome", "Europe", 62.93, 40.58),
        new("xichang", "Xichang Satellite Launch Center", "Asia", 28.25, 102.03),
        new("satish-dhawan", "Satish Dhawan Space Center", "Asia", 13.72, 80.23),
        new("vostochny", "Vostochny Cosmodrome", "Asia", 51.88, 128.33),
        new("dombarovsky", "Dombarovsky Air Base", "Asia", 50.8, 59.52),
        new("taiuan", "Taiyuan Satellite Launch Center", "Asia", 38.85, 111.61),
        new("jiuquan", "Jiuquan Satellite Launch Center", "Asia", 40.96, 100.28),
        new("kennedy", "Kennedy Space Center", "Americas", 28.57, -80.65),
    ];

    public static Site? Match(string launchSiteName)
    {
        if (string.IsNullOrWhiteSpace(launchSiteName)) return null;
        var n = launchSiteName.Trim().ToLowerInvariant();
        return All.FirstOrDefault(s =>
            n.Contains(s.Name.ToLowerInvariant(), StringComparison.Ordinal)
            || s.Name.ToLowerInvariant().Contains(n, StringComparison.Ordinal)
            || n.Contains(s.Id.Replace('-', ' '), StringComparison.Ordinal));
    }
}
