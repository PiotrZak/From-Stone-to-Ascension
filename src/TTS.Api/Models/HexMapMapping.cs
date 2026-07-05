namespace TTS.Api.Models;

using TTS.Contracts;
using TTS.Core.Models;

public static class HexMapMapping
{
    public static HexMapDto ToDto(GrainHexMap map) => new()
    {
        PlanetRadius = map.PlanetRadius,
        Frequency = map.Frequency,
        Seed = map.Seed,
        CapitalTileByCivilizationId = map.CapitalTileByCivilizationId,
        Tiles = map.Tiles.Select(t => new HexTileDto
        {
            Id = t.Id,
            Biome = t.Biome,
            ResourceYield = t.ResourceYield,
            ControllingCivilizationId = t.ControllingCivilizationId,
            IsCapital = t.IsCapital,
            WorldRegionId = t.WorldRegionId,
            WorldRegionName = WorldMacroRegion.DisplayName(t.WorldRegionId),
            CenterX = t.CenterX,
            CenterY = t.CenterY,
            CenterZ = t.CenterZ,
            NormalX = t.NormalX,
            NormalY = t.NormalY,
            NormalZ = t.NormalZ,
            PolygonVertices = t.PolygonVertices
                .Select(v => new Vec3Dto { X = v.X, Y = v.Y, Z = v.Z })
                .ToList(),
            NeighbourIds = t.NeighbourIds,
            IsPentagon = t.IsPentagon
        }).ToList()
    };
}
