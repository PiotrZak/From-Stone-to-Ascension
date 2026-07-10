namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>Country zones for China–Africa trade choropleth plus Indian Ocean corridor.</summary>
public static class TradeCountryZones
{
    public const string China = "china";
    public const string India = "india";
    public const string MiddleEast = "middle-east";
    public const string Nigeria = "nigeria";
    public const string Kenya = "kenya";
    public const string Tanzania = "tanzania";
    public const string Ethiopia = "ethiopia";
    public const string Egypt = "egypt";
    public const string Ghana = "ghana";
    public const string SouthAfrica = "south-africa";

    public static readonly string[] DatasetCountries =
    [
        China,
        Nigeria,
        Kenya,
        Tanzania,
        Ethiopia,
        Egypt,
        Ghana,
        SouthAfrica
    ];

    public static readonly string[] CorridorCountries =
    [
        India,
        MiddleEast
    ];

    public static readonly string[] MapCountries =
    [
        ..DatasetCountries,
        ..CorridorCountries
    ];

    private const double DatasetThreshold = 0.22;
    private const double CorridorThreshold = 0.20;

    private static readonly EarthZoneLayout.Zone[] DatasetZones =
    [
        new(China, 0.78, 0.34, 0.10, 0.14, 1.1),
        new(Nigeria, 0.50, 0.56, 0.05, 0.07, 1.0),
        new(Kenya, 0.57, 0.60, 0.04, 0.05, 1.0),
        new(Tanzania, 0.56, 0.63, 0.04, 0.05, 1.0),
        new(Ethiopia, 0.57, 0.54, 0.04, 0.05, 1.0),
        new(Egypt, 0.54, 0.42, 0.04, 0.05, 1.0),
        new(Ghana, 0.47, 0.57, 0.04, 0.05, 1.0),
        new(SouthAfrica, 0.53, 0.72, 0.05, 0.06, 1.0)
    ];

    private static readonly EarthZoneLayout.Zone[] CorridorZones =
    [
        new(India, 0.71, 0.40, 0.055, 0.075, 1.05),
        new(MiddleEast, 0.58, 0.37, 0.075, 0.055, 1.0)
    ];

    public static string ClassifyCountry(double nx, double ny)
    {
        var dataset = EarthZoneLayout.ClassifyRegion(nx, ny, DatasetZones, DatasetThreshold);
        if (DatasetCountries.Contains(dataset))
            return dataset;

        var corridor = EarthZoneLayout.ClassifyRegion(nx, ny, CorridorZones, CorridorThreshold);
        if (CorridorCountries.Contains(corridor))
            return corridor;

        return WorldMacroRegion.Ocean;
    }

    public static bool IsMapCountry(string countryId) => MapCountries.Contains(countryId);

    public static string DisplayName(string countryId) => countryId switch
    {
        China => "China",
        India => "India",
        MiddleEast => "Middle East",
        Nigeria => "Nigeria",
        Kenya => "Kenya",
        Tanzania => "Tanzania",
        Ethiopia => "Ethiopia",
        Egypt => "Egypt",
        Ghana => "Ghana",
        SouthAfrica => "South Africa",
        _ => countryId
    };

    public static string SlugFromDatasetName(string name) => name.Trim().ToLowerInvariant() switch
    {
        "china" => China,
        "nigeria" => Nigeria,
        "kenya" => Kenya,
        "tanzania" => Tanzania,
        "ethiopia" => Ethiopia,
        "egypt" => Egypt,
        "ghana" => Ghana,
        "south africa" => SouthAfrica,
        _ => name.Trim().ToLowerInvariant().Replace(' ', '-')
    };
}
