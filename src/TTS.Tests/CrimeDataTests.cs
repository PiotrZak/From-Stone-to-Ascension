using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

namespace TTS.Tests;

public class CrimeDataTests
{
    [Fact]
    public void CrimeDataRepository_LoadsCalifornia2015()
    {
        var path = CrimeDataRepository.ResolveDefaultPath();
        var repo = new CrimeDataRepository(path);
        if (!repo.IsLoaded)
            return;

        var profile = repo.ToProfile("California", 2015);

        Assert.NotNull(profile);
        Assert.Equal("California", profile!.SourceState);
        Assert.Equal(2015, profile.DataYear);
        Assert.True(profile.ViolentCrimeRate > 0);
        Assert.True(profile.GdpPerCapita > 0);
    }

    [Fact]
    public void SampleWorld_AttachesCrimeProfilesToRegions()
    {
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, useStandardArena: true);
        var repo = CrimeDataRepository.Default;
        if (!repo.IsLoaded)
            return;

        var greenBasin = world.Regions.First(r => r.Id == "reg-a");
        var ironCoast = world.Regions.First(r => r.Id == "reg-b");

        Assert.Equal("Meridian Bay", greenBasin.Name);
        Assert.Equal("Redstone Harbor", ironCoast.Name);
        Assert.NotNull(greenBasin.CrimeProfile);
        Assert.NotNull(ironCoast.CrimeProfile);
        Assert.Equal("California", greenBasin.CrimeProfile!.SourceState);
        Assert.Equal("Louisiana", ironCoast.CrimeProfile!.SourceState);
        Assert.True(greenBasin.Population > 1_000_000);
    }

    [Fact]
    public void CrimeSystem_AppliesPressureAtTts4()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, useStandardArena: true);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        player.CurrentTier = TechTier.InformationAge;
        var stabilityBefore = player.PoliticalStability;

        services.Crime.ApplyTurnPressure(player, world);

        if (world.Regions.Any(r => r.CrimeProfile is not null))
            Assert.True(player.PoliticalStability < stabilityBefore);
    }

    [Fact]
    public void CrimePerspective_UnavailableBelowTts4()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, useStandardArena: true);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        player.CurrentTier = TechTier.EarlyElectronics;

        var summary = services.Crime.GetPerspective(player, world);

        Assert.False(summary.Available);
    }

    [Fact]
    public void GameToolSurface_ExposesCrimePerspective()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, useStandardArena: true);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        player.CurrentTier = TechTier.InformationAge;
        var tools = services.CreateToolSurface(world);

        var summary = tools.GetCrimePerspective(player.Id);

        if (world.Regions.Any(r => r.CrimeProfile is not null))
            Assert.True(summary.Available);
    }

    [Fact]
    public void GameToolSurface_PolicyResearchAnalysis_MapsCategoryToBranch()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.ClassicStone);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var tools = services.CreateToolSurface(world);

        var analysis = tools.GetPolicyResearchAnalysis(player.Id);
        var agriculture = analysis.RankedCandidates.First(c => c.TechnologyId == "tech-agriculture");

        Assert.Equal(TechCategory.Agriculture, agriculture.Category);
        Assert.Equal("agriculture", agriculture.Branch);
    }

    [Fact]
    public void GameLoop_RecordsResearchDecisions()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create(MatchPresets.Sprint8h, useStandardArena: true);
        var result = services.CreateGameLoop(world).RunTurn();

        Assert.NotEmpty(result.ResearchDecisions);
        Assert.Contains(result.ResearchDecisions, d => d.Researched && d.TechnologyId is not null);
    }
}
