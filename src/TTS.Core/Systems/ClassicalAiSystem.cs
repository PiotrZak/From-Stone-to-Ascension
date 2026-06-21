namespace TTS.Core.Systems;

using TTS.Core.Models;
using TTS.Core.Simulation;

/// <summary>
/// Classical AI for autonomous civilization turns (all tiers below TTS 5 agent mode).
/// </summary>
public class ClassicalAiSystem
{
    private readonly SimulationServices _services;

    public ClassicalAiSystem(SimulationServices services) => _services = services;

    public ClassicalAiTurnResult RunTurn(Civilization civilization, WorldState world)
    {
        var slots = ResearchThroughput.SlotsFor(civilization);
        var researchedNames = new List<string>();
        string? lastId = null;
        ResearchCandidateEvaluation? lastEvaluation = null;

        for (var slot = 0; slot < slots; slot++)
        {
            var analysis = _services.AutoPolicy.Analyze(civilization, world, civilization.Policy);
            if (analysis.Recommended is not ResearchCandidateEvaluation recommended)
                break;

            var next = world.Technologies.First(t => t.Id == recommended.TechnologyId);
            var result = _services.Research.Execute(civilization, next, world);
            if (!result.Success)
                break;

            researchedNames.Add(next.Name);
            lastId = next.Id;
            lastEvaluation = recommended;
        }

        if (researchedNames.Count == 0)
            return ClassicalAiTurnResult.Skipped("No research candidates match current policy.");

        var message = researchedNames.Count == 1
            ? $"Researched '{researchedNames[0]}'."
            : $"Researched {string.Join(", ", researchedNames.Select(n => $"'{n}'"))}.";

        return ClassicalAiTurnResult.Completed(message, lastId!, lastEvaluation);
    }
}

public readonly record struct ClassicalAiTurnResult(
    bool DidResearch,
    string Message,
    string? TechnologyId = null,
    ResearchCandidateEvaluation? Evaluation = null)
{
    public static ClassicalAiTurnResult Completed(
        string message,
        string id,
        ResearchCandidateEvaluation? evaluation = null) =>
        new(true, message, id, evaluation);

    public static ClassicalAiTurnResult Skipped(string message) =>
        new(false, message);
}
