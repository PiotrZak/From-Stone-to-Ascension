namespace TTS.Api.Models;

using TTS.Contracts;

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
            IsCapital = t.IsCapital
        }).ToList()
    };
}
