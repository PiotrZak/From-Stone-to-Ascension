namespace TTS.Llm.Tools;

using System.Text.Json;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>
/// Exposes <see cref="IGameToolSurface"/> as LLM-callable tools. Simulation stays authoritative.
/// </summary>
public sealed class GameToolRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private static readonly IReadOnlyDictionary<GameTool, (string Description, Func<GameToolRegistry, JsonElement, string> Execute)> ToolSpecs =
        new Dictionary<GameTool, (string, Func<GameToolRegistry, JsonElement, string>)>
        {
            [GameTool.GetCivilizationState] = (
                "Read political, economic, and tech stability for a civilization.",
                (r, a) => SerializeState(r._tools.GetCivilizationState(r.GetCivId(a)))),
            [GameTool.GetFactionTensions] = (
                "Read internal faction tension scores.",
                (r, a) => JsonSerializer.Serialize(r._tools.GetFactionTensions(r.GetCivId(a)), JsonOptions)),
            [GameTool.GetAvailableTechnologies] = (
                "List technologies that can be researched next.",
                (r, a) => SerializeTechList(r._tools.GetAvailableTechnologies(r.GetCivId(a)))),
            [GameTool.GetPolicyResearchAnalysis] = (
                "Get policy stance and ranked research candidates.",
                (r, a) => SerializeAnalysis(r._tools.GetPolicyResearchAnalysis(r.GetCivId(a)))),
            [GameTool.GetGlobalEvents] = (
                "List active global events affecting the world.",
                (r, a) => SerializeEvents(r._tools.GetGlobalEvents(GetBool(a, "active_only", true)))),
            [GameTool.GetPendingDecisions] = (
                "List unresolved decision gates blocking research.",
                (r, a) => SerializeGates(r._tools.GetPendingDecisions(r.GetCivId(a)))),
            [GameTool.ProposeResearch] = (
                "Research a technology (validated by simulation). Call once per turn.",
                (r, a) => SerializeProposal(r._tools.ProposeResearch(
                    r.GetCivId(a),
                    GetString(a, "technology_id") ?? throw new ArgumentException("technology_id required")))),
            [GameTool.SetResearchPriority] = (
                "Set branch research weight (TTS 5+).",
                (r, a) => SerializeAction(r._tools.SetResearchPriority(
                    r.GetCivId(a),
                    GetString(a, "branch") ?? "general",
                    GetDouble(a, "weight")))),
            [GameTool.ProposeDiplomaticAction] = (
                "Queue a diplomatic proposal.",
                (r, a) => SerializeAction(r._tools.ProposeDiplomaticAction(
                    r.GetCivId(a),
                    GetString(a, "action") ?? "diplomacy",
                    GetString(a, "target_civilization_id") ?? ""))),
        };

    private readonly IGameToolSurface _tools;
    private readonly string _civilizationId;
    private readonly bool _readOnly;

    public GameToolRegistry(IGameToolSurface tools, string civilizationId, bool readOnly = false)
    {
        _tools = tools;
        _civilizationId = civilizationId;
        _readOnly = readOnly;
    }

    public IReadOnlyList<LlmToolDefinition> GetDefinitions()
    {
        return AvailableTools()
            .Select(tool => new LlmToolDefinition(
                tool,
                ToolSpecs[tool].Description,
                tool.IsReadOnly(),
                SchemaFor(tool)))
            .ToList();
    }

    public IEnumerable<GameTool> AvailableTools()
    {
        foreach (var tool in Enum.GetValues<GameTool>())
        {
            if (_readOnly && !tool.IsReadOnly())
                continue;

            yield return tool;
        }
    }

    public LlmToolCallResult Execute(string toolName, JsonElement arguments) =>
        GameToolExtensions.TryParse(toolName, out var tool)
            ? Execute(tool, arguments)
            : new LlmToolCallResult(false, "{}", $"Unknown tool: {toolName}");

    public LlmToolCallResult Execute(GameTool tool, JsonElement arguments)
    {
        if (_readOnly && !tool.IsReadOnly())
            return new LlmToolCallResult(false, "{}", $"Tool '{tool.ToApiName()}' is not available in read-only mode.");

        try
        {
            var json = ToolSpecs[tool].Execute(this, arguments);
            return new LlmToolCallResult(true, json);
        }
        catch (Exception ex)
        {
            return new LlmToolCallResult(false, "{}", ex.Message);
        }
    }

    private string GetCivId(JsonElement args) =>
        GetString(args, "civilization_id") ?? _civilizationId;

    private static JsonElement SchemaFor(GameTool tool) => tool switch
    {
        GameTool.GetCivilizationState => Obj(("civilization_id", Str("Civilization id"))),
        GameTool.GetFactionTensions => Obj(("civilization_id", Str("Civilization id"))),
        GameTool.GetAvailableTechnologies => Obj(("civilization_id", Str("Civilization id"))),
        GameTool.GetPolicyResearchAnalysis => Obj(("civilization_id", Str("Civilization id"))),
        GameTool.GetGlobalEvents => Obj(("active_only", Bool("Filter to active events only"))),
        GameTool.GetPendingDecisions => Obj(("civilization_id", Str("Civilization id"))),
        GameTool.ProposeResearch => Obj(
            ("civilization_id", Str("Civilization id")),
            ("technology_id", Str("Technology id from get_available_technologies"))),
        GameTool.SetResearchPriority => Obj(
            ("civilization_id", Str("Civilization id")),
            ("branch", Str("Branch key e.g. ai, computing")),
            ("weight", Num("Priority weight 0–1"))),
        GameTool.ProposeDiplomaticAction => Obj(
            ("civilization_id", Str("Civilization id")),
            ("action", Str("Action name")),
            ("target_civilization_id", Str("Target civ id"))),
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
    };

    private static string SerializeState(CivilizationStateSnapshot s) =>
        JsonSerializer.Serialize(new
        {
            s.Id,
            s.Name,
            tier = (int)s.CurrentTier,
            s.PoliticalStability,
            s.EconomicStability,
            s.TechnologicalStability,
            s.ResearchedTechnologyIds,
            s.ControlledRegionIds
        }, JsonOptions);

    private static string SerializeTechList(IReadOnlyList<Technology> techs) =>
        JsonSerializer.Serialize(techs.Select(t => new
        {
            t.Id,
            t.Name,
            tier = (int)t.Tier,
            t.RiskLevel,
            t.IsForbidden,
            role = t.Role.ToString()
        }), JsonOptions);

    private static string SerializeAnalysis(PolicyResearchAnalysis a) =>
        JsonSerializer.Serialize(new
        {
            research_stance = a.ResearchStance.ToString(),
            risk = a.RiskTolerance.ToString(),
            recommended = a.Recommended is { } r
                ? new { r.TechnologyId, r.Name, tier = (int)r.Tier, r.Branch, r.TotalScore }
                : null,
            candidates = a.RankedCandidates.Take(8).Select(c => new
            {
                c.TechnologyId,
                c.Name,
                tier = (int)c.Tier,
                c.Branch,
                c.TotalScore,
                c.AllowedByRisk
            })
        }, JsonOptions);

    private static string SerializeEvents(IReadOnlyList<GlobalEvent> events) =>
        JsonSerializer.Serialize(events.Select(e => new
        {
            e.Id,
            e.Name,
            e.Description,
            e.Severity,
            e.RemainingTurns
        }), JsonOptions);

    private static string SerializeGates(IReadOnlyList<DecisionGate> gates) =>
        JsonSerializer.Serialize(gates.Select(g => new
        {
            g.Id,
            type = g.Type.ToString(),
            g.Title,
            default_option = g.DefaultOptionId,
            options = g.Options.Select(o => o.Id)
        }), JsonOptions);

    private static string SerializeProposal(ProposeResearchResult r) =>
        JsonSerializer.Serialize(new { accepted = r.Accepted, r.Message, technology_id = r.TechnologyId }, JsonOptions);

    private static string SerializeAction(ActionResult r) =>
        JsonSerializer.Serialize(new { accepted = r.Accepted, r.Message }, JsonOptions);

    private static JsonElement Obj(params (string key, JsonElement value)[] props)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "object");
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            foreach (var (key, value) in props)
            {
                writer.WritePropertyName(key);
                value.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    private static JsonElement Str(string description)
    {
        using var doc = JsonDocument.Parse($$"""{"type":"string","description":"{{description.Replace("\"", "\\\"")}}"}""");
        return doc.RootElement.Clone();
    }

    private static JsonElement Bool(string description)
    {
        using var doc = JsonDocument.Parse($$"""{"type":"boolean","description":"{{description.Replace("\"", "\\\"")}}"}""");
        return doc.RootElement.Clone();
    }

    private static JsonElement Num(string description)
    {
        using var doc = JsonDocument.Parse($$"""{"type":"number","description":"{{description.Replace("\"", "\\\"")}}"}""");
        return doc.RootElement.Clone();
    }

    private static string? GetString(JsonElement args, string key) =>
        args.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static bool GetBool(JsonElement args, string key, bool fallback) =>
        args.TryGetProperty(key, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean()
            : fallback;

    private static double GetDouble(JsonElement args, string key) =>
        args.TryGetProperty(key, out var el) && el.TryGetDouble(out var d) ? d : 0.5;
}
