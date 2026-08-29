using TTS.Core.Systems;
using Xunit;

namespace TTS.Tests;

public class SatelliteGlobeBuilderTests
{
    [Fact]
    public void Loader_ReadsSatellitesCsv()
    {
        var dataset = SatelliteDatasetLoader.Load();
        Assert.True(dataset.Satellites.Count > 1000);
        Assert.Contains(dataset.PurposeGroups, g => g == "communications");
        Assert.Contains(dataset.OrbitClasses, o => o.Equals("LEO", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_UsesDatasetAndCapsVisibleSatellites()
    {
        var catalog = SatelliteDatasetLoader.Load();
        var model = SatelliteGlobeBuilder.Build(catalog, null, catalog);

        Assert.NotEmpty(model.Map.Tiles);
        Assert.NotEmpty(model.Constellations);
        Assert.True(model.TotalSatelliteCount > 1000);
        Assert.True(model.VisibleSatelliteCount <= 100);
        Assert.NotEmpty(model.GroundStations);
        Assert.True(model.MaxCoverage > 0);
    }

    [Fact]
    public void Build_FiltersByPurposeGroup()
    {
        var catalog = SatelliteDatasetLoader.Load();
        var filter = new SatelliteGlobeFilter(PurposeGroupId: "earth-observation");
        var dataset = SatelliteDatasetLoader.Filter(catalog, filter.PurposeGroupId, null, null);
        var model = SatelliteGlobeBuilder.Build(dataset, filter, catalog);

        Assert.All(model.Satellites, s => Assert.Equal("earth-observation", s.ConstellationId));
        Assert.True(model.TotalSatelliteCount > 100);
    }
}
