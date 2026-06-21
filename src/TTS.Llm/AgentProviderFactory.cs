namespace TTS.Llm;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;
using TTS.Llm.Agents;

/// <summary>Wires Ollama tool agents into the simulation turn loop with classical fallback.</summary>
public sealed class OllamaTurnAgent : ILlmTurnAgent, IDisposable
{
    private readonly AgentProviderSettings _settings;
    private readonly OllamaClient _client;
    private readonly CivilizationTurnAgent _turnAgent;

    public OllamaTurnAgent(AgentProviderSettings? settings = null, OllamaClient? client = null)
    {
        _settings = settings ?? AgentProviderSettings.FromEnvironment();
        _client = client ?? new OllamaClient();
        _turnAgent = new CivilizationTurnAgent(_client);
    }

    public bool IsEnabled =>
        _settings.AgentsEnabled
        && string.Equals(_settings.Provider, "ollama", StringComparison.OrdinalIgnoreCase);

    public AgentTurnResult? TryRunTurn(
        Civilization civilization,
        IGameToolSurface tools,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || (int)civilization.CurrentTier < (int)TechTier.EarlyAI)
            return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var result = _turnAgent.RunAsync(civilization, tools, cts.Token).GetAwaiter().GetResult();
            if (!result.Success)
                return null;

            if (result.ResearchedTechnologyId is { } techId)
            {
                var tech = tools.GetTechnologyDetail(techId);
                var evaluation = new ResearchCandidateEvaluation(
                    tech.Id,
                    tech.Name,
                    tech.Category,
                    tech.Branch,
                    tech.FusionTags,
                    tech.Tier,
                    tech.RiskLevel,
                    tech.IsForbidden,
                    AllowedByRisk: true,
                    BranchWeightScore: 0,
                    StanceBonus: 0,
                    TotalScore: 0);

                return AgentTurnResult.Completed(
                    $"LLM agent researched '{tech.Name}'.",
                    techId,
                    evaluation);
            }

            return string.IsNullOrWhiteSpace(result.Message)
                ? null
                : AgentTurnResult.Completed(result.Message);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _client.Dispose();
}

public static class AgentProviderFactory
{
    public static ILlmTurnAgent? CreateTurnAgent()
    {
        var settings = AgentProviderSettings.FromEnvironment();
        if (!settings.AgentsEnabled)
            return null;

        return settings.Provider.ToLowerInvariant() switch
        {
            "ollama" => new OllamaTurnAgent(settings),
            "none" => null,
            _ => null
        };
    }
}
