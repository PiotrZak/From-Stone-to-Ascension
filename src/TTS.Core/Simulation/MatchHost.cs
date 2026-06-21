namespace TTS.Core.Simulation;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Local scheduled match runner — load/save, tick on wall clock (Phase 4).</summary>
public sealed class MatchHost
{
    private readonly MatchPersistence _persistence;
    private readonly TickScheduler _scheduler = new();

    public WorldState World { get; }
    public SimulationServices Services { get; }
    public GameLoop Loop { get; }
    public string SavePath { get; }

    private MatchHost(WorldState world, SimulationServices services, string savePath)
    {
        World = world;
        Services = services;
        Loop = services.CreateGameLoop(world);
        SavePath = savePath;
        _persistence = new MatchPersistence();
    }

    public static MatchHost CreateNew(
        MatchConfig config,
        string savePath,
        bool withDemoGate = false,
        DateTimeOffset? startedAt = null,
        ILlmTurnAgent? llmTurnAgent = null)
    {
        var world = SampleWorldFactory.Create(config, withDemoGate);
        var services = new SimulationServices { LlmTurnAgent = llmTurnAgent };
        RestoreTurnHistory(services, []);
        return new MatchHost(world, services, savePath);
    }

    public static MatchHost Load(string savePath, ILlmTurnAgent? llmTurnAgent = null)
    {
        var persistence = new MatchPersistence();
        var doc = persistence.Load(savePath);
        var world = persistence.RestoreWorld(doc);
        var services = new SimulationServices { LlmTurnAgent = llmTurnAgent };
        RestoreTurnHistory(services, persistence.RestoreTurnHistory(doc));
        return new MatchHost(world, services, savePath);
    }

    public static MatchHost LoadOrCreate(
        string savePath,
        MatchConfig config,
        bool forceNew = false,
        bool withDemoGate = false)
    {
        var persistence = new MatchPersistence();
        if (!forceNew && persistence.Exists(savePath))
            return Load(savePath);

        return CreateNew(config, savePath, withDemoGate);
    }

    public MatchStatusInfo GetStatus(DateTimeOffset? now = null)
    {
        now ??= DateTimeOffset.UtcNow;
        var match = World.Match
            ?? throw new InvalidOperationException("World has no match configuration.");

        var pending = World.Civilizations
            .SelectMany(c => c.PendingDecisions.Where(g => !g.IsResolved).Select(g => (c, g)))
            .Select(x => new PendingGateInfo(
                x.c.Id,
                x.c.Name,
                x.g.Id,
                x.g.Type,
                x.g.Title,
                x.g.ExpiresAt,
                x.g.DefaultOptionId))
            .ToList();

        return new MatchStatusInfo(
            match.MatchId,
            match.Config.DisplayName,
            match.Status,
            match.TickCount,
            match.Config.MaxTicks,
            match.StartedAt,
            match.LastTickAt,
            match.NextTickAt,
            World.SimulatedNow,
            _scheduler.ShouldTick(match, now.Value)
                || (match.TickCount == 0 && match.Status == MatchStatus.Running),
            pending);
    }

    public MatchTickResult TryRunDueTick(DateTimeOffset now)
    {
        var match = World.Match
            ?? throw new InvalidOperationException("World has no match configuration.");

        if (match.Status == MatchStatus.Ended)
            return new MatchTickResult(MatchTickOutcome.MatchEnded, Message: "Match has ended.");

        var due = _scheduler.ShouldTick(match, now)
            || (match.TickCount == 0 && match.Status == MatchStatus.Running);

        if (!due)
            return new MatchTickResult(MatchTickOutcome.NotDue, Message: $"Next tick at {match.NextTickAt:u}.");

        match.LastTickAt = now;
        match.NextTickAt = now + match.Config.TickInterval;

        var result = Loop.RunTurn(now);

        if (match.TickCount >= match.Config.MaxTicks)
        {
            match.Status = MatchStatus.Ended;
            match.EndedAt = now;
            Services.DecisionGates.ExpireGates(World, Services);
            Save();
            return new MatchTickResult(MatchTickOutcome.MatchEnded, result, "Match ended.");
        }

        Save();
        return new MatchTickResult(MatchTickOutcome.Completed, result);
    }

    public IReadOnlyList<MatchResultEntry> GetResults() =>
        new MatchResultsBuilder().Build(World, Services);

    public string GetResultsSummary()
    {
        var match = World.Match;
        var mode = match?.Config.DisplayName ?? "Match";
        return new MatchResultsBuilder().Format(GetResults(), mode);
    }

    public IReadOnlyList<MatchTickLogEntry> GetTickLog() =>
        MatchLogBuilder.Build(World, Services.TurnHistory);

    public IReadOnlyList<TurnResult> RunInstantTicks(int count)
    {
        var results = new List<TurnResult>();
        for (var i = 0; i < count; i++)
        {
            if (World.Match is { Status: MatchStatus.Ended })
                break;

            var now = World.SimulatedNow + (World.Match?.Config.TickInterval ?? TimeSpan.FromHours(1));
            if (World.Match is { } match)
            {
                match.LastTickAt = now;
                match.NextTickAt = now + match.Config.TickInterval;
            }

            results.Add(Loop.RunTurn(now));

            if (World.Match is { } m && m.TickCount >= m.Config.MaxTicks)
            {
                m.Status = MatchStatus.Ended;
                m.EndedAt = now;
                break;
            }
        }

        return results;
    }

    public void Save() => _persistence.Save(World, Services.TurnHistory, SavePath);

    public GateResolutionResult ResolveDecision(string civilizationId, string gateId, string optionId)
    {
        var civ = World.Civilizations.FirstOrDefault(c => c.Id == civilizationId)
            ?? throw new KeyNotFoundException($"Civilization '{civilizationId}' not found.");

        var result = Services.DecisionGates.Resolve(World, civ, gateId, optionId, autoResolved: false, Services);
        if (result.Success)
            Save();

        return result;
    }

    public void UpdatePolicy(string civilizationId, string presetId)
    {
        var civ = World.Civilizations.FirstOrDefault(c => c.Id == civilizationId)
            ?? throw new KeyNotFoundException($"Civilization '{civilizationId}' not found.");

        civ.Policy = PolicyPresets.Resolve(presetId);
        Save();
    }

    public void StartMatch(DateTimeOffset now)
    {
        var match = World.Match
            ?? throw new InvalidOperationException("World has no match configuration.");

        if (match.Status != MatchStatus.Lobby)
            throw new InvalidOperationException($"Match cannot start from status '{match.Status}'.");

        _scheduler.StartMatch(match, now);
        World.SimulatedNow = now;
        Save();
    }

    public IGameToolSurface CreateToolSurface() => Services.CreateToolSurface(World);

    private static void RestoreTurnHistory(SimulationServices services, IReadOnlyList<TurnSnapshot> history)
    {
        services.TurnHistory.Clear();
        services.TurnHistory.AddRange(history);
    }
}
