namespace TTS.Core.Models;

/// <summary>Match preset — wall-clock schedule and victory rules (Phase 4).</summary>
public sealed class MatchConfig
{
    public required string ModeId { get; init; }
    public required string DisplayName { get; init; }
    public int MaxTicks { get; init; }
    public TimeSpan TickInterval { get; init; }
    public TimeSpan DecisionWindow { get; init; }
    public int MinPlayers { get; init; }
    public int MaxPlayers { get; init; }
    public TechTier VictoryTier { get; init; }
    public double VictoryStabilityMin { get; init; }
    public bool EnableForbiddenTechGates { get; init; }
}

public enum MatchStatus
{
    Lobby,
    Running,
    Ended
}

public sealed class MatchState
{
    public string MatchId { get; }
    public MatchConfig Config { get; }
    public MatchStatus Status { get; set; }
    public int TickCount { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastTickAt { get; set; }
    public DateTimeOffset NextTickAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public MatchState(string matchId, MatchConfig config, DateTimeOffset startedAt)
    {
        MatchId = matchId;
        Config = config;
        Status = MatchStatus.Lobby;
        StartedAt = startedAt;
        LastTickAt = startedAt;
        NextTickAt = startedAt + config.TickInterval;
    }
}

public static class MatchPresets
{
    public static MatchConfig Sprint8h => new()
    {
        ModeId = "sprint-8h",
        DisplayName = "Sprint (8h)",
        MaxTicks = 8,
        TickInterval = TimeSpan.FromHours(1),
        DecisionWindow = TimeSpan.FromHours(2),
        MinPlayers = 2,
        MaxPlayers = 4,
        VictoryTier = TechTier.EarlyAI,
        VictoryStabilityMin = 55,
        EnableForbiddenTechGates = false
    };

    public static MatchConfig Blitz24h => new()
    {
        ModeId = "blitz-24h",
        DisplayName = "Blitz (24h)",
        MaxTicks = 6,
        TickInterval = TimeSpan.FromHours(4),
        DecisionWindow = TimeSpan.FromHours(8),
        MinPlayers = 2,
        MaxPlayers = 6,
        VictoryTier = TechTier.BioNano,
        VictoryStabilityMin = 50,
        EnableForbiddenTechGates = true
    };

    public static MatchConfig Standard36h => new()
    {
        ModeId = "standard-36h",
        DisplayName = "Standard (36h)",
        MaxTicks = 12,
        TickInterval = TimeSpan.FromHours(3),
        DecisionWindow = TimeSpan.FromHours(12),
        MinPlayers = 2,
        MaxPlayers = 8,
        VictoryTier = TechTier.BioNano,
        VictoryStabilityMin = 50,
        EnableForbiddenTechGates = true
    };

    public static MatchConfig Extended48h => new()
    {
        ModeId = "extended-48h",
        DisplayName = "Extended (48h)",
        MaxTicks = 12,
        TickInterval = TimeSpan.FromHours(4),
        DecisionWindow = TimeSpan.FromHours(24),
        MinPlayers = 2,
        MaxPlayers = 8,
        VictoryTier = TechTier.Temporal,
        VictoryStabilityMin = 45,
        EnableForbiddenTechGates = true
    };

    /// <summary>Dev-only: 6 ticks × 30s ≈ 3 minutes wall-clock.</summary>
    public static MatchConfig DevBlitz3m => new()
    {
        ModeId = "dev-blitz-3m",
        DisplayName = "Dev Blitz (3m)",
        MaxTicks = 6,
        TickInterval = TimeSpan.FromSeconds(30),
        DecisionWindow = TimeSpan.FromMinutes(2),
        MinPlayers = 1,
        MaxPlayers = 2,
        VictoryTier = TechTier.EarlyElectronics,
        VictoryStabilityMin = 50,
        EnableForbiddenTechGates = false
    };

    public static MatchConfig Resolve(string modeId) => modeId.ToLowerInvariant() switch
    {
        "sprint-8h" or "sprint" => Sprint8h,
        "blitz-24h" or "blitz" => Blitz24h,
        "standard-36h" or "standard" => Standard36h,
        "extended-48h" or "extended" => Extended48h,
        "dev-blitz-3m" or "dev-blitz" or "dev" => DevBlitz3m,
        _ => throw new ArgumentException($"Unknown match mode '{modeId}'.")
    };

    public static IReadOnlyList<MatchConfig> All => [Sprint8h, Blitz24h, Standard36h, Extended48h, DevBlitz3m];
}
