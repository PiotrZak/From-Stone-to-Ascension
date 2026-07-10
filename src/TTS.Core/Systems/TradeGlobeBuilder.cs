namespace TTS.Core.Systems;

using TTS.Core.Models;

public sealed record TradeGlobeHub(
    string Id,
    string Name,
    string CountryId,
    double LatDeg,
    double LonDeg,
    string TileId,
    double CenterX,
    double CenterY,
    double CenterZ,
    bool IsPort);

public sealed record TradeGlobeFlow(
    string FromHubId,
    string ToHubId,
    string DeparturePort,
    string ArrivalPort,
    string ImportCountry,
    int ShipmentCount,
    double TotalValueUsd);

public sealed record TradeGlobeModel(
    HexMap Map,
    IReadOnlyDictionary<string, TradeCountryAggregate> CountryStatsById,
    IReadOnlyList<TradeGlobeHub> Hubs,
    IReadOnlyList<TradeGlobeFlow> Flows,
    IReadOnlyDictionary<string, string> TradeCountryByTileId,
    double MaxCountryValueUsd,
    IReadOnlyList<string> Commodities,
    IReadOnlyList<string> ImportCountries,
    IReadOnlyList<string> TransportModes,
    IReadOnlyList<string> ActiveCountryIds,
    IReadOnlyList<string> ActiveHubIds,
    IReadOnlyDictionary<string, TradeHubStats> HubStatsById,
    TradeGlobeFilter AppliedFilter,
    int FilteredShipmentCount);

public static class TradeGlobeBuilder
{
    private const int TradeFrequency = 4;
    private const double PlanetRadius = 100;
    private const int TradeSeed = 42_001;

    private const int MaxVisibleFlows = 60;

    private static readonly HashSet<string> CorridorHubIds =
    [
        "dubai", "jeddah", "mumbai", "colombo"
    ];

    public static TradeGlobeModel Build(TradeDataset dataset, TradeGlobeFilter? filter = null, TradeDataset? catalog = null)
    {
        filter ??= new TradeGlobeFilter(null, null, null);
        catalog ??= dataset;
        var meshes = GoldbergPolyhedronGenerator.Generate(TradeFrequency, PlanetRadius);
        var map = new HexMap
        {
            PlanetRadius = PlanetRadius,
            Frequency = TradeFrequency,
            Seed = TradeSeed
        };

        var tradeCountryByTile = new Dictionary<string, string>(StringComparer.Ordinal);
        var countryStats = dataset.ByImportCountry
            .Select(c => new TradeCountryAggregate(
                TradeCountryZones.SlugFromDatasetName(c.CountryId),
                c.ShipmentCount,
                c.TotalValueUsd,
                c.TopCommodity))
            .Concat(dataset.ByExportCountry.Select(c => new TradeCountryAggregate(
                TradeCountryZones.China,
                c.ShipmentCount,
                c.TotalValueUsd,
                c.TopCommodity)))
            .GroupBy(c => c.CountryId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.TotalValueUsd).First());

        foreach (var mesh in meshes)
        {
            var (nx, ny) = EarthZoneLayout.NormalToNormalized(mesh.Normal);
            var macroRegion = EarthZoneLayout.ClassifyRegion(nx, ny, EarthZoneLayout.ContinentZones);
            var tradeCountry = TradeCountryZones.ClassifyCountry(nx, ny);
            var isMapCountry = TradeCountryZones.IsMapCountry(tradeCountry);
            var isLand = macroRegion != WorldMacroRegion.Ocean;

            var elevationNoise = ValueNoise.Fbm(TradeSeed, nx * 3.0, ny * 3.0);
            var moistureNoise = ValueNoise.Fbm(TradeSeed + 17_371, nx * 3.0 + 3.1, ny * 3.0 + 1.7);
            var elevation = isLand ? 0.48 + elevationNoise * 0.32 : 0.12 + elevationNoise * 0.08;
            var biome = isLand
                ? ClassifyBiome(macroRegion, elevation, moistureNoise)
                : Biome.Ocean;

            var tile = new HexTile(mesh.Id)
            {
                Biome = biome,
                Elevation = elevation,
                WorldRegionId = macroRegion,
                RegionId = isMapCountry ? tradeCountry : null,
                CenterX = mesh.Center.X,
                CenterY = mesh.Center.Y,
                CenterZ = mesh.Center.Z,
                NormalX = mesh.Normal.X,
                NormalY = mesh.Normal.Y,
                NormalZ = mesh.Normal.Z,
                PolygonVertices = mesh.PolygonVertices.Select(v => (v.X, v.Y, v.Z)).ToList(),
                NeighbourIds = mesh.NeighbourIds.ToList(),
                ResourceYield = isMapCountry && countryStats.TryGetValue(tradeCountry, out var stats)
                    ? stats.TotalValueUsd / Math.Max(1, stats.ShipmentCount)
                    : 0
            };

            if (isMapCountry)
                tradeCountryByTile[tile.Id] = tradeCountry;

            map.Tiles.Add(tile);
        }

        map.RebuildIndex();
        EnsureMapCountryStats(countryStats);
        var hubs = BuildHubs(map);
        var flows = BuildFlows(dataset, hubs).Take(MaxVisibleFlows).ToList();
        var hubStats = TradeHubStatsBuilder.Build(dataset, hubs);
        var activeHubIds = ResolveActiveHubIds(flows);
        var activeCountryIds = ResolveActiveCountryIds(flows, hubs, filter);
        var maxValue = countryStats.Values
            .Where(c => activeCountryIds.Contains(c.CountryId) && c.TotalValueUsd > 0)
            .Select(c => c.TotalValueUsd)
            .DefaultIfEmpty(0)
            .Max();

