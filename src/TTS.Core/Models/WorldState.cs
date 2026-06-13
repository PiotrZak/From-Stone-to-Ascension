namespace TTS.Core.Models;

/// <summary>Aggregate simulation state passed to systems and agent tools.</summary>
public class WorldState
{
    public int Turn { get; set; } = 1;
    public List<Civilization> Civilizations { get; } = [];
    public List<Region> Regions { get; } = [];
    public List<Technology> Technologies { get; } = [];
    public List<KnowledgeNetwork> KnowledgeNetworks { get; } = [];
    public List<GlobalEvent> ActiveEvents { get; } = [];
    public DateTimeOffset SimulatedNow { get; set; } = DateTimeOffset.UtcNow;
    public MatchState? Match { get; set; }
}

/// <summary>Global event affecting one or more civilizations or regions.</summary>
public class GlobalEvent
{
    public string Id { get; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TechTier MinimumTier { get; set; }
    public int Severity { get; set; }
    public int RemainingTurns { get; set; }

    public GlobalEvent(string id, string name, string description, TechTier minimumTier, int severity, int duration)
    {
        Id = id;
        Name = name;
        Description = description;
        MinimumTier = minimumTier;
        Severity = severity;
        RemainingTurns = duration;
    }
}
