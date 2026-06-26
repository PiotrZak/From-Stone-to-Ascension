namespace TTS.Api.Services;

using System.Text.Json;
using TTS.Api.Models;
using TTS.Core.Models;
using TTS.Core.Simulation;

public sealed class MatchRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly object _lock = new();
    private MatchRegistryDocument _document;

    private static readonly string[] SlotIds = ["civ-player", "civ-rival"];

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

    public MatchRegistryEntry Create(string modeId, string modeDisplayName, int? worldSeed = null)
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
                CreatedAt = DateTimeOffset.UtcNow,
                WorldSeed = worldSeed ?? MatchSeeds.FromMatchId(matchId)
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
            var slotIndex = Array.FindIndex(SlotIds, id => !taken.Contains(id));
            if (slotIndex < 0)
                throw new InvalidOperationException("Match is full.");

            var civId = SlotIds[slotIndex];
            var seed = match.WorldSeed;
            var civName = WorldNameGenerator.CivilizationName(seed, slotIndex);

            var player = new MatchPlayerEntry
            {
                PlayerId = Guid.NewGuid().ToString("N")[..8],
                PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Governor" : playerName.Trim(),
                CivilizationId = civId,
                CivilizationName = civName,
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

    /// <summary>Re-add registry entries for match saves found on disk after restart.</summary>
    public void ReconcileWithSavedMatches()
    {
        lock (_lock)
        {
            var directory = MatchSavePaths.ResolveDirectory();
            if (!Directory.Exists(directory))
                return;

            var changed = false;
            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
            {
                var matchId = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(matchId) || GetById(matchId) is not null)
                    continue;

                var modeId = "dev-blitz-3m";
                var worldSeed = MatchSeeds.FromMatchId(matchId);
                try
                {
                    var doc = new MatchPersistence().Load(file);
                    if (!string.IsNullOrWhiteSpace(doc.Match?.ModeId))
                        modeId = doc.Match.ModeId;
                    if (doc.Match?.WorldSeed > 0)
                        worldSeed = doc.Match.WorldSeed;
                }
                catch
                {
                    // Keep defaults for corrupted saves; grain activation may still fail gracefully.
                }

                var config = MatchPresets.Resolve(modeId);
                _document.Matches.Add(new MatchRegistryEntry
                {
                    MatchId = matchId,
                    JoinCode = matchId.Length >= 6 ? matchId[^6..].ToUpperInvariant() : matchId.ToUpperInvariant(),
                    ModeId = modeId,
                    ModeDisplayName = config.DisplayName,
                    CreatedAt = File.GetCreationTimeUtc(file),
                    WorldSeed = worldSeed
                });
                changed = true;
            }

            if (changed)
                Save();
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
        var tempPath = _path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(_document, JsonOptions));
        File.Move(tempPath, _path, overwrite: true);
    }
}
