namespace TTS.Core.Systems;

using TTS.Core.Models;

public sealed class TerritorySystem
{
    public TerritoryClaimResult TryClaim(WorldState world, string civilizationId, int q, int r)
    {
        if (world.Map is null)
            return TerritoryClaimResult.Rejected("This match has no hex map.");

        var tile = world.Map.GetTile(q, r);
        if (tile is null)
            return TerritoryClaimResult.Rejected("Hex is outside the map.");

        if (!tile.IsLand)
            return TerritoryClaimResult.Rejected("Cannot claim ocean tiles.");

        if (!string.IsNullOrEmpty(tile.ControllingCivilizationId))
            return TerritoryClaimResult.Rejected("Hex is already controlled.");

        if (!IsAdjacentToCivilization(world.Map, civilizationId, q, r))
            return TerritoryClaimResult.Rejected("Hex must border your territory.");

        tile.ControllingCivilizationId = civilizationId;
        var region = world.Regions.FirstOrDefault(rgn => rgn.ControllingCivilizationId == civilizationId);
        if (region is not null)
        {
            tile.RegionId = region.Id;
            if (!region.HexKeys.Contains(tile.Key))
                region.HexKeys.Add(tile.Key);
        }

        return TerritoryClaimResult.Succeeded(tile.Key);
    }

    private static bool IsAdjacentToCivilization(HexMap map, string civilizationId, int q, int r)
    {
        foreach (var neighbor in HexCoordKey.Neighbors(q, r))
        {
            var adj = map.GetTile(neighbor.Q, neighbor.R);
            if (adj?.ControllingCivilizationId == civilizationId)
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
