namespace TTS.Core.Systems;

/// <summary>One row from satelites.csv (UCS-style satellite database).</summary>
public sealed record SatelliteCatalogEntry(
    string Id,
    string Name,
    string Operator,
    string Country,
    string Users,
    string Purpose,
    string PurposeGroupId,
    string DetailedPurpose,
    string OrbitClass,
    string OrbitType,
    double? GeoLongitudeDeg,
    double PerigeeKm,
    double ApogeeKm,
    double InclinationDeg,
    double PeriodMinutes,
    string LaunchDate,
    string LaunchSite,
    string LaunchVehicle,
    string NoradNumber);

public sealed record SatelliteDataset(
    IReadOnlyList<SatelliteCatalogEntry> Satellites,
    IReadOnlyList<string> PurposeGroups,
    IReadOnlyList<string> OrbitClasses,
    IReadOnlyList<string> Countries);

/// <summary>Loads UCS-style satellite catalog CSV.</summary>
public static class SatelliteDatasetLoader
{
    public static SatelliteDataset Load(string? csvPath = null)
    {
        var path = csvPath ?? ResolveDefaultPath();
        var satellites = File.Exists(path) ? LoadEntries(path) : [];

        return new SatelliteDataset(
            satellites,
            satellites.Select(s => s.PurposeGroupId).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
            satellites.Select(s => s.OrbitClass).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
            satellites.Select(s => s.Country).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList());
    }

    public static SatelliteDataset Filter(
        SatelliteDataset dataset,
        string? purposeGroupId = null,
        string? orbitClass = null,
        string? country = null)
    {
        var rows = dataset.Satellites.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(purposeGroupId))
            rows = rows.Where(s => s.PurposeGroupId.Equals(purposeGroupId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(orbitClass))
            rows = rows.Where(s => s.OrbitClass.Equals(orbitClass, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(country))
            rows = rows.Where(s => s.Country.Equals(country, StringComparison.OrdinalIgnoreCase));

        var filtered = rows.ToList();
        return new SatelliteDataset(
            filtered,
            dataset.PurposeGroups,
            dataset.OrbitClasses,
            dataset.Countries);
    }

    public static string ResolveDefaultPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "satelites.csv"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "data", "satelites.csv"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data", "satelites.csv"))
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    public static string PurposeGroupIdFromPurpose(string purpose)
    {
        var p = purpose.Trim().ToLowerInvariant();
        if (p.Contains("navigation"))
            return "navigation";
        if (p.Contains("earth observation") || p.Contains("earth science") || p.Contains("earth/space"))
            return "earth-observation";
        if (p.Contains("communication"))
            return "communications";
        if (p.Contains("technology"))
            return "technology";
        if (p.Contains("space science") || p.Contains("space observation"))
            return "space-science";
        return "other";
    }

    public static string PurposeGroupDisplayName(string groupId) => groupId switch
    {
        "communications" => "Communications",
        "earth-observation" => "Earth Observation",
        "navigation" => "Navigation",
        "technology" => "Technology Development",
        "space-science" => "Space Science",
        _ => "Other"
    };

    public static string PurposeGroupColor(string groupId) => groupId switch
    {
        "communications" => "#38bdf8",
        "earth-observation" => "#f472b6",
        "navigation" => "#a3e635",
        "technology" => "#c084fc",
        "space-science" => "#fbbf24",
        _ => "#94a3b8"
    };

    private static List<SatelliteCatalogEntry> LoadEntries(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            return [];

        var header = ParseCsvLine(lines[0]);
        var idx = header
            .Select((name, i) => (NormalizeHeader(name), i))
            .GroupBy(x => x.Item1)
            .ToDictionary(g => g.Key, g => g.First().i, StringComparer.Ordinal);

        int? Col(string name) => idx.TryGetValue(NormalizeHeader(name), out var i) ? i : null;
        string Get(List<string> fields, string name)
        {
            var i = Col(name);
            if (i is null || i.Value >= fields.Count) return "";
            return fields[i.Value].Trim();
        }

        var records = new List<SatelliteCatalogEntry>(lines.Length - 1);
        for (var row = 1; row < lines.Length; row++)
        {
            var fields = ParseCsvLine(lines[row]);
            if (fields.Count == 0) continue;

            var name = Get(fields, "Official Name of Satellite");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var purpose = Get(fields, "Purpose");
            var orbitClass = Get(fields, "Class of Orbit");
            var norad = Get(fields, "NORAD Number");
            var id = !string.IsNullOrWhiteSpace(norad)
                ? $"norad-{norad}"
                : $"sat-{Slug(name)}-{row}";

            records.Add(new SatelliteCatalogEntry(
                id,
                name,
                Get(fields, "Operator/Owner"),
                Get(fields, "Country of Operator/Owner"),
                Get(fields, "Users"),
                purpose,
                PurposeGroupIdFromPurpose(purpose),
                Get(fields, "Detailed Purpose"),
                orbitClass,
                Get(fields, "Type of Orbit"),
                ParseNullableDouble(Get(fields, "Longitude of Geosynchronous Orbit (Degrees)")),
                ParseDouble(Get(fields, "Perigee (Kilometers)")),
                ParseDouble(Get(fields, "Apogee (Kilometers)")),
                ParseDouble(Get(fields, "Inclination (Degrees)")),
                ParseDouble(Get(fields, "Period (Minutes)")),
                Get(fields, "Date of Launch"),
                Get(fields, "Launch Site"),
                Get(fields, "Launch Vehicle"),
                norad));
        }

        return records;
    }

    private static string NormalizeHeader(string name) =>
        name.Trim().ToLowerInvariant();

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or ' ')
            .Select(c => c == ' ' ? '-' : c)
            .ToArray();
        return new string(chars).Trim('-');
    }

    private static double ParseDouble(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        raw = raw.Replace(",", "").Trim();
        return double.TryParse(raw, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : 0;
    }

    private static double? ParseNullableDouble(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Replace(",", "").Trim();
        return double.TryParse(raw, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : null;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
