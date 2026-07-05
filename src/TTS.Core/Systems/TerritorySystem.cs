namespace TTS.Core.Systems;

using TTS.Core.Models;

public sealed class TerritorySystem
{
    public TerritoryClaimResult TryClaim(WorldState world, string civilizationId, string tileId)
    {
        if (world.Map is null)
            return TerritoryClaimResult.Rejected("This match has no hex map.");

        var tile = world.Map.GetTile(tileId);
        if (tile is null)
            return TerritoryClaimResult.Rejected("Tile is outside the map.");

        if (!tile.IsLand)
            return TerritoryClaimResult.Rejected("Cannot claim ocean tiles.");

        if (!string.IsNullOrEmpty(tile.ControllingCivilizationId))
            return TerritoryClaimResult.Rejected("Tile is already controlled.");

        if (!IsAdjacentToCivilization(world.Map, civilizationId, tile))
            return TerritoryClaimResult.Rejected("Tile must border your territory.");

        tile.ControllingCivilizationId = civilizationId;
        var region = world.Regions.FirstOrDefault(rgn => rgn.ControllingCivilizationId == civilizationId);
        if (region is not null)
        {
            tile.RegionId = region.Id;
            if (!region.HexKeys.Contains(tile.Id))
                region.HexKeys.Add(tile.Id);
        }

        return TerritoryClaimResult.Succeeded(tile.Id);
    }

    private static bool IsAdjacentToCivilization(HexMap map, string civilizationId, HexTile tile)
    {
        foreach (var neighbourId in tile.NeighbourIds)
        {
            if (map.GetTile(neighbourId)?.ControllingCivilizationId == civilizationId)
                return true;
        }

        return false;
    }
}

public readonly record struct TerritoryClaimResult(bool Success, string Message, string? HexKey = null)
{
    public static TerritoryClaimResult Succeeded(string hexKey) =>
        new(true, "Territory claimed.", hexKey);

    public static TerritoryClaimResult Rejected(string message) =>
        new(false, message);
}
