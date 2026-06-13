namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Classical AI for autonomous civilization turns (all tiers below TTS 5 agent mode).
/// </summary>
public class ClassicalAiSystem
{
    private readonly AutoPolicySystem _autoPolicy = new();
    private readonly TechTreeSystem _techTreeSystem = new();
    private readonly ForbiddenTechSystem _forbiddenTechSystem = new();

    public ClassicalAiTurnResult RunTurn(Civilization civilization, WorldState world)
    {
        var next = _autoPolicy.SelectNextTechnology(civilization, world, civilization.Policy);
        if (next is null)
            return ClassicalAiTurnResult.Skipped("No research candidates match current policy.");

        var result = _techTreeSystem.Research(civilization, next, _forbiddenTechSystem);
        return result.Success
            ? ClassicalAiTurnResult.Completed(next.Name, next.Id)
            : ClassicalAiTurnResult.Skipped(result.Message);
    }
}

public readonly record struct ClassicalAiTurnResult(bool DidResearch, string Message, string? TechnologyId = null)
{
    public static ClassicalAiTurnResult Completed(string name, string id) =>
        new(true, $"Researched '{name}'.", id);

    public static ClassicalAiTurnResult Skipped(string message) =>
        new(false, message);
}
