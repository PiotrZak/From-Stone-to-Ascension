namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// TTS 4+ crime and socioeconomic pressure from regional data.
/// Unlocks the "Crime perspective" — stability responds to poverty, inequality, and crime rates.
/// </summary>
public class CrimeSystem
{
    public const string CybersecurityTechId = "tech-cybersecurity";

    public void ApplyTurnPressure(Civilization civilization, WorldState world)
    {
        if (civilization.CurrentTier < TechTier.InformationAge)
            return;

        var controlledRegions = world.Regions
            .Where(r => r.ControllingCivilizationId == civilization.Id && r.CrimeProfile is not null)
            .ToList();

        if (controlledRegions.Count == 0)
            return;

        var avgPressure = controlledRegions.Average(r => r.CrimeProfile!.CrimePressureIndex);
        var mitigation = civilization.ResearchedTechnologyIds.Contains(CybersecurityTechId) ? 0.6 : 1.0;
        var penalty = avgPressure * 0.04 * mitigation;

        civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability - penalty, 0, 100);
        civilization.EconomicStability = Math.Clamp(
            civilization.EconomicStability - penalty * 0.7, 0, 100);
    }

    public CrimePerspectiveSummary GetPerspective(Civilization civilization, WorldState world)
    {
        var regions = world.Regions
            .Where(r => r.ControllingCivilizationId == civilization.Id && r.CrimeProfile is not null)
            .ToList();

        if (regions.Count == 0 || civilization.CurrentTier < TechTier.InformationAge)
            return CrimePerspectiveSummary.Unavailable();

        var profiles = regions.Select(r => r.CrimeProfile!).ToList();
        var hasCyber = civilization.ResearchedTechnologyIds.Contains(CybersecurityTechId);

        return new CrimePerspectiveSummary(
            Available: true,
            AverageCrimePressure: profiles.Average(p => p.CrimePressureIndex),
            AverageViolentCrimeRate: profiles.Average(p => p.ViolentCrimeRate),
            AveragePovertyRate: profiles.Average(p => p.PovertyRate),
            AverageGini: profiles.Average(p => p.GiniCoefficient),
            CybersecurityMitigationActive: hasCyber,
            Regions: regions.Select(r => new RegionCrimeSnapshot(
                r.Name,
                r.CrimeProfile!.SourceState,
                r.CrimeProfile.DataYear,
                r.CrimeProfile.CrimePressureIndex)).ToList());
    }
}

public readonly record struct CrimePerspectiveSummary(
    bool Available,
    double AverageCrimePressure = 0,
    double AverageViolentCrimeRate = 0,
    double AveragePovertyRate = 0,
    double AverageGini = 0,
    bool CybersecurityMitigationActive = false,
    IReadOnlyList<RegionCrimeSnapshot>? Regions = null)
{
    public static CrimePerspectiveSummary Unavailable() => new(false);
}

public readonly record struct RegionCrimeSnapshot(
    string RegionName,
    string SourceState,
    int DataYear,
    double CrimePressureIndex);
