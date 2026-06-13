namespace TTS.Core.Systems;

using System.Text.Json;
using System.Text.Json.Serialization;
using TTS.Core.Models;

/// <summary>Loads per-TTS technology sub-trees from JSON (see data/tech/catalog.json).</summary>
public sealed class TechTreeCatalog
{
    private static TechTreeCatalog? _default;
    private readonly List<Technology> _technologies;
    private readonly bool _isLoaded;

    public TechTreeCatalog(string? jsonPath = null)
    {
        var path = jsonPath ?? ResolveDefaultPath();
        _isLoaded = File.Exists(path);
        _technologies = _isLoaded ? LoadFromFile(path) : [];
    }

    public static TechTreeCatalog Default => _default ??= new TechTreeCatalog();

    public bool IsLoaded => _isLoaded;

    public IReadOnlyList<Technology> Technologies => _technologies;

    public IEnumerable<Technology> GetForTier(TechTier tier) =>
        _technologies.Where(t => t.Tier == tier);

    public Technology? GetById(string id) =>
        _technologies.FirstOrDefault(t => t.Id == id);

    public static string ResolveDefaultPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "tech", "catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "data", "tech", "catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "tech", "catalog.json")
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static List<Technology> LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<TechCatalogDocument>(json, JsonOptions)
            ?? throw new InvalidDataException($"Failed to parse tech catalog: {path}");

        return document.Nodes.Select(ToTechnology).ToList();
    }

    private static Technology ToTechnology(TechNodeDefinition node)
    {
        if (!Enum.TryParse<TechTier>(node.Tier.ToString(), out var tier))
            throw new InvalidDataException($"Unknown tier value {node.Tier} for node {node.Id}.");

        if (!Enum.TryParse<TechCategory>(node.Category, ignoreCase: true, out var category))
            throw new InvalidDataException($"Unknown category '{node.Category}' for node {node.Id}.");

        var role = Enum.TryParse<TechNodeRole>(node.Role, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : TechNodeRole.Branch;

        return new Technology(
            node.Id,
            node.Name,
            tier,
            category,
            node.Prerequisites,
            node.RiskLevel,
            node.IsForbidden,
            node.FusionTags,
            role);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private sealed class TechCatalogDocument
    {
        [JsonPropertyName("nodes")]
        public List<TechNodeDefinition> Nodes { get; init; } = [];
    }

    private sealed class TechNodeDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = "";

        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("tier")]
        public int Tier { get; init; }

        [JsonPropertyName("category")]
        public string Category { get; init; } = "";

        [JsonPropertyName("role")]
        public string Role { get; init; } = "branch";

        [JsonPropertyName("prerequisites")]
        public List<string> Prerequisites { get; init; } = [];

        [JsonPropertyName("risk_level")]
        public int RiskLevel { get; init; }

        [JsonPropertyName("is_forbidden")]
        public bool IsForbidden { get; init; }

        [JsonPropertyName("fusion_tags")]
        public List<string> FusionTags { get; init; } = [];
    }
}
