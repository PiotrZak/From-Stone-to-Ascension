namespace TTS.Api.Services;

using System.Text.Json;
using TTS.Api.Models;
using TTS.Core.Models;

public sealed class MatchRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly object _lock = new();
    private MatchRegistryDocument _document;

    private static readonly (string CivId, string CivName)[] Slots =
    [
        ("civ-player", "Aurora Collective"),
        ("civ-rival", "Iron Dominion")
    ];

    public MatchRegistry()
    {
        _path = Path.Combine(AppContext.BaseDirectory, "Data", "match-registry.json");
        _document = Load();
    }

    public IReadOnlyList<MatchRegistryEntry> List() =>
        _document.Matches.OrderByDescending(m => m.CreatedAt).ToList();

    public MatchRegistryEntry? GetById(string matchId) =>
        _document.Matches.FirstOrDefault(m => m.MatchId == matchId);

    public MatchRegistryEntry? GetByJoinCode(string joinCode) =>
        _document.Matches.FirstOrDefault(m =>
            m.JoinCode.Equals(joinCode, StringComparison.OrdinalIgnoreCase));

    public MatchRegistryEntry Create(string modeId, string modeDisplayName)
    {
        lock (_lock)
        {
            var matchId = $"match-{Guid.NewGuid():N}"[..10];
            var joinCode = matchId[^6..].ToUpperInvariant();
            var entry = new MatchRegistryEntry
            {
                MatchId = matchId,
                JoinCode = joinCode,
                ModeId = modeId,
                ModeDisplayName = modeDisplayName,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _document.Matches.Add(entry);
            Save();
            return entry;
        }
    }

    public MatchPlayerEntry Join(string matchId, string playerName)
    {
        lock (_lock)
        {
            var match = GetById(matchId)
                ?? throw new KeyNotFoundException($"Match '{matchId}' not found.");

            var config = MatchPresets.Resolve(match.ModeId);
            if (match.Players.Count >= config.MaxPlayers)
                throw new InvalidOperationException("Match is full.");

            var taken = match.Players.Select(p => p.CivilizationId).ToHashSet();
            var slot = Slots.FirstOrDefault(s => !taken.Contains(s.CivId));
            if (slot == default)
                throw new InvalidOperationException("Match is full.");

            var player = new MatchPlayerEntry
            {
                PlayerId = Guid.NewGuid().ToString("N")[..8],
                PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Governor" : playerName.Trim(),
                CivilizationId = slot.CivId,
                CivilizationName = slot.CivName,
                JoinedAt = DateTimeOffset.UtcNow,
                IsReady = false
            };

            if (match.HostPlayerId is null)
                match.HostPlayerId = player.PlayerId;

            match.Players.Add(player);
            Save();
            return player;
        }
    }

    public MatchPlayerEntry SetReady(string matchId, string playerId, bool ready)
    {
        lock (_lock)
        {
            var match = GetById(matchId)
                ?? throw new KeyNotFoundException($"Match '{matchId}' not found.");

            var player = match.Players.FirstOrDefault(p => p.PlayerId == playerId)
                ?? throw new KeyNotFoundException($"Player '{playerId}' not in match.");

            player.IsReady = ready;
            Save();
            return player;
        }
    }

    public void ValidateStart(string matchId, string playerId)
    {
        lock (_lock)
        {
            var match = GetById(matchId)
                ?? throw new KeyNotFoundException($"Match '{matchId}' not found.");

            if (match.HostPlayerId != playerId)
                throw new InvalidOperationException("Only the host can start the match.");

            var config = MatchPresets.Resolve(match.ModeId);
            var readyCount = match.Players.Count(p => p.IsReady);
            if (readyCount < config.MinPlayers)
                throw new InvalidOperationException(
                    $"Need at least {config.MinPlayers} ready player(s) ({readyCount} ready).");
        }
    }

    private MatchRegistryDocument Load()
    {
        if (!File.Exists(_path))
            return new MatchRegistryDocument();

        var json = File.ReadAllText(_path);
        var doc = JsonSerializer.Deserialize<MatchRegistryDocument>(json) ?? new MatchRegistryDocument();
        foreach (var match in doc.Matches.Where(m => m.HostPlayerId is null && m.Players.Count > 0))
            match.HostPlayerId = match.Players[0].PlayerId;

        return doc;
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(_document, JsonOptions));
    }
}
