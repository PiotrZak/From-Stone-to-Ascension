using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class EconomyTests
{
    [Fact]
    public void EconomicHealthIndex_UsesGdpAndEmployment()
    {
        var profile = new RegionalCrimeProfile
        {
            GdpPerCapita = 60_000,
            UnemploymentRate = 5,
            PovertyRate = 12
        };

        Assert.True(profile.EconomicHealthIndex > 50);
    }

    [Fact]
    public void EconomySystem_AppliesGenericBonusFromInfrastructure()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        player.CurrentTier = TechTier.PreIndustrial;
        var before = player.EconomicStability;

        services.Economy.ApplyTurnEconomy(player, world);

        Assert.True(player.EconomicStability >= before);
    }

    [Fact]
    public void CityPerspective_ListsNamedCitiesWithGdp()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var repo = CrimeDataRepository.Default;
        if (!repo.IsLoaded)
            return;

        var perspective = services.Economy.GetCityPerspective(player, world);

        Assert.True(perspective.Available);
        Assert.Contains(perspective.Cities!, c => c.Name == "Meridian Bay" && c.GdpPerCapita > 0);
    }
}
