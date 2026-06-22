namespace TTS.Llm;

using System.Text.Json;
using Microsoft.Extensions.AI;
using TTS.Core.Agents;
using TTS.Llm.Tools;

/// <summary>Maps <see cref="GameToolRegistry"/> tools to MAF <see cref="AIFunction"/> instances.</summary>
internal sealed class AgentGameToolBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly GameToolRegistry _registry;
    private readonly List<string> _toolLog = [];
    private readonly List<string> _diplomaticActions = [];
    private int _toolCallCount;

    public AgentGameToolBridge(GameToolRegistry registry) => _registry = registry;

    public string? LastResearchId { get; private set; }

    public IReadOnlyList<string> ToolLog => _toolLog;

    public IReadOnlyList<string> DiplomaticActions => _diplomaticActions;

    public IList<AITool> BuildTools(AgentSessionLimits limits)
    {
        var tools = new List<AITool>();
        foreach (var def in _registry.GetDefinitions())
        {
            var tool = def.Tool;
            tools.Add(new RegistryGameToolFunction(def, args => Invoke(tool, args, limits)));
        }

        return tools;
    }

    private string Invoke(GameTool tool, AIFunctionArguments args, AgentSessionLimits limits)
    {
        if (++_toolCallCount > limits.MaxToolCallsPerSession)
            return JsonSerializer.Serialize(new { error = "Tool call budget reached." }, JsonOptions);

        _toolLog.Add(tool.ToApiName());

        var arguments = ToJsonElement(args);
        var result = _registry.Execute(tool, arguments);

        if (result.Success)
        {
            if (tool == GameTool.ProposeResearch)
                TryCaptureResearch(result.Json);

            if (tool == GameTool.ProposeDiplomaticAction)
                TryCaptureDiplomacy(arguments);
        }

        return result.Error is null
            ? result.Json
            : JsonSerializer.Serialize(new { error = result.Error, result = result.Json }, JsonOptions);
    }

    private void TryCaptureResearch(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("accepted", out var ok) && ok.GetBoolean()
            && doc.RootElement.TryGetProperty("technology_id", out var tid))
            LastResearchId = tid.GetString();
    }

    private void TryCaptureDiplomacy(JsonElement arguments)
    {
        var target = arguments.TryGetProperty("target_civilization_id", out var t) ? t.GetString() : "?";
        var action = arguments.TryGetProperty("action", out var a) ? a.GetString() : "diplomacy";
        _diplomaticActions.Add($"{action} → {target}");
    }

    private static JsonElement ToJsonElement(AIFunctionArguments args)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var (key, value) in args)
            dict[key] = value;

        return JsonSerializer.SerializeToElement(dict, JsonOptions);
    }

    private sealed class RegistryGameToolFunction : AIFunction
    {
        private readonly AIFunctionDeclaration _declaration;
        private readonly Func<AIFunctionArguments, string> _invoke;

        public RegistryGameToolFunction(LlmToolDefinition def, Func<AIFunctionArguments, string> invoke)
        {
            _declaration = AIFunctionFactory.CreateDeclaration(
                def.Name,
                def.Description,
                def.ParametersSchema,
                returnJsonSchema: null);
            _invoke = invoke;
        }

        public override string Name => _declaration.Name;

        public override string Description => _declaration.Description;

        public override JsonElement JsonSchema => _declaration.JsonSchema;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<object?>(_invoke(arguments));
    }
}
