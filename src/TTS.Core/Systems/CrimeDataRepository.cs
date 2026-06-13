namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>Loads and caches state crime/income records from CSV.</summary>
public sealed class CrimeDataRepository
{
    private static CrimeDataRepository? _default;
    private readonly List<StateCrimeRecord> _records;
    private readonly bool _isLoaded;

    public CrimeDataRepository(string? csvPath = null)
    {
        var path = csvPath ?? ResolveDefaultPath();
        _isLoaded = File.Exists(path);
        _records = _isLoaded ? LoadRecords(path) : [];
    }

    public static CrimeDataRepository Default => _default ??= new CrimeDataRepository();

    public bool IsLoaded => _isLoaded;

    public IReadOnlyList<StateCrimeRecord> Records => _records;

    public StateCrimeRecord? GetRecord(string stateName, int? year = null)
    {
        var matches = _records
            .Where(r => r.State.Equals(stateName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return null;

        return year.HasValue
            ? matches.FirstOrDefault(r => r.Year == year) ?? matches.OrderByDescending(r => r.Year).First()
            : matches.OrderByDescending(r => r.Year).First();
    }

    public RegionalCrimeProfile? ToProfile(string stateName, int? year = null)
    {
        var record = GetRecord(stateName, year);
        return record?.ToProfile();
    }

    public static string ResolveDefaultPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "state_crime_income_merged.csv"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "data", "state_crime_income_merged.csv"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data", "state_crime_income_merged.csv"))
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static List<StateCrimeRecord> LoadRecords(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            return [];

        var records = new List<StateCrimeRecord>();
        for (var i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            if (fields.Count < 33)
                continue;

            if (!int.TryParse(fields[1], out var year))
                continue;

            records.Add(new StateCrimeRecord
            {
                State = fields[0],
                Year = year,
                StateAbbreviation = fields[14],
                Population = ParseLong(fields[15]),
                ViolentCrime = ParseLong(fields[16]),
                PropertyCrime = ParseLong(fields[17]),
                Location = fields[20],
                GdpPerCapita = ParseDouble(fields[24]),
                GiniCoefficient = ParseDouble(fields[25]),
                CorruptionPerMillion = ParseDouble(fields[29]),
                PovertyRate = ParseDouble(fields[31]),
                UnemploymentRate = ParseDouble(fields[32])
            });
        }

        return records;
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

    private static double ParseDouble(string value) =>
        double.TryParse(value, out var result) ? result : 0;

    private static long ParseLong(string value) =>
        long.TryParse(value, out var result) ? result : 0;
}

public sealed class StateCrimeRecord
{
    public string State { get; init; } = "";
    public int Year { get; init; }
    public string StateAbbreviation { get; init; } = "";
    public long Population { get; init; }
    public long ViolentCrime { get; init; }
    public long PropertyCrime { get; init; }
    public string Location { get; init; } = "";
    public double GdpPerCapita { get; init; }
    public double GiniCoefficient { get; init; }
    public double CorruptionPerMillion { get; init; }
    public double PovertyRate { get; init; }
    public double UnemploymentRate { get; init; }

    public RegionalCrimeProfile ToProfile()
    {
        var population = Math.Max(Population, 1);
        return new RegionalCrimeProfile
        {
            SourceState = State,
            StateAbbreviation = StateAbbreviation,
            DataYear = Year,
            Location = Location,
            ViolentCrimeRate = ViolentCrime * 100_000.0 / population,
            PropertyCrimeRate = PropertyCrime * 100_000.0 / population,
            PovertyRate = PovertyRate,
            GiniCoefficient = GiniCoefficient,
            GdpPerCapita = GdpPerCapita,
            UnemploymentRate = UnemploymentRate,
            CorruptionConvictionsPerMillion = CorruptionPerMillion
        };
    }
}