        return new TradeGlobeModel(
            map,
            countryStats,
            hubs,
            flows,
            tradeCountryByTile,
            maxValue,
            catalog.Commodities,
            catalog.ImportCountries,
            catalog.TransportModes,
            activeCountryIds,
            activeHubIds,
            hubStats,
            filter,
            dataset.Shipments.Count);
    }

    private static void EnsureMapCountryStats(Dictionary<string, TradeCountryAggregate> countryStats)
    {
        foreach (var countryId in TradeCountryZones.MapCountries)
        {
            if (countryStats.ContainsKey(countryId))
                continue;

            countryStats[countryId] = new TradeCountryAggregate(
                countryId,
                ShipmentCount: 0,
                TotalValueUsd: 0,
                TopCommodity: TradeCountryZones.CorridorCountries.Contains(countryId)
                    ? "Transit corridor"
                    : "—");
        }
    }

    private static List<string> ResolveActiveHubIds(IReadOnlyList<TradeGlobeFlow> flows)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var flow in flows)
        {
            ids.Add(flow.FromHubId);
            ids.Add(flow.ToHubId);
        }

        return ids.OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    private static List<string> ResolveActiveCountryIds(
        IReadOnlyList<TradeGlobeFlow> flows,
        IReadOnlyList<TradeGlobeHub> hubs,
        TradeGlobeFilter filter)
    {
        if (!filter.HasAny)
            return TradeCountryZones.MapCountries.ToList();

        var hubById = hubs.ToDictionary(h => h.Id, StringComparer.Ordinal);
        var active = new HashSet<string>(StringComparer.Ordinal);

        foreach (var flow in flows)
        {
            active.Add(flow.ImportCountry);
            if (hubById.TryGetValue(flow.FromHubId, out var from))
                active.Add(from.CountryId);
        }

        foreach (var hubId in CorridorHubIds)
        {
            if (!hubById.TryGetValue(hubId, out var hub))
                continue;

            var used = flows.Any(f => f.FromHubId == hubId || f.ToHubId == hubId);
            if (used)
                active.Add(hub.CountryId);
        }

        active.IntersectWith(TradeCountryZones.MapCountries);

        return active.OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    private static List<TradeGlobeHub> BuildHubs(HexMap map)
    {
        var hubs = new List<TradeGlobeHub>();
        foreach (var def in TradeGeoCatalog.Hubs)
        {
            var unit = EarthZoneLayout.LatLonToUnitSphere(def.LatDeg, def.LonDeg);
            var tile = FindNearestTile(map, unit);
            hubs.Add(new TradeGlobeHub(
                def.Id,
                def.Name,
                def.CountryId,
                def.LatDeg,
                def.LonDeg,
                tile.Id,
                tile.CenterX,
                tile.CenterY,
                tile.CenterZ,
                def.IsPort));
        }

        return hubs;
    }

    private static List<TradeGlobeFlow> BuildFlows(TradeDataset dataset, IReadOnlyList<TradeGlobeHub> hubs)
    {
        var hubByName = hubs.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);
        return dataset.Routes
            .Select(route =>
            {
                hubByName.TryGetValue(route.DeparturePort, out var from);
                hubByName.TryGetValue(route.ArrivalPort, out var to);
                return new TradeGlobeFlow(
                    from?.Id ?? TradeGeoCatalog.HubIdForPort(route.DeparturePort),
                    to?.Id ?? TradeGeoCatalog.HubIdForPort(route.ArrivalPort),
                    route.DeparturePort,
                    route.ArrivalPort,
                    TradeCountryZones.SlugFromDatasetName(route.ImportCountry),
                    route.ShipmentCount,
                    route.TotalValueUsd);
            })
            .OrderByDescending(f => f.TotalValueUsd)
            .ToList();
    }

    private static HexTile FindNearestTile(HexMap map, SphereVec3 unit)
    {
        HexTile? best = null;
        var bestDist = double.MaxValue;
        foreach (var tile in map.Tiles)
        {
            var dx = tile.CenterX - unit.X * map.PlanetRadius;
            var dy = tile.CenterY - unit.Y * map.PlanetRadius;
            var dz = tile.CenterZ - unit.Z * map.PlanetRadius;
            var dist = dx * dx + dy * dy + dz * dz;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = tile;
            }
        }

        return best ?? map.Tiles[0];
    }

    private static Biome ClassifyBiome(string regionId, double elevation, double moisture)
    {
        if (elevation > 0.78)
            return Biome.Mountains;

        return regionId switch
        {
            WorldMacroRegion.NorthAmerica => moisture < 0.38 ? Biome.Plains : Biome.Forest,
            WorldMacroRegion.SouthAmerica => moisture > 0.42 ? Biome.Forest : Biome.Plains,
            WorldMacroRegion.Europe => moisture > 0.48 ? Biome.Forest : Biome.Plains,
            WorldMacroRegion.Africa => moisture < 0.34 ? Biome.Desert
                : moisture > 0.55 ? Biome.Wetlands : Biome.Plains,
            WorldMacroRegion.Asia => moisture < 0.28 ? Biome.Desert
                : moisture > 0.5 ? Biome.Forest : Biome.Plains,
            WorldMacroRegion.Australia => moisture < 0.42 ? Biome.Desert : Biome.Plains,
            _ => Biome.Plains
        };
    }
}
