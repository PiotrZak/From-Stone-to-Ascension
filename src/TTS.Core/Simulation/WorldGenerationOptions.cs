namespace TTS.Core.Simulation;

using TTS.Core.Models;

/// <summary>Controls how a match world is bootstrapped.</summary>
public sealed class WorldGenerationOptions
{
    public required int Seed { get; init; }
    public int CivilizationCount { get; init; } = 2;
    public bool UseCrimeDataAnchors { get; init; } = true;
    public bool UseStandardArena { get; init; }

    public static WorldGenerationOptions FromMatch(MatchConfig config, string matchId, int? seedOverride = null) =>
        new()
        {
            Seed = seedOverride ?? MatchSeeds.FromMatchId(matchId),
            CivilizationCount = Math.Clamp(config.MaxPlayers, 2, 8),
            UseCrimeDataAnchors = true,
            UseStandardArena = false
        };

    public static WorldGenerationOptions Standard(string matchId) =>
        new()
        {
            Seed = MatchSeeds.FromMatchId(matchId),
            CivilizationCount = 2,
            UseCrimeDataAnchors = true,
            UseStandardArena = true
        };
}

public static class MatchSeeds
{
    public static int FromMatchId(string matchId)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in matchId)
                hash = hash * 31 + ch;
            return Math.Abs(hash);
        }
    }

    public static int Mix(int seed, int salt) =>
        unchecked(seed * 31 + salt);
}
