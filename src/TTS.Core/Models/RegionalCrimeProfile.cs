namespace TTS.Core.Models;

/// <summary>
/// Socioeconomic and crime indicators for a region (TTS 4+ Information Age perspective).
/// Sourced from real-world state-level data in state_crime_income_merged.csv.
/// </summary>
public class RegionalCrimeProfile
{
    public string SourceState { get; set; } = "";
    public string StateAbbreviation { get; set; } = "";
    public int DataYear { get; set; }
    public string Location { get; set; } = "";

    /// <summary>Violent crimes per 100,000 population.</summary>
    public double ViolentCrimeRate { get; set; }

    /// <summary>Property crimes per 100,000 population.</summary>
    public double PropertyCrimeRate { get; set; }

    public double PovertyRate { get; set; }
    public double GiniCoefficient { get; set; }
    public double GdpPerCapita { get; set; }
    public double UnemploymentRate { get; set; }
    public double CorruptionConvictionsPerMillion { get; set; }

    /// <summary>Composite 0–100 pressure index used by CrimeSystem.</summary>
    public double CrimePressureIndex =>
        Math.Clamp(
            ViolentCrimeRate * 0.05 +
            PropertyCrimeRate * 0.01 +
            PovertyRate * 0.6 +
            GiniCoefficient * 15 +
            UnemploymentRate * 0.4 +
            CorruptionConvictionsPerMillion * 0.3,
            0, 100);
}
