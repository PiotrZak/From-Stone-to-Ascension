namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>Regional economy — city resources, infrastructure, and CSV-backed prosperity (see economy.md).</summary>
public sealed class EconomySystem
{
    public void ApplyTurnEconomy(Civilization civilization, WorldState world)
    {
        var regions = world.Regions
            .Where(r => r.ControllingCivilizationId == civilization.Id)
            .ToList();

        if (regions.Count == 0)
            return;

        var genericBonus = regions.Average(r =>
            r.Resources * 0.008 + r.Infrastructure * 0.006);

        civilization.EconomicStability = Math.Clamp(
            civilization.EconomicStability + genericBonus, 0, 100);

        if (civilization.CurrentTier < TechTier.InformationAge)
            return;

        var profiled = regions.Where(r => r.CrimeProfile is not null).ToList();
        if (profiled.Count == 0)
            return;

        var avgHealth = profiled.Average(r => r.CrimeProfile!.EconomicHealthIndex);
        var avgCrime = profiled.Average(r => r.CrimeProfile!.CrimePressureIndex);
        var net = (avgHealth - avgCrime * 0.35) * 0.05;

        civilization.EconomicStability = Math.Clamp(
            civilization.EconomicStability + net, 0, 100);
    }

    public CityPerspectiveSummary GetCityPerspective(Civilization civilization, WorldState world)
    {
        var regions = world.Regions
            .Where(r => r.ControllingCivilizationId == civilization.Id)
            .ToList();

        if (regions.Count == 0)
            return CityPerspectiveSummary.Unavailable();

        var cities = regions.Select(r =>
        {
            var profile = r.CrimeProfile;
            return new CitySnapshot(
                r.Id,
                r.Name,
                r.Population,
                r.Infrastructure,
                r.Resources,
                profile?.SourceState,
                profile?.DataYear,
                profile?.GdpPerCapita ?? 0,
                profile?.UnemploymentRate ?? 0,
                profile?.PovertyRate ?? 0,
                profile?.EconomicHealthIndex ?? Math.Clamp(r.Infrastructure + r.Resources * 0.5, 0, 100),
                profile?.CrimePressureIndex ?? 0);
        }).ToList();

        return new CityPerspectiveSummary(
            Available: true,
            AverageEconomicHealth: cities.Average(c => c.EconomicHealth),
            AverageCrimePressure: cities.Any(c => c.CrimePressure > 0)
                ? cities.Where(c => c.CrimePressure > 0).Average(c => c.CrimePressure)
                : 0,
            Cities: cities);
    }
}

public readonly record struct CityPerspectiveSummary(
    bool Available,
    double AverageEconomicHealth = 0,
    double AverageCrimePressure = 0,
    IReadOnlyList<CitySnapshot>? Cities = null)
{
    public static CityPerspectiveSummary Unavailable() => new(false);
}

public readonly record struct CitySnapshot(
    string RegionId,
    string Name,
    long Population,
    double Infrastructure,
    double Resources,
    string? SourceState,
    int? DataYear,
    double GdpPerCapita,
    double UnemploymentRate,
    double PovertyRate,
    double EconomicHealth,
    double CrimePressure);
