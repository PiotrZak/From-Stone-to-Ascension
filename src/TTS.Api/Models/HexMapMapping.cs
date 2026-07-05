namespace TTS.Api.Models;

using TTS.Contracts;
using TTS.Core.Models;

public static class HexMapMapping
{
    public static HexMapDto ToDto(GrainHexMap map) => new()
    {
        Width = map.Width,
        Height = map.Height,
        Seed = map.Seed,
        CapitalHexByCivilizationId = map.CapitalHexByCivilizationId,
        Tiles = map.Tiles.Select(t => new HexTileDto
        {
            Q = t.Q,
            R = t.R,
            Biome = t.Biome,
            ResourceYield = t.ResourceYield,
            ControllingCivilizationId = t.ControllingCivilizationId,
            IsCapital = t.IsCapital,
            WorldRegionId = t.WorldRegionId,
            WorldRegionName = WorldMacroRegion.DisplayName(t.WorldRegionId)
        }).ToList()
    };
}
