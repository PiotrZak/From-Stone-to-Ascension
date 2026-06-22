namespace TTS.Llm.Agents;

using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Llm;

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
