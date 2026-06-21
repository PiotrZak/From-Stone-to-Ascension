namespace TTS.Core.Models;

/// <summary>High-impact moment requiring player choice (async MP Phase 3).</summary>
public enum GateType
{
    TierAdvancement,
    GlobalCrisis,
    ForbiddenTech,
    FactionCrisis,
    CrimePressure,
    AiAlignment
}

public sealed class DecisionOption
{
    public string Id { get; }
    public string Label { get; }
    public string Description { get; }

    public DecisionOption(string id, string label, string description)
    {
        Id = id;
        Label = label;
        Description = description;
    }
}

public sealed class DecisionGate
{
    public string Id { get; }
    public string CivilizationId { get; }
    public GateType Type { get; }
    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<DecisionOption> Options { get; }
    public string DefaultOptionId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public string? ContextTechnologyId { get; }
    public string? ContextEventId { get; }
    public bool IsResolved { get; set; }
    public string? ResolvedOptionId { get; set; }
    public bool WasAutoResolved { get; set; }

    /// <summary>LLM-generated tier fable (optional).</summary>
    public string? Fable { get; set; }

    public string DisplayText => string.IsNullOrWhiteSpace(Fable) ? Description : Fable;

    public DecisionGate(
        string id,
        string civilizationId,
        GateType type,
        string title,
        string description,
        IEnumerable<DecisionOption> options,
        string defaultOptionId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? contextTechnologyId = null,
        string? contextEventId = null)
    {
        Id = id;
        CivilizationId = civilizationId;
        Type = type;
        Title = title;
        Description = description;
        Options = options.ToList();
        DefaultOptionId = defaultOptionId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        ContextTechnologyId = contextTechnologyId;
        ContextEventId = contextEventId;
    }
}
