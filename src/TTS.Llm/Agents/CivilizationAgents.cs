namespace TTS.Llm.Agents;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;
using TTS.Llm.Tools;

/// <summary>TTS 5+ AI civilization turn via LLM tool loop (research + diplomacy).</summary>
public sealed class CivilizationTurnAgent
{
    private readonly OllamaClient _client;

    public CivilizationTurnAgent(OllamaClient? client = null) => _client = client ?? new OllamaClient();

    public async Task<LlmAgentSessionResult> RunAsync(
        Civilization civilization,
        IGameToolSurface tools,
        CancellationToken cancellationToken = default)
    {
        var registry = new GameToolRegistry(tools, civilization.Id, readOnly: false);
        var system = """
            You are the autonomous governor of an AI civilization in TTS (Technology Tier Simulation).
            Use tools to inspect state, then call propose_research exactly once with the best technology id.
            Respect the civilization policy stance. Never invent technology ids — only use get_available_technologies.
            After researching, briefly summarize your choice in plain text.
            """;

        var user = $"""
            Civilization: {civilization.Name} ({civilization.Id})
            Policy: {civilization.Policy.Research}, risk {civilization.Policy.Risk}
            Current tier: TTS {(int)civilization.CurrentTier}
            Run your turn: inspect factions and candidates, then propose_research.
            """;

        var session = new LlmAgentSession(_client, registry, system);
        return await session.RunAsync(user, cancellationToken);
    }
}

/// <summary>Read-only strategic advisor (TTS 5+ player briefing).</summary>
public sealed class AdvisorAgent
{
    private readonly OllamaClient _client;

    public AdvisorAgent(OllamaClient? client = null) => _client = client ?? new OllamaClient();

    public async Task<string?> GetBriefingAsync(
        string civilizationId,
        IGameToolSurface tools,
        CancellationToken cancellationToken = default)
    {
        var registry = new GameToolRegistry(tools, civilizationId, readOnly: true);
        var system = """
            You are an in-world strategic advisor for a civilization tech simulation (TTS 5+).
            Use read-only tools to inspect state before advising. Be concise: 3-5 sentences plus one concrete recommendation.
            Do not propose research directly — recommend only.
            """;

        var user = $"""
            Brief the governor of civilization {civilizationId}.
            Call get_civilization_state, get_faction_tensions, get_policy_research_analysis, and get_pending_decisions first.
            """;

        var session = new LlmAgentSession(_client, registry, system);
        var result = await session.RunAsync(user, cancellationToken);
        return result.Success ? result.Message : null;
    }
}

/// <summary>Crisis narrator for alignment / global events — structured player-facing text.</summary>
public sealed class CrisisWorkflowAgent
{
    private readonly OllamaClient _client;

    public CrisisWorkflowAgent(OllamaClient? client = null) => _client = client ?? new OllamaClient();

    public async Task<string?> NarrateAsync(
        string civilizationName,
        GlobalEvent crisis,
        CivilizationStateSnapshot civState,
        CancellationToken cancellationToken = default)
    {
        var user = $"""
            Crisis event: {crisis.Name}
            Description: {crisis.Description}
            Severity: {crisis.Severity}/5
            Civilization: {civilizationName} at TTS {(int)civState.CurrentTier}
            Stability — political {civState.PoliticalStability:F0}, economic {civState.EconomicStability:F0}, tech {civState.TechnologicalStability:F0}

            Write:
            1. A dramatic 2-3 sentence briefing
            2. Three choices A, B, C with one-sentence stability impact each
            """;

        return await _client.TryChatAsync(
            "You narrate sci-fi civilization crises. Output clear structured choices for a strategy game.",
            user,
            cancellationToken);
    }
}
