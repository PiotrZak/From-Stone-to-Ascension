using TTS.Core.Systems;
using Xunit;

namespace TTS.Tests;

public class TradeGlobeBuilderTests
{
    [Fact]
    public void Build_IncludesDatasetCountriesAndHubs()
    {
        var dataset = TradeDatasetLoader.Load();
        var globe = TradeGlobeBuilder.Build(dataset);

        Assert.True(globe.Map.Tiles.Count > 0);
        Assert.Equal(16, TradeGeoCatalog.Hubs.Length);
        Assert.All(globe.Hubs, h => Assert.NotNull(globe.Map.GetTile(h.TileId)));
        Assert.Contains(TradeCountryZones.China, globe.CountryStatsById.Keys);
        Assert.Equal(7, dataset.ImportCountries.Count);
        Assert.Equal(10, globe.ActiveCountryIds.Count);
    }

    [Fact]
    public void Build_IncludesIndianOceanCorridorRegions()
    {
        var dataset = TradeDatasetLoader.Load();
        var globe = TradeGlobeBuilder.Build(dataset);

        Assert.Contains(TradeCountryZones.India, globe.CountryStatsById.Keys);
        Assert.Contains(TradeCountryZones.MiddleEast, globe.CountryStatsById.Keys);
        Assert.Contains(globe.TradeCountryByTileId.Values, v => v == TradeCountryZones.India);
        Assert.Contains(globe.TradeCountryByTileId.Values, v => v == TradeCountryZones.MiddleEast);
        Assert.Contains(globe.Hubs, h => h.Id == "dubai");
        Assert.Contains(globe.Hubs, h => h.Id == "mumbai");

        var indiaTiles = globe.TradeCountryByTileId.Values.Count(v => v == TradeCountryZones.India);
        var middleEastTiles = globe.TradeCountryByTileId.Values.Count(v => v == TradeCountryZones.MiddleEast);
        Assert.True(indiaTiles >= 8, $"Expected visible India tiles, got {indiaTiles}");
        Assert.True(middleEastTiles >= 8, $"Expected visible Middle East tiles, got {middleEastTiles}");
        Assert.Equal(16, globe.Hubs.Count);
    }

    [Fact]
    public void Build_FilterByImportCountry_ActivatesOnlyMatchingTradeCorridor()
    {
        var catalog = TradeDatasetLoader.Load();
        var filtered = TradeDatasetLoader.Filter(catalog, null, "Kenya", null);
        var filter = new TradeGlobeFilter(null, "Kenya", null);
        var globe = TradeGlobeBuilder.Build(filtered, filter, catalog);

        Assert.True(filter.HasAny);
        Assert.Contains(TradeCountryZones.Kenya, globe.ActiveCountryIds);
        Assert.Contains(TradeCountryZones.China, globe.ActiveCountryIds);
        Assert.DoesNotContain(TradeCountryZones.Nigeria, globe.ActiveCountryIds);
        Assert.True(globe.Flows.All(f => f.ImportCountry == TradeCountryZones.Kenya));
        Assert.True(globe.Flows.Count <= 60);
    }

    [Fact]
    public void Build_FilterByTransportMode_ReducesActiveFlows()
    {
        var catalog = TradeDatasetLoader.Load();
        var filtered = TradeDatasetLoader.Filter(catalog, null, null, "Ship");
        var filter = new TradeGlobeFilter(null, null, "Ship");
        var globe = TradeGlobeBuilder.Build(filtered, filter, catalog);

        Assert.True(globe.FilteredShipmentCount < catalog.Shipments.Count);
        Assert.True(globe.ActiveHubIds.Count > 0);
        Assert.True(globe.Flows.Count > 0);
    }

    [Fact]
    public void Build_FlowTotalsMatchDataset()
    {
        var dataset = TradeDatasetLoader.Load();
        var globe = TradeGlobeBuilder.Build(dataset);
        var flowSum = globe.Flows.Sum(f => f.TotalValueUsd);
        var routeSum = dataset.Routes.OrderByDescending(r => r.TotalValueUsd).Take(60).Sum(r => r.TotalValueUsd);
        Assert.Equal(routeSum, flowSum, 0.01);
    }
}
