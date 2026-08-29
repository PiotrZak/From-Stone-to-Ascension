namespace TTS.Api.Models;

using TTS.Core.Systems;

public static class SatelliteGlobeMapping
{
    public static SatelliteGlobeDto ToDto(SatelliteGlobeModel model) => new()
    {
        Map = HexMapMapping.ToDto(model.Map),
        Constellations = model.Constellations.Select(MapConstellation).ToList(),
        AvailableConstellations = model.AvailableConstellations.Select(MapConstellation).ToList(),
        Satellites = model.Satellites.Select(s => new SatelliteBodyDto
        {
            Id = s.Id,
            Name = s.Name,
            ConstellationId = s.ConstellationId,
            AltitudeFactor = s.AltitudeFactor,
            InclinationRad = s.InclinationRad,
            PhaseRad = s.PhaseRad,
            AngularSpeed = s.AngularSpeed,
            Role = s.Role,
            Operator = s.Operator,
            Country = s.Country,
            Users = s.Users,
            Purpose = s.Purpose,
            OrbitClass = s.OrbitClass,
            OrbitType = s.OrbitType,
            PerigeeKm = s.PerigeeKm,
            ApogeeKm = s.ApogeeKm,
            InclinationDeg = s.InclinationDeg,
            PeriodMinutes = s.PeriodMinutes,
            LaunchDate = s.LaunchDate,
            LaunchSite = s.LaunchSite,
            NoradNumber = s.NoradNumber
        }).ToList(),
        GroundStations = model.GroundStations.Select(g => new SatelliteGroundStationDto
        {
            Id = g.Id,
            Name = g.Name,
            Region = g.Region,
            ConstellationId = g.ConstellationId,
            TileId = g.TileId,
            LatDeg = g.LatDeg,
            LonDeg = g.LonDeg,
            CenterX = g.CenterX,
            CenterY = g.CenterY,
            CenterZ = g.CenterZ,
            LaunchCount = g.LaunchCount
        }).ToList(),
        CoverageByTileId = model.CoverageByTileId,
        MaxCoverage = model.MaxCoverage,
        OrbitClasses = model.OrbitClasses,
        Countries = model.Countries,
        TotalSatelliteCount = model.TotalSatelliteCount,
        VisibleSatelliteCount = model.VisibleSatelliteCount,
        AppliedPurposeGroupId = model.AppliedFilter.PurposeGroupId,
        AppliedOrbitClass = model.AppliedFilter.OrbitClass,
        AppliedCountry = model.AppliedFilter.Country
    };

    private static SatelliteConstellationDto MapConstellation(SatelliteConstellationInfo c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Domain = c.Domain,
        Color = c.Color,
        Summary = c.Summary,
        SatelliteCount = c.SatelliteCount,
        AverageCoverage = c.AverageCoverage,
        GroundStationCount = c.GroundStationCount
    };
}
