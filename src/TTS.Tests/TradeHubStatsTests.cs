using TTS.Core.Systems;
using Xunit;

namespace TTS.Tests;

public class TradeHubStatsTests
{
    [Fact]
    public void Build_HubStatsIncludeOutboundAndInbound()
    {
        var dataset = TradeDatasetLoader.Load();
        var globe = TradeGlobeBuilder.Build(dataset);
        var shanghai = globe.Hubs.First(h => h.Id == "shanghai");
        var mombasa = globe.Hubs.First(h => h.Id == "mombasa");

        Assert.True(globe.HubStatsById.TryGetValue(shanghai.Id, out var exportHub));
        Assert.True(exportHub.OutboundShipments > 0);
        Assert.True(exportHub.OutboundValueUsd > 0);
        Assert.NotEmpty(exportHub.TopCommodities);
        Assert.NotEmpty(exportHub.TopRoutes);

        Assert.True(globe.HubStatsById.TryGetValue(mombasa.Id, out var importHub));
        Assert.True(importHub.InboundShipments > 0);
        Assert.True(importHub.InboundValueUsd > 0);
    }
}
