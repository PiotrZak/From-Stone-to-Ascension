namespace TTS.Core.Systems;

public sealed record TradeShipment(
    string ShipmentId,
    string ExportCountry,
    string ImportCountry,
    string Commodity,
    string DeparturePort,
    string ArrivalPort,
    string TransportMode,
    double DeclaredValueUsd);

public sealed record TradeCountryAggregate(
    string CountryId,
    int ShipmentCount,
    double TotalValueUsd,
    string TopCommodity);

public sealed record TradeRouteAggregate(
    string DeparturePort,
    string ArrivalPort,
    string ImportCountry,
    int ShipmentCount,
    double TotalValueUsd);

public sealed record TradeDataset(
    IReadOnlyList<TradeShipment> Shipments,
    IReadOnlyList<TradeCountryAggregate> ByImportCountry,
    IReadOnlyList<TradeCountryAggregate> ByExportCountry,
    IReadOnlyList<TradeRouteAggregate> Routes,
    IReadOnlyList<string> Commodities,
    IReadOnlyList<string> ImportCountries,
    IReadOnlyList<string> TransportModes);

/// <summary>Loads China–Africa trade shipments from CSV.</summary>
public static class TradeDatasetLoader
{
    public static TradeDataset Load(string? csvPath = null)
    {
        var path = csvPath ?? ResolveDefaultPath();
        var shipments = File.Exists(path) ? LoadShipments(path) : [];

        var byImport = shipments
            .GroupBy(s => s.ImportCountry, StringComparer.OrdinalIgnoreCase)
            .Select(g => ToCountryAggregate(g.Key, g))
            .OrderByDescending(c => c.TotalValueUsd)
            .ToList();

        var byExport = shipments
            .GroupBy(s => s.ExportCountry, StringComparer.OrdinalIgnoreCase)
            .Select(g => ToCountryAggregate(g.Key, g))
            .OrderByDescending(c => c.TotalValueUsd)
            .ToList();

        var routes = shipments
            .GroupBy(s => (s.DeparturePort, s.ArrivalPort, s.ImportCountry))
            .Select(g => new TradeRouteAggregate(
                g.Key.DeparturePort,
                g.Key.ArrivalPort,
                g.Key.ImportCountry,
                g.Count(),
                g.Sum(s => s.DeclaredValueUsd)))
            .OrderByDescending(r => r.TotalValueUsd)
            .ToList();

        return new TradeDataset(
            shipments,
            byImport,
            byExport,
            routes,
            shipments.Select(s => s.Commodity).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList(),
            shipments.Select(s => s.ImportCountry).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList(),
            shipments.Select(s => s.TransportMode).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList());
    }

    public static TradeDataset Filter(
        TradeDataset dataset,
        string? commodity,
        string? importCountry,
        string? transportMode = null)
    {
        var shipments = dataset.Shipments.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(commodity))
            shipments = shipments.Where(s => s.Commodity.Equals(commodity, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(importCountry))
            shipments = shipments.Where(s => s.ImportCountry.Equals(importCountry, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(transportMode))
            shipments = shipments.Where(s => s.TransportMode.Equals(transportMode, StringComparison.OrdinalIgnoreCase));

        var filtered = shipments.ToList();
        var byImport = filtered
            .GroupBy(s => s.ImportCountry, StringComparer.OrdinalIgnoreCase)
            .Select(g => ToCountryAggregate(g.Key, g))
            .OrderByDescending(c => c.TotalValueUsd)
            .ToList();
        var byExport = filtered
            .GroupBy(s => s.ExportCountry, StringComparer.OrdinalIgnoreCase)
            .Select(g => ToCountryAggregate(g.Key, g))
            .OrderByDescending(c => c.TotalValueUsd)
            .ToList();
        var routes = filtered
            .GroupBy(s => (s.DeparturePort, s.ArrivalPort, s.ImportCountry))
            .Select(g => new TradeRouteAggregate(
                g.Key.DeparturePort,
                g.Key.ArrivalPort,
                g.Key.ImportCountry,
                g.Count(),
                g.Sum(s => s.DeclaredValueUsd)))
            .OrderByDescending(r => r.TotalValueUsd)
            .ToList();

        return new TradeDataset(
            filtered,
            byImport,
            byExport,
            routes,
            dataset.Commodities,
            dataset.ImportCountries,
            dataset.TransportModes);
    }

    public static string ResolveDefaultPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "china_africa_trade_dataset.csv"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "data", "china_africa_trade_dataset.csv"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data", "china_africa_trade_dataset.csv"))
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static List<TradeShipment> LoadShipments(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            return [];

        var header = ParseCsvLine(lines[0]);
        var idx = header
            .Select((name, i) => (name.Trim().ToLowerInvariant(), i))
            .ToDictionary(x => x.Item1, x => x.i, StringComparer.Ordinal);

        int Col(string name) => idx[name];

        var records = new List<TradeShipment>(lines.Length - 1);
        for (var i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            if (fields.Count < header.Count)
                continue;

            if (!double.TryParse(fields[Col("declared_value_usd")], out var value))
                continue;

            records.Add(new TradeShipment(
                fields[Col("shipment_id")],
                fields[Col("export_country")],
                fields[Col("import_country")],
                fields[Col("commodity")],
                fields[Col("departure_port")],
                fields[Col("arrival_port")],
                fields[Col("transport_mode")],
                value));
        }

        return records;
    }

    private static TradeCountryAggregate ToCountryAggregate(string countryId, IEnumerable<TradeShipment> group)
    {
        var list = group.ToList();
        var topCommodity = list
            .GroupBy(s => s.Commodity, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Sum(s => s.DeclaredValueUsd))
            .First()
            .Key;

        return new TradeCountryAggregate(
            countryId,
            list.Count,
            list.Sum(s => s.DeclaredValueUsd),
            topCommodity);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                fields.Add(current);
                current = "";
                continue;
            }

            current += ch;
        }

        fields.Add(current);
        return fields;
    }
}
