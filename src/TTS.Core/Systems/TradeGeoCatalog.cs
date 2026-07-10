namespace TTS.Core.Systems;

public sealed record TradeHubDefinition(
    string Id,
    string Name,
    string CountryId,
    double LatDeg,
    double LonDeg,
    bool IsPort);

/// <summary>Lat/lon anchors for dataset departure and arrival hubs.</summary>
public static class TradeGeoCatalog
{
    public static readonly TradeHubDefinition[] Hubs =
    [
        new("beijing", "Beijing", TradeCountryZones.China, 39.90, 116.40, false),
        new("shanghai", "Shanghai", TradeCountryZones.China, 31.23, 121.47, true),
        new("shenzhen", "Shenzhen", TradeCountryZones.China, 22.54, 114.06, true),
        new("guangzhou", "Guangzhou", TradeCountryZones.China, 23.13, 113.26, true),
        new("qingdao", "Qingdao", TradeCountryZones.China, 36.07, 120.38, true),
        new("dubai", "Dubai", TradeCountryZones.MiddleEast, 25.20, 55.27, true),
        new("jeddah", "Jeddah", TradeCountryZones.MiddleEast, 21.54, 39.17, true),
        new("mumbai", "Mumbai", TradeCountryZones.India, 19.08, 72.88, true),
        new("colombo", "Colombo", TradeCountryZones.India, 6.93, 79.85, true),
        new("lagos", "Lagos", TradeCountryZones.Nigeria, 6.52, 3.38, true),
        new("mombasa", "Mombasa", TradeCountryZones.Kenya, -4.04, 39.67, true),
        new("dar-es-salaam", "Dar es Salaam", TradeCountryZones.Tanzania, -6.79, 39.28, true),
        new("durban", "Durban", TradeCountryZones.SouthAfrica, -29.86, 31.02, true),
        new("addis-ababa", "Addis Ababa", TradeCountryZones.Ethiopia, 9.03, 38.74, false),
        new("cairo", "Cairo", TradeCountryZones.Egypt, 30.04, 31.24, false),
        new("tema", "Tema", TradeCountryZones.Ghana, 5.67, -0.02, true)
    ];

    public static TradeHubDefinition? FindByName(string name) =>
        Hubs.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static string HubIdForPort(string portName)
    {
        var hub = FindByName(portName);
        return hub?.Id ?? portName.Trim().ToLowerInvariant().Replace(' ', '-');
    }
}
