namespace TTS.Core.Systems;

using TTS.Core.Models;

public sealed record SatelliteGlobeFilter(
    string? PurposeGroupId = null,
    string? OrbitClass = null,
    string? Country = null)
{
    public bool HasAny =>
        !string.IsNullOrWhiteSpace(PurposeGroupId)
        || !string.IsNullOrWhiteSpace(OrbitClass)
        || !string.IsNullOrWhiteSpace(Country);
}

public sealed record SatelliteBody(
    string Id,
    string Name,
    string ConstellationId,
    double AltitudeFactor,
    double InclinationRad,
    double PhaseRad,
    double AngularSpeed,
    string Role,
    string Operator,
    string Country,
    string Users,
    string Purpose,
    string OrbitClass,
    string OrbitType,
    double PerigeeKm,
    double ApogeeKm,
    double InclinationDeg,
    double PeriodMinutes,
    string LaunchDate,
    string LaunchSite,
    string NoradNumber);

public sealed record SatelliteGroundStation(
    string Id,
    string Name,
    string Region,
    string ConstellationId,
    string TileId,
    double LatDeg,
    double LonDeg,
    double CenterX,
    double CenterY,
    double CenterZ,
    int LaunchCount);

public sealed record SatelliteConstellationInfo(
    string Id,
    string Name,
    string Domain,
    string Color,
    string Summary,
    int SatelliteCount,
    double AverageCoverage,
    int GroundStationCount);

public sealed record SatelliteGlobeModel(
    HexMap Map,
    IReadOnlyList<SatelliteConstellationInfo> Constellations,
    IReadOnlyList<SatelliteConstellationInfo> AvailableConstellations,
    IReadOnlyList<SatelliteBody> Satellites,
    IReadOnlyList<SatelliteGroundStation> GroundStations,
    IReadOnlyDictionary<string, double> CoverageByTileId,
    double MaxCoverage,
    IReadOnlyList<string> OrbitClasses,
    IReadOnlyList<string> Countries,
    int TotalSatelliteCount,
    int VisibleSatelliteCount,
    SatelliteGlobeFilter AppliedFilter);

public static class SatelliteGlobeBuilder
{
    private const int Frequency = 4;
    private const double PlanetRadius = 100;
    private const int Seed = 77_401;
    private const int MaxVisibleSatellites = 100;

