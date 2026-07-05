namespace TTS.Core.Models;

/// <summary>Stylized Earth continents for the world hex map (not GIS-accurate).</summary>
public static class WorldMacroRegion
{
    public const string NorthAmerica = "north-america";
    public const string SouthAmerica = "south-america";
    public const string Europe = "europe";
    public const string Britain = "britain";
    public const string Africa = "africa";
    public const string MiddleEast = "middle-east";
    public const string Asia = "asia";
    public const string Australia = "australia";
    public const string Ocean = "ocean";

    public static readonly string[] Continents =
    [
        NorthAmerica,
        SouthAmerica,
        Europe,
        Africa,
        Asia,
        Australia
    ];

    public static string DisplayName(string? regionId) => regionId switch
    {
        NorthAmerica => "North America",
        SouthAmerica => "South America",
        Europe => "Europe",
        Britain => "Europe",
        Africa => "Africa",
        MiddleEast => "Asia",
        Asia => "Asia",
        Australia => "Australia",
        Ocean => "Ocean",
        _ => "Unknown"
    };

    /// <summary>Preferred spawn order for up to 6 civilizations (one per continent).</summary>
    public static readonly string[] SpawnOrder =
    [
        NorthAmerica,
        Europe,
        Asia,
        SouthAmerica,
        Africa,
        Australia
    ];
}
