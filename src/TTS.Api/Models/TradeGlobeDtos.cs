namespace TTS.Api.Models;

public sealed class TradeHubRouteSummaryDto
{
    public required string CounterpartHubId { get; init; }
    public required string CounterpartName { get; init; }
    public required string Direction { get; init; }
    public int ShipmentCount { get; init; }
    public double TotalValueUsd { get; init; }
}

public sealed class TradeHubStatsDto
{
    public required string HubId { get; init; }
    public int OutboundShipments { get; init; }
    public int InboundShipments { get; init; }
    public double OutboundValueUsd { get; init; }
    public double InboundValueUsd { get; init; }
    public required IReadOnlyList<string> TopCommodities { get; init; }
    public required IReadOnlyList<TradeHubRouteSummaryDto> TopRoutes { get; init; }
}

public sealed class TradeCountryStatsDto
{
    public required string CountryId { get; init; }
    public required string DisplayName { get; init; }
    public int ShipmentCount { get; init; }
    public double TotalValueUsd { get; init; }
    public required string TopCommodity { get; init; }
}

public sealed class TradeHubDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string CountryId { get; init; }
    public double LatDeg { get; init; }
    public double LonDeg { get; init; }
    public required string TileId { get; init; }
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double CenterZ { get; init; }
    public bool IsPort { get; init; }
}

public sealed class TradeFlowDto
{
    public required string FromHubId { get; init; }
    public required string ToHubId { get; init; }
    public required string DeparturePort { get; init; }
    public required string ArrivalPort { get; init; }
    public required string ImportCountry { get; init; }
    public int ShipmentCount { get; init; }
    public double TotalValueUsd { get; init; }
}

public sealed class TradeGlobeDto
{
    public required HexMapDto Map { get; init; }
    public required IReadOnlyList<TradeCountryStatsDto> Countries { get; init; }
    public required IReadOnlyList<TradeHubDto> Hubs { get; init; }
    public required IReadOnlyList<TradeFlowDto> Flows { get; init; }
    public required IReadOnlyDictionary<string, string> TradeCountryByTileId { get; init; }
    public double MaxCountryValueUsd { get; init; }
    public required IReadOnlyList<string> Commodities { get; init; }
    public required IReadOnlyList<string> ImportCountries { get; init; }
    public required IReadOnlyList<string> TransportModes { get; init; }
    public required IReadOnlyList<string> ActiveCountryIds { get; init; }
    public required IReadOnlyList<string> ActiveHubIds { get; init; }
    public required IReadOnlyDictionary<string, TradeHubStatsDto> HubStats { get; init; }
    public string? AppliedCommodity { get; init; }
    public string? AppliedImportCountry { get; init; }
    public string? AppliedTransportMode { get; init; }
    public int FilteredShipmentCount { get; init; }
}
