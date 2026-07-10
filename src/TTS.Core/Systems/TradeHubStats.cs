namespace TTS.Core.Systems;

public sealed record TradeHubRouteSummary(
    string CounterpartHubId,
    string CounterpartName,
    string Direction,
    int ShipmentCount,
    double TotalValueUsd);

public sealed record TradeHubStats(
    string HubId,
    int OutboundShipments,
    int InboundShipments,
    double OutboundValueUsd,
    double InboundValueUsd,
    IReadOnlyList<string> TopCommodities,
    IReadOnlyList<TradeHubRouteSummary> TopRoutes);

public static class TradeHubStatsBuilder
{
  private const int MaxTopRoutes = 6;

  public static IReadOnlyDictionary<string, TradeHubStats> Build(
      TradeDataset dataset,
      IReadOnlyList<TradeGlobeHub> hubs)
  {
    var hubByName = hubs.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);
    var outbound = hubs.ToDictionary(h => h.Id, _ => new RouteAccumulator());
    var inbound = hubs.ToDictionary(h => h.Id, _ => new RouteAccumulator());
    var commodities = hubs.ToDictionary(h => h.Id, _ => new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase));

    foreach (var shipment in dataset.Shipments)
    {
      if (hubByName.TryGetValue(shipment.DeparturePort, out var from))
      {
        outbound[from.Id].Add(shipment.ArrivalPort, shipment.DeclaredValueUsd);
        AddCommodity(commodities[from.Id], shipment.Commodity, shipment.DeclaredValueUsd);
      }

      if (hubByName.TryGetValue(shipment.ArrivalPort, out var to))
      {
        inbound[to.Id].Add(shipment.DeparturePort, shipment.DeclaredValueUsd);
        AddCommodity(commodities[to.Id], shipment.Commodity, shipment.DeclaredValueUsd);
      }
    }

    return hubs.ToDictionary(
        h => h.Id,
        h => ToStats(h, outbound[h.Id], inbound[h.Id], commodities[h.Id], hubByName));
  }

  private static void AddCommodity(Dictionary<string, double> bucket, string commodity, double value)
  {
    bucket[commodity] = bucket.GetValueOrDefault(commodity) + value;
  }

  private static TradeHubStats ToStats(
      TradeGlobeHub hub,
      RouteAccumulator outbound,
      RouteAccumulator inbound,
      Dictionary<string, double> commodities,
      IReadOnlyDictionary<string, TradeGlobeHub> hubByName)
  {
    var topCommodities = commodities
        .OrderByDescending(kv => kv.Value)
        .Take(3)
        .Select(kv => kv.Key)
        .ToList();

    var routes = outbound.Routes
        .Select(kv => ToRoute(hub.Id, kv.Key, "Outbound", kv.Value, hubByName))
        .Concat(inbound.Routes.Select(kv => ToRoute(hub.Id, kv.Key, "Inbound", kv.Value, hubByName)))
        .OrderByDescending(r => r.TotalValueUsd)
        .Take(MaxTopRoutes)
        .ToList();

    return new TradeHubStats(
        hub.Id,
        outbound.Shipments,
        inbound.Shipments,
        outbound.Value,
        inbound.Value,
        topCommodities,
        routes);
  }

  private static TradeHubRouteSummary ToRoute(
      string hubId,
      string counterpartPort,
      string direction,
      RouteTotals totals,
      IReadOnlyDictionary<string, TradeGlobeHub> hubByName)
  {
    hubByName.TryGetValue(counterpartPort, out var counterpart);
    return new TradeHubRouteSummary(
        counterpart?.Id ?? TradeGeoCatalog.HubIdForPort(counterpartPort),
        counterpartPort,
        direction,
        totals.Shipments,
        totals.Value);
  }

  private sealed class RouteAccumulator
  {
    private readonly Dictionary<string, RouteTotals> _routes = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, RouteTotals> Routes => _routes;
    public int Shipments { get; private set; }
    public double Value { get; private set; }

    public void Add(string counterpartPort, double value)
    {
      Shipments++;
      Value += value;
      if (!_routes.TryGetValue(counterpartPort, out var totals))
      {
        totals = new RouteTotals();
        _routes[counterpartPort] = totals;
      }

      totals.Shipments++;
      totals.Value += value;
    }
  }

  private sealed class RouteTotals
  {
    public int Shipments { get; set; }
    public double Value { get; set; }
  }
}
