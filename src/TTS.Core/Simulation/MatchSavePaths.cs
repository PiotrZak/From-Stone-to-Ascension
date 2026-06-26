namespace TTS.Core.Simulation;

using TTS.Core.Models;

/// <summary>Shared match save locations for API, grains, and recovery.</summary>
public static class MatchSavePaths
{
    public static string ResolveDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "matches");

    public static string ResolveSavePath(string matchId) =>
        Path.Combine(ResolveDirectory(), $"{matchId}.json");

    public static string ResolveTempSavePath(string matchId) =>
        Path.Combine(ResolveDirectory(), $"{matchId}.json.tmp");

    public static bool SaveExists(string matchId) =>
        File.Exists(ResolveSavePath(matchId));

    public static bool TryReadMatchStatus(string matchId, out MatchStatus status)
    {
        status = MatchStatus.Lobby;
        if (!SaveExists(matchId))
            return false;

        try
        {
            var doc = new MatchPersistence().Load(ResolveSavePath(matchId));
            if (doc.Match is null)
                return false;

            status = doc.Match.Status;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
