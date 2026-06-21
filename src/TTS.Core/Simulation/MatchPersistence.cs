namespace TTS.Core.Simulation;

using System.Text.Json;
using System.Text.Json.Serialization;
using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>JSON save/load for local scheduled matches (Phase 4).</summary>
public sealed class MatchPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string DefaultSavePath =>
        Path.Combine(Directory.GetCurrentDirectory(), "match-state.json");

    public bool Exists(string? path = null) => File.Exists(path ?? DefaultSavePath);

    public SavedMatchDocument Load(string? path = null)
    {
        path ??= DefaultSavePath;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SavedMatchDocument>(json, JsonOptions)
            ?? throw new InvalidDataException($"Failed to parse match save: {path}");
    }

    public void Save(WorldState world, IReadOnlyList<TurnSnapshot> turnHistory, string? path = null)
    {
        path ??= DefaultSavePath;
        var doc = ToDocument(world, turnHistory);
        var json = JsonSerializer.Serialize(doc, JsonOptions);
        File.WriteAllText(path, json);
    }

    public WorldState RestoreWorld(SavedMatchDocument doc)
    {
        var world = new WorldState
        {
            Turn = doc.Turn,
            SimulatedNow = doc.SimulatedNow
        };

        if (doc.Match is not null)
        {
            var config = MatchPresets.Resolve(doc.Match.ModeId);
            world.Match = new MatchState(doc.Match.MatchId, config, doc.Match.StartedAt)
            {
                Status = doc.Match.Status,
                TickCount = doc.Match.TickCount,
                LastTickAt = doc.Match.LastTickAt,
                NextTickAt = doc.Match.NextTickAt,
                EndedAt = doc.Match.EndedAt
            };
        }

        var catalog = TechTreeCatalog.Default;
        world.Technologies.AddRange(
            catalog.IsLoaded && catalog.Technologies.Count > 0
                ? catalog.Technologies
                : SampleWorldFactory.CreateFallbackTechnologiesOnly());

        foreach (var saved in doc.Civilizations)
        {
            var civ = new Civilization(saved.Id, saved.Name, saved.IsPlayerControlled)
            {
                CurrentTier = saved.CurrentTier,
                PoliticalStability = saved.PoliticalStability,
                EconomicStability = saved.EconomicStability,
                TechnologicalStability = saved.TechnologicalStability,
                Policy = saved.Policy
            };

            foreach (var techId in saved.ResearchedTechnologyIds)
                civ.ResearchedTechnologyIds.Add(techId);
            foreach (var regionId in saved.ControlledRegionIds)
                civ.ControlledRegionIds.Add(regionId);
            foreach (var bannedId in saved.BannedTechnologyIds)
                civ.BannedTechnologyIds.Add(bannedId);
            foreach (var key in saved.OfferedGateKeys)
                civ.OfferedGateKeys.Add(key);

            foreach (var faction in saved.Factions)
            {
                civ.Factions.Add(new Faction(
                    faction.Id, faction.Name, faction.CivilizationId, faction.Type, faction.Stance)
                {
                    Influence = faction.Influence
                });
            }

            foreach (var gate in saved.PendingDecisions)
            {
                civ.PendingDecisions.Add(new DecisionGate(
                    gate.Id,
                    gate.CivilizationId,
                    gate.Type,
                    gate.Title,
                    gate.Description,
                    gate.Options.Select(o => new DecisionOption(o.Id, o.Label, o.Description)),
                    gate.DefaultOptionId,
                    gate.CreatedAt,
                    gate.ExpiresAt,
                    gate.ContextTechnologyId,
                    gate.ContextEventId)
                {
                    IsResolved = gate.IsResolved,
                    ResolvedOptionId = gate.ResolvedOptionId,
                    WasAutoResolved = gate.WasAutoResolved,
                    Fable = gate.Fable
                });
            }

            world.Civilizations.Add(civ);
        }

        foreach (var saved in doc.Regions)
        {
            var region = new Region(saved.Id, saved.Name)
            {
                Population = saved.Population,
                Resources = saved.Resources,
                Infrastructure = saved.Infrastructure,
                ControllingCivilizationId = saved.ControllingCivilizationId,
                CrimeProfile = saved.CrimeProfile
            };
            world.Regions.Add(region);
        }

        foreach (var saved in doc.KnowledgeNetworks)
        {
            var link = new KnowledgeNetwork(saved.SourceCivilizationId, saved.TargetCivilizationId, saved.Channel)
            {
                Strength = saved.Strength
            };
            foreach (var techId in saved.KnownTechnologyIds)
                link.KnownTechnologyIds.Add(techId);
            world.KnowledgeNetworks.Add(link);
        }

        foreach (var saved in doc.ActiveEvents)
        {
            world.ActiveEvents.Add(new GlobalEvent(
                saved.Id, saved.Name, saved.Description, saved.MinimumTier, saved.Severity, saved.RemainingTurns));
        }

        return world;
    }

    public IReadOnlyList<TurnSnapshot> RestoreTurnHistory(SavedMatchDocument doc) =>
        doc.TurnHistory.Select(ToTurnSnapshot).ToList();

    private static SavedMatchDocument ToDocument(WorldState world, IReadOnlyList<TurnSnapshot> turnHistory) =>
        new()
        {
            Version = 1,
            Turn = world.Turn,
            SimulatedNow = world.SimulatedNow,
            Match = world.Match is null ? null : new SavedMatchState
            {
                MatchId = world.Match.MatchId,
                ModeId = world.Match.Config.ModeId,
                Status = world.Match.Status,
                TickCount = world.Match.TickCount,
                StartedAt = world.Match.StartedAt,
                LastTickAt = world.Match.LastTickAt,
                NextTickAt = world.Match.NextTickAt,
                EndedAt = world.Match.EndedAt
            },
            Civilizations = world.Civilizations.Select(ToSavedCivilization).ToList(),
            Regions = world.Regions.Select(ToSavedRegion).ToList(),
            KnowledgeNetworks = world.KnowledgeNetworks.Select(ToSavedKnowledgeNetwork).ToList(),
            ActiveEvents = world.ActiveEvents.Select(ToSavedEvent).ToList(),
            TurnHistory = turnHistory.Select(ToSavedTurnSnapshot).ToList()
        };

    private static SavedCivilization ToSavedCivilization(Civilization civ) => new()
    {
        Id = civ.Id,
        Name = civ.Name,
        IsPlayerControlled = civ.IsPlayerControlled,
        CurrentTier = civ.CurrentTier,
        PoliticalStability = civ.PoliticalStability,
        EconomicStability = civ.EconomicStability,
        TechnologicalStability = civ.TechnologicalStability,
        Policy = civ.Policy,
        ResearchedTechnologyIds = civ.ResearchedTechnologyIds.ToList(),
        ControlledRegionIds = civ.ControlledRegionIds.ToList(),
        BannedTechnologyIds = civ.BannedTechnologyIds.ToList(),
        OfferedGateKeys = civ.OfferedGateKeys.ToList(),
        Factions = civ.Factions.Select(f => new SavedFaction
        {
            Id = f.Id,
            Name = f.Name,
            CivilizationId = f.CivilizationId,
            Type = f.Type,
            Stance = f.Stance,
            Influence = f.Influence
        }).ToList(),
        PendingDecisions = civ.PendingDecisions.Select(ToSavedGate).ToList()
    };

    private static SavedRegion ToSavedRegion(Region region) => new()
    {
        Id = region.Id,
        Name = region.Name,
        Population = region.Population,
        Resources = region.Resources,
        Infrastructure = region.Infrastructure,
        ControllingCivilizationId = region.ControllingCivilizationId,
        CrimeProfile = region.CrimeProfile
    };

    private static SavedKnowledgeNetwork ToSavedKnowledgeNetwork(KnowledgeNetwork link) => new()
    {
        SourceCivilizationId = link.SourceCivilizationId,
        TargetCivilizationId = link.TargetCivilizationId,
        Channel = link.Channel,
        Strength = link.Strength,
        KnownTechnologyIds = link.KnownTechnologyIds.ToList()
    };

    private static SavedGlobalEvent ToSavedEvent(GlobalEvent e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        MinimumTier = e.MinimumTier,
        Severity = e.Severity,
        RemainingTurns = e.RemainingTurns
    };

    private static SavedDecisionGate ToSavedGate(DecisionGate gate) => new()
    {
        Id = gate.Id,
        CivilizationId = gate.CivilizationId,
        Type = gate.Type,
        Title = gate.Title,
        Description = gate.Description,
        Options = gate.Options.Select(o => new SavedDecisionOption
        {
            Id = o.Id,
            Label = o.Label,
            Description = o.Description
        }).ToList(),
        DefaultOptionId = gate.DefaultOptionId,
        CreatedAt = gate.CreatedAt,
        ExpiresAt = gate.ExpiresAt,
        ContextTechnologyId = gate.ContextTechnologyId,
        ContextEventId = gate.ContextEventId,
        IsResolved = gate.IsResolved,
        ResolvedOptionId = gate.ResolvedOptionId,
        WasAutoResolved = gate.WasAutoResolved,
        Fable = gate.Fable
    };

    private static SavedTurnSnapshot ToSavedTurnSnapshot(TurnSnapshot snapshot) => new()
    {
        Turn = snapshot.Turn,
        SimulatedAt = snapshot.SimulatedAt,
        CivilizationsAtStart = snapshot.CivilizationsAtStart.ToDictionary(
            kvp => kvp.Key,
            kvp => new SavedCivTurnStart
            {
                Tier = kvp.Value.Tier,
                AverageStability = kvp.Value.AverageStability,
                ResearchedCount = kvp.Value.ResearchedCount,
                ResearchedTechnologyIds = kvp.Value.ResearchedTechnologyIds.ToList()
            }),
        GateResolutions = snapshot.GateResolutions.ToList(),
        NewEvents = snapshot.NewEvents.ToList(),
        ResearchedThisTurn = snapshot.ResearchedThisTurn.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList()),
        TierChanges = snapshot.TierChanges.ToDictionary(
            kvp => kvp.Key,
            kvp => new SavedTierChange { From = kvp.Value.From, To = kvp.Value.To })
    };

    private static TurnSnapshot ToTurnSnapshot(SavedTurnSnapshot saved)
    {
        var snapshot = new TurnSnapshot
        {
            Turn = saved.Turn,
            SimulatedAt = saved.SimulatedAt
        };

        foreach (var (civId, start) in saved.CivilizationsAtStart)
        {
            snapshot.CivilizationsAtStart[civId] = new CivTurnStartSnapshot(
                start.Tier,
                start.AverageStability,
                start.ResearchedCount,
                start.ResearchedTechnologyIds);
        }

        snapshot.GateResolutions.AddRange(saved.GateResolutions);
        snapshot.NewEvents.AddRange(saved.NewEvents);

        foreach (var (civId, techs) in saved.ResearchedThisTurn)
            snapshot.ResearchedThisTurn[civId] = techs;

        foreach (var (civId, change) in saved.TierChanges)
            snapshot.TierChanges[civId] = new TierChangeRecord(change.From, change.To);

        return snapshot;
    }

    public sealed class SavedMatchDocument
    {
        public int Version { get; set; } = 1;
        public int Turn { get; set; }
        public DateTimeOffset SimulatedNow { get; set; }
        public SavedMatchState? Match { get; set; }
        public List<SavedCivilization> Civilizations { get; set; } = [];
        public List<SavedRegion> Regions { get; set; } = [];
        public List<SavedKnowledgeNetwork> KnowledgeNetworks { get; set; } = [];
        public List<SavedGlobalEvent> ActiveEvents { get; set; } = [];
        public List<SavedTurnSnapshot> TurnHistory { get; set; } = [];
    }

    public sealed class SavedMatchState
    {
        public string MatchId { get; set; } = "";
        public string ModeId { get; set; } = "sprint-8h";
        public MatchStatus Status { get; set; }
        public int TickCount { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset LastTickAt { get; set; }
        public DateTimeOffset NextTickAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
    }

    public sealed class SavedCivilization
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsPlayerControlled { get; set; }
        public TechTier CurrentTier { get; set; }
        public double PoliticalStability { get; set; }
        public double EconomicStability { get; set; }
        public double TechnologicalStability { get; set; }
        public CivilizationPolicy Policy { get; set; } = CivilizationPolicy.Balanced();
        public List<string> ResearchedTechnologyIds { get; set; } = [];
        public List<string> ControlledRegionIds { get; set; } = [];
        public List<string> BannedTechnologyIds { get; set; } = [];
        public List<string> OfferedGateKeys { get; set; } = [];
        public List<SavedFaction> Factions { get; set; } = [];
        public List<SavedDecisionGate> PendingDecisions { get; set; } = [];
    }

    public sealed class SavedFaction
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string CivilizationId { get; set; } = "";
        public FactionType Type { get; set; }
        public FactionStance Stance { get; set; }
        public double Influence { get; set; }
    }

    public sealed class SavedRegion
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public long Population { get; set; }
        public double Resources { get; set; }
        public double Infrastructure { get; set; }
        public string? ControllingCivilizationId { get; set; }
        public RegionalCrimeProfile? CrimeProfile { get; set; }
    }

    public sealed class SavedKnowledgeNetwork
    {
        public string SourceCivilizationId { get; set; } = "";
        public string TargetCivilizationId { get; set; } = "";
        public DiffusionChannel Channel { get; set; }
        public double Strength { get; set; }
        public List<string> KnownTechnologyIds { get; set; } = [];
    }

    public sealed class SavedGlobalEvent
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public TechTier MinimumTier { get; set; }
        public int Severity { get; set; }
        public int RemainingTurns { get; set; }
    }

    public sealed class SavedDecisionGate
    {
        public string Id { get; set; } = "";
        public string CivilizationId { get; set; } = "";
        public GateType Type { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<SavedDecisionOption> Options { get; set; } = [];
        public string DefaultOptionId { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string? ContextTechnologyId { get; set; }
        public string? ContextEventId { get; set; }
        public bool IsResolved { get; set; }
        public string? ResolvedOptionId { get; set; }
        public bool WasAutoResolved { get; set; }
        public string? Fable { get; set; }
    }

    public sealed class SavedDecisionOption
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public sealed class SavedTurnSnapshot
    {
        public int Turn { get; set; }
        public DateTimeOffset SimulatedAt { get; set; }
        public Dictionary<string, SavedCivTurnStart> CivilizationsAtStart { get; set; } = new();
        public List<GateResolutionRecord> GateResolutions { get; set; } = [];
        public List<string> NewEvents { get; set; } = [];
        public Dictionary<string, List<string>> ResearchedThisTurn { get; set; } = new();
        public Dictionary<string, SavedTierChange> TierChanges { get; set; } = new();
    }

    public sealed class SavedCivTurnStart
    {
        public TechTier Tier { get; set; }
        public double AverageStability { get; set; }
        public int ResearchedCount { get; set; }
        public List<string> ResearchedTechnologyIds { get; set; } = [];
    }

    public sealed class SavedTierChange
    {
        public TechTier From { get; set; }
        public TechTier To { get; set; }
    }
}
