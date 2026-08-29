namespace TTS.Api.Models;

public sealed class SatelliteBodyDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ConstellationId { get; init; }
    public double AltitudeFactor { get; init; }
    public double InclinationRad { get; init; }
    public double PhaseRad { get; init; }
    public double AngularSpeed { get; init; }
    public required string Role { get; init; }
    public required string Operator { get; init; }
    public required string Country { get; init; }
    public required string Users { get; init; }
    public required string Purpose { get; init; }
    public required string OrbitClass { get; init; }
    public required string OrbitType { get; init; }
    public double PerigeeKm { get; init; }
    public double ApogeeKm { get; init; }
    public double InclinationDeg { get; init; }
    public double PeriodMinutes { get; init; }
    public required string LaunchDate { get; init; }
    public required string LaunchSite { get; init; }
    public required string NoradNumber { get; init; }
}

public sealed class SatelliteGroundStationDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Region { get; init; }
    public required string ConstellationId { get; init; }
    public required string TileId { get; init; }
    public double LatDeg { get; init; }
    public double LonDeg { get; init; }
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double CenterZ { get; init; }
    public int LaunchCount { get; init; }
}

public sealed class SatelliteConstellationDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Domain { get; init; }
    public required string Color { get; init; }
    public required string Summary { get; init; }
    public int SatelliteCount { get; init; }
    public double AverageCoverage { get; init; }
    public int GroundStationCount { get; init; }
}

public sealed class SatelliteGlobeDto
{
    public required HexMapDto Map { get; init; }
    public required IReadOnlyList<SatelliteConstellationDto> Constellations { get; init; }
    public required IReadOnlyList<SatelliteConstellationDto> AvailableConstellations { get; init; }
    public required IReadOnlyList<SatelliteBodyDto> Satellites { get; init; }
    public required IReadOnlyList<SatelliteGroundStationDto> GroundStations { get; init; }
    public required IReadOnlyDictionary<string, double> CoverageByTileId { get; init; }
    public double MaxCoverage { get; init; }
    public required IReadOnlyList<string> OrbitClasses { get; init; }
    public required IReadOnlyList<string> Countries { get; init; }
    public int TotalSatelliteCount { get; init; }
    public int VisibleSatelliteCount { get; init; }
    public string? AppliedPurposeGroupId { get; init; }
    public string? AppliedOrbitClass { get; init; }
    public string? AppliedCountry { get; init; }
}
