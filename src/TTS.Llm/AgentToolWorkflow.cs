namespace TTS.Llm;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Llm.Tools;

/// <summary>Microsoft Agent Framework workflow over game tools.</summary>
public sealed class AgentToolWorkflow : IAgentWorkflow, IDisposable
{
    private readonly AgentProviderSettings _settings;
    private readonly AgentLlmSettings _llmSettings;
    private OpenAIClient? _client;

    public AgentToolWorkflow(AgentProviderSettings? settings = null, AgentLlmSettings? llmSettings = null)
    {
        _settings = settings ?? AgentProviderSettings.FromEnvironment();
        _llmSettings = llmSettings ?? AgentLlmSettings.FromEnvironment(_settings.Provider);
    }

    public bool IsConfigured => _settings.AgentsEnabled && _llmSettings.IsConfigured;

    public Task<AgentSessionResult> RunCivilizationTurnAsync(
        Civilization civilization,
        IGameToolSurface tools,
        IReadOnlyList<RivalSummary> rivals,
        AgentSessionLimits limits,
        CancellationToken cancellationToken = default) =>
        RunSessionAsync(
            civilization,
            tools,
            limits,
            readOnly: false,
            TurnSystemPrompt,
            BuildTurnUserPrompt(civilization, rivals),
            cancellationToken);

    public Task<string?> RunAdvisorBriefingAsync(
        string civilizationId,
        IGameToolSurface tools,
        AgentSessionLimits limits,
        CancellationToken cancellationToken = default) =>
        RunAdvisorAsync(civilizationId, tools, limits, cancellationToken);

    private async Task<AgentSessionResult> RunSessionAsync(
        Civilization civilization,
        IGameToolSurface tools,
        AgentSessionLimits limits,
        bool readOnly,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return AgentSessionResult.Unavailable("LLM provider is not configured.");

        try
        {
            var registry = new GameToolRegistry(tools, civilization.Id, readOnly);
            var bridge = new AgentGameToolBridge(registry);
            var agent = CreateAgent(systemPrompt, bridge, limits);
            var response = await agent.RunAsync(userPrompt, cancellationToken: cancellationToken);

            return new AgentSessionResult(
                true,
                response.Text ?? "",
                bridge.LastResearchId,
                bridge.ToolLog,
                bridge.DiplomaticActions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AgentSessionResult.Unavailable(ex.Message);
        }
    }

    private async Task<string?> RunAdvisorAsync(
        string civilizationId,
        IGameToolSurface tools,
        AgentSessionLimits limits,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return null;

        try
        {
            var registry = new GameToolRegistry(tools, civilizationId, readOnly: true);
            var bridge = new AgentGameToolBridge(registry);
            var agent = CreateAgent(AdvisorSystemPrompt, bridge, limits);
            var response = await agent.RunAsync(
                $"Brief the governor of civilization {civilizationId}. Inspect current threats, policy fit, and the best research path.",
                cancellationToken: cancellationToken);
            return string.IsNullOrWhiteSpace(response.Text) ? null : response.Text;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private ChatClientAgent CreateAgent(string instructions, AgentGameToolBridge bridge, AgentSessionLimits limits)
    {
        var chatClient = GetClient().GetChatClient(_llmSettings.Model);
        var tools = bridge.BuildTools(limits);

        return chatClient.AsAIAgent(
            instructions,
            tools: tools,
            clientFactory: client => new FunctionInvokingChatClient(client)
            {
                MaximumIterationsPerRequest = limits.MaxToolRounds,
            }.AsBuilder().Build());
    }

    private OpenAIClient GetClient() =>
        _client ??= new OpenAIClient(
            new ApiKeyCredential(_llmSettings.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(_llmSettings.BaseUrl) });

    public void Dispose() => _client = null;

    private static string TurnSystemPrompt => """
        You are the autonomous governor of an AI civilization in TTS (Technology Tier Simulation).
        Run a full turn in two phases using tools only — never invent ids.

        Phase 1 — Diplomacy (optional): inspect faction tensions and rivals, then call propose_diplomatic_action
        at most once (e.g. propose_trade, intelligence_sharing, non_aggression) toward one rival id.

        Phase 2 — Research (required): call get_available_technologies and get_policy_research_analysis,
        then call propose_research exactly once with the best technology id for your policy stance.

        Finish with one short plain-text summary of diplomacy + research choices.
        """;

    private static string AdvisorSystemPrompt => """
        You are an in-world strategic advisor for a civilization tech simulation (TTS 5+).
        Use read-only tools first, especially get_pending_decisions.

        If a decision gate is pending, the briefing MUST center on that gate:
        - Name the gate crisis in your headline.
        - Compare each gate option against current stability and policy.
        - Recommend exactly one gate option by id in a line: "Gate choice: <option_id>".
        - Explain why other options are weaker for this civ right now.

        If no gate is pending, focus on policy, research, and stability.

        Format:
        Line 1: headline (gate title if pending).
        Lines 2-4: bullet highlights starting with "• ".
        Final paragraph: narrative rationale.
        Last line: "Gate choice: <option_id>" OR "Recommend: <research action>" when no gate.

        Do not call write tools.
        """;

    private static string BuildTurnUserPrompt(Civilization civilization, IReadOnlyList<RivalSummary> rivals)
    {
        var rivalBlock = rivals.Count == 0
            ? "No known rival civilizations."
            : string.Join("\n", rivals.Select(r =>
                $"- {r.Name} ({r.Id}) · TTS {r.Tier} · stability {r.Stability:F0}"));

        return $"""
            Civilization: {civilization.Name} ({civilization.Id})
            Policy: {civilization.Policy.Research}, risk {civilization.Policy.Risk}
            Current tier: TTS {(int)civilization.CurrentTier}

            Known rivals:
            {rivalBlock}

            Execute phase 1 (optional diplomacy) then phase 2 (research).
            """;
    }
}
