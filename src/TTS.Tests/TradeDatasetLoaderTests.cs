using TTS.Core.Systems;
using Xunit;

namespace TTS.Tests;

public class TradeDatasetLoaderTests
{
    [Fact]
    public void Load_ParsesTenThousandShipments()
    {
        var path = TradeDatasetLoader.ResolveDefaultPath();
        Assert.True(File.Exists(path), $"Missing trade CSV at {path}");

        var dataset = TradeDatasetLoader.Load(path);
        Assert.Equal(10000, dataset.Shipments.Count);
        Assert.Single(dataset.ByExportCountry);
        Assert.Equal("China", dataset.ByExportCountry[0].CountryId);
        Assert.Equal(7, dataset.ImportCountries.Count);
        Assert.Equal(5, dataset.Commodities.Count);
    }

    [Fact]
    public void Load_CountryTotalsMatchShipmentSum()
    {
        var dataset = TradeDatasetLoader.Load();
        var importSum = dataset.ByImportCountry.Sum(c => c.TotalValueUsd);
        var shipmentSum = dataset.Shipments.Sum(s => s.DeclaredValueUsd);
        Assert.Equal(shipmentSum, importSum, 0.01);
    }

    [Fact]
    public void Filter_ByCommodity_ReducesRows()
    {
        var dataset = TradeDatasetLoader.Load();
        var filtered = TradeDatasetLoader.Filter(dataset, "Electronics", null, null);
        Assert.True(filtered.Shipments.Count < dataset.Shipments.Count);
        Assert.All(filtered.Shipments, s => Assert.Equal("Electronics", s.Commodity, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Filter_ByTransportMode_ReducesRows()
    {
        var dataset = TradeDatasetLoader.Load();
        var filtered = TradeDatasetLoader.Filter(dataset, null, null, "Air");
        Assert.True(filtered.Shipments.Count < dataset.Shipments.Count);
        Assert.All(filtered.Shipments, s => Assert.Equal("Air", s.TransportMode, StringComparer.OrdinalIgnoreCase));
    }
}