    public static SatelliteGlobeModel Build(
        SatelliteDataset? dataset = null,
        SatelliteGlobeFilter? filter = null,
        SatelliteDataset? catalog = null)
    {
        filter ??= new SatelliteGlobeFilter();
        catalog ??= dataset ?? SatelliteDatasetLoader.Load();
        dataset ??= SatelliteDatasetLoader.Filter(
            catalog,
            filter.PurposeGroupId,
            filter.OrbitClass,
            filter.Country);

        var meshes = GoldbergPolyhedronGenerator.Generate(Frequency, PlanetRadius);
        var map = new HexMap
        {
            PlanetRadius = PlanetRadius,
            Frequency = Frequency,
            Seed = Seed
        };

        foreach (var mesh in meshes)
        {
            var (nx, ny) = EarthZoneLayout.NormalToNormalized(mesh.Normal);
            var macroRegion = EarthZoneLayout.ClassifyRegion(nx, ny, EarthZoneLayout.ContinentZones);
            var isLand = macroRegion != WorldMacroRegion.Ocean;
            var elevationNoise = ValueNoise.Fbm(Seed, nx * 3.0, ny * 3.0);
            var moistureNoise = ValueNoise.Fbm(Seed + 19_001, nx * 3.0 + 2.4, ny * 3.0 + 1.1);
            var elevation = isLand ? 0.48 + elevationNoise * 0.32 : 0.12 + elevationNoise * 0.08;
            var biome = isLand ? ClassifyBiome(macroRegion, elevation, moistureNoise) : Biome.Ocean;

            map.Tiles.Add(new HexTile(mesh.Id)
            {
                Biome = biome,
                Elevation = elevation,
                WorldRegionId = macroRegion,
                CenterX = mesh.Center.X,
                CenterY = mesh.Center.Y,
                CenterZ = mesh.Center.Z,
                NormalX = mesh.Normal.X,
                NormalY = mesh.Normal.Y,
                NormalZ = mesh.Normal.Z,
                PolygonVertices = mesh.PolygonVertices.Select(v => (v.X, v.Y, v.Z)).ToList(),
                NeighbourIds = mesh.NeighbourIds.ToList()
            });
        }

        map.RebuildIndex();

        var visibleEntries = SelectVisible(dataset.Satellites);
        var satellites = visibleEntries.Select(ToBody).ToList();
        var stations = BuildGroundStations(map, dataset.Satellites);
        var coverage = ComputeCoverage(map, dataset.Satellites, stations);
        var maxCoverage = coverage.Values.DefaultIfEmpty(0).Max();

        var activeGroups = dataset.Satellites
            .GroupBy(s => s.PurposeGroupId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var constellationInfos = activeGroups
            .OrderByDescending(kv => kv.Value)
            .Select(kv => ToConstellationInfo(kv.Key, kv.Value, stations, coverage))
            .ToList();

        var available = catalog.PurposeGroups
            .Select(id =>
            {
                var count = catalog.Satellites.Count(s => s.PurposeGroupId.Equals(id, StringComparison.OrdinalIgnoreCase));
                return ToConstellationInfo(id, count, stations, coverage);
            })
            .OrderByDescending(c => c.SatelliteCount)
            .ToList();

        return new SatelliteGlobeModel(
            map,
            constellationInfos,
            available,
            satellites,
            stations,
            coverage,
            maxCoverage,
            catalog.OrbitClasses,
            catalog.Countries,
            dataset.Satellites.Count,
            satellites.Count,
            filter);
    }

    private static SatelliteConstellationInfo ToConstellationInfo(
        string groupId,
        int count,
        IReadOnlyList<SatelliteGroundStation> stations,
        IReadOnlyDictionary<string, double> coverage)
    {
        var avg = coverage.Count == 0 ? 0 : coverage.Values.Average();
        return new SatelliteConstellationInfo(
            groupId,
            SatelliteDatasetLoader.PurposeGroupDisplayName(groupId),
            SatelliteDatasetLoader.PurposeGroupDisplayName(groupId),
            SatelliteDatasetLoader.PurposeGroupColor(groupId),
            $"{count:n0} catalog satellites in this purpose group.",
            count,
            Math.Clamp(avg, 0, 100),
            stations.Count(s => s.ConstellationId.Equals(groupId, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(s.ConstellationId)));
    }

    private static List<SatelliteCatalogEntry> SelectVisible(IReadOnlyList<SatelliteCatalogEntry> entries)
    {
        if (entries.Count <= MaxVisibleSatellites)
            return entries.ToList();

        // Prefer a balanced sample across purpose groups and orbit classes.
        var selected = new List<SatelliteCatalogEntry>();
        foreach (var group in entries.GroupBy(e => e.PurposeGroupId))
        {
            var take = Math.Max(8, MaxVisibleSatellites * group.Count() / Math.Max(1, entries.Count));
            selected.AddRange(group
                .OrderByDescending(e => OrbitPriority(e.OrbitClass))
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Take(take));
        }

        return selected
            .DistinctBy(e => e.Id)
            .Take(MaxVisibleSatellites)
            .ToList();
    }

    private static int OrbitPriority(string orbitClass) => orbitClass.ToUpperInvariant() switch
    {
        "GEO" => 3,
        "MEO" => 2,
        "LEO" => 1,
        _ => 0
    };

    private static SatelliteBody ToBody(SatelliteCatalogEntry e)
    {
        var (altitudeFactor, angularSpeed, phase) = OrbitVisuals(e);
        return new SatelliteBody(
            e.Id,
            e.Name,
            e.PurposeGroupId,
            altitudeFactor,
            e.InclinationDeg * Math.PI / 180.0,
            phase,
            angularSpeed,
            string.IsNullOrWhiteSpace(e.DetailedPurpose) ? e.Purpose : e.DetailedPurpose,
            e.Operator,
            e.Country,
            e.Users,
            e.Purpose,
            e.OrbitClass,
            e.OrbitType,
            e.PerigeeKm,
            e.ApogeeKm,
            e.InclinationDeg,
            e.PeriodMinutes,
            e.LaunchDate,
            e.LaunchSite,
            e.NoradNumber);
    }

    private static (double AltitudeFactor, double AngularSpeed, double Phase) OrbitVisuals(SatelliteCatalogEntry e)
    {
        var meanAlt = (e.PerigeeKm + e.ApogeeKm) * 0.5;
        if (meanAlt <= 0)
            meanAlt = e.OrbitClass.ToUpperInvariant() switch
            {
                "GEO" => 35786,
                "MEO" => 20200,
                "ELLIPTICAL" => 15000,
                _ => 700
            };

        var orbitClass = e.OrbitClass.ToUpperInvariant();
        var altitudeFactor = orbitClass switch
        {
            "GEO" => 1.68,
            "MEO" => 1.48 + Math.Clamp(meanAlt / 40000.0, 0, 0.12),
            "ELLIPTICAL" => 1.52,
            _ => 1.18 + Math.Clamp(meanAlt / 8000.0, 0, 0.2)
        };

        var period = e.PeriodMinutes > 1 ? e.PeriodMinutes : orbitClass switch
        {
            "GEO" => 1436,
            "MEO" => 720,
            _ => 95
        };

        // Visual angular speed — GEO almost parked, LEO moves faster.
        var angularSpeed = Math.Clamp(18.0 / period, 0.008, 0.35);
        if (orbitClass == "GEO")
            angularSpeed *= 0.15;

        var phase = e.GeoLongitudeDeg is { } lon
            ? lon * Math.PI / 180.0
            : StablePhase(e.Id);

        return (altitudeFactor, angularSpeed, phase);
    }

    private static double StablePhase(string id)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in id)
                hash = hash * 31 + c;
            return (hash & 0xffff) / (double)0xffff * Math.PI * 2;
        }
    }

    private static List<SatelliteGroundStation> BuildGroundStations(
        HexMap map,
        IReadOnlyList<SatelliteCatalogEntry> entries)
    {
        var counts = entries
            .Select(e => (Entry: e, Site: SatelliteLaunchSites.Match(e.LaunchSite)))
            .Where(x => x.Site is not null)
            .GroupBy(x => x.Site!.Id)
            .Select(g =>
            {
                var site = g.First().Site!;
                var topGroup = g
                    .GroupBy(x => x.Entry.PurposeGroupId)
                    .OrderByDescending(x => x.Count())
                    .First()
                    .Key;
                return (Site: site, Count: g.Count(), GroupId: topGroup);
            })
            .OrderByDescending(x => x.Count)
            .Take(12)
            .ToList();

        var stations = new List<SatelliteGroundStation>();
        foreach (var item in counts)
        {
            var unit = EarthZoneLayout.LatLonToUnitSphere(item.Site.LatDeg, item.Site.LonDeg);
            var tile = NearestTile(map, unit.X * PlanetRadius, unit.Y * PlanetRadius, unit.Z * PlanetRadius);
            stations.Add(new SatelliteGroundStation(
                item.Site.Id,
                item.Site.Name,
                item.Site.Region,
                item.GroupId,
                tile.Id,
                item.Site.LatDeg,
                item.Site.LonDeg,
                tile.CenterX,
                tile.CenterY,
                tile.CenterZ,
                item.Count));
        }

        return stations;
    }

    private static Dictionary<string, double> ComputeCoverage(
        HexMap map,
        IReadOnlyList<SatelliteCatalogEntry> entries,
        IReadOnlyList<SatelliteGroundStation> stations)
    {
        var coverage = new Dictionary<string, double>(StringComparer.Ordinal);
        if (entries.Count == 0)
            return coverage;

        var leoShare = entries.Count(e => e.OrbitClass.Equals("LEO", StringComparison.OrdinalIgnoreCase)) / (double)entries.Count;
        var geoShare = entries.Count(e => e.OrbitClass.Equals("GEO", StringComparison.OrdinalIgnoreCase)) / (double)entries.Count;
        var eoShare = entries.Count(e => e.PurposeGroupId == "earth-observation") / (double)entries.Count;
        var baseCoverage = 28 + leoShare * 35 + geoShare * 20 + eoShare * 12;

        foreach (var tile in map.Tiles)
        {
            if (tile.Biome == Biome.Ocean)
            {
                coverage[tile.Id] = Math.Min(22, baseCoverage * 0.3);
                continue;
            }

            var score = baseCoverage * 0.55;
            var (_, ny) = EarthZoneLayout.NormalToNormalized(
                new SphereVec3(tile.NormalX, tile.NormalY, tile.NormalZ));
            var latBias = 1.0 - Math.Abs(ny - 0.5) * 1.1;
            score += Math.Max(0, latBias) * 14;

            foreach (var station in stations)
            {
                var dx = tile.CenterX - station.CenterX;
                var dy = tile.CenterY - station.CenterY;
                var dz = tile.CenterZ - station.CenterZ;
                var dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                var strength = Math.Min(1, station.LaunchCount / 80.0);
                if (dist < PlanetRadius * 0.55)
                    score += (22 + 18 * strength) * (1.0 - dist / (PlanetRadius * 0.55));
            }

            coverage[tile.Id] = Math.Clamp(score, 0, 100);
        }

        return coverage;
    }

    private static HexTile NearestTile(HexMap map, double x, double y, double z)
    {
        HexTile? best = null;
        var bestDist = double.MaxValue;
        foreach (var tile in map.Tiles)
        {
            var dx = tile.CenterX - x;
            var dy = tile.CenterY - y;
            var dz = tile.CenterZ - z;
            var d = dx * dx + dy * dy + dz * dz;
            if (d >= bestDist) continue;
            bestDist = d;
            best = tile;
        }

        return best ?? map.Tiles[0];
    }

    private static Biome ClassifyBiome(string macroRegion, double elevation, double moisture) =>
        macroRegion switch
        {
            WorldMacroRegion.Africa when moisture < 0.35 => Biome.Desert,
            WorldMacroRegion.Australia when moisture < 0.4 => Biome.Desert,
            _ when elevation > 0.72 => Biome.Mountains,
            _ when moisture > 0.62 => Biome.Forest,
            _ when moisture < 0.32 => Biome.Plains,
            _ => Biome.Coast
        };
}
