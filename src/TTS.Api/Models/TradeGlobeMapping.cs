namespace TTS.Api.Models;

using TTS.Core.Systems;

public static class TradeGlobeMapping
{
    public static TradeGlobeDto ToDto(TradeGlobeModel model) => new()
    {
        Map = HexMapMapping.ToDto(model.Map),
        Countries = model.CountryStatsById.Values
            .Select(c => new TradeCountryStatsDto
            {
                CountryId = c.CountryId,
                DisplayName = TradeCountryZones.DisplayName(c.CountryId),
                ShipmentCount = c.ShipmentCount,
                TotalValueUsd = c.TotalValueUsd,
                TopCommodity = c.TopCommodity
            })
            .OrderByDescending(c => c.TotalValueUsd)
            .ToList(),
        Hubs = model.Hubs.Select(h => new TradeHubDto
        {
            Id = h.Id,
            Name = h.Name,
            CountryId = h.CountryId,
            LatDeg = h.LatDeg,
            LonDeg = h.LonDeg,
            TileId = h.TileId,
            CenterX = h.CenterX,
            CenterY = h.CenterY,
            CenterZ = h.CenterZ,
            IsPort = h.IsPort
        }).ToList(),
        Flows = model.Flows.Select(f => new TradeFlowDto
        {
            FromHubId = f.FromHubId,
            ToHubId = f.ToHubId,
            DeparturePort = f.DeparturePort,
            ArrivalPort = f.ArrivalPort,
            ImportCountry = f.ImportCountry,
            ShipmentCount = f.ShipmentCount,
            TotalValueUsd = f.TotalValueUsd
        }).ToList(),
        TradeCountryByTileId = model.TradeCountryByTileId,
        MaxCountryValueUsd = model.MaxCountryValueUsd,
        Commodities = model.Commodities,
        ImportCountries = model.ImportCountries,
        TransportModes = model.TransportModes,
        ActiveCountryIds = model.ActiveCountryIds,
        ActiveHubIds = model.ActiveHubIds,
        HubStats = model.HubStatsById.ToDictionary(
            kv => kv.Key,
            kv => new TradeHubStatsDto
            {
                HubId = kv.Value.HubId,
                OutboundShipments = kv.Value.OutboundShipments,
                InboundShipments = kv.Value.InboundShipments,
                OutboundValueUsd = kv.Value.OutboundValueUsd,
                InboundValueUsd = kv.Value.InboundValueUsd,
                TopCommodities = kv.Value.TopCommodities,
                TopRoutes = kv.Value.TopRoutes.Select(r => new TradeHubRouteSummaryDto
                {
                    CounterpartHubId = r.CounterpartHubId,
                    CounterpartName = r.CounterpartName,
                    Direction = r.Direction,
                    ShipmentCount = r.ShipmentCount,
                    TotalValueUsd = r.TotalValueUsd
                }).ToList()
            }),
        AppliedCommodity = model.AppliedFilter.Commodity,
        AppliedImportCountry = model.AppliedFilter.ImportCountry,
        AppliedTransportMode = model.AppliedFilter.TransportMode,
        FilteredShipmentCount = model.FilteredShipmentCount
    };
}
