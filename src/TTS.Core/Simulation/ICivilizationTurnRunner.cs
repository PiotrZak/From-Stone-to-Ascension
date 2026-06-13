namespace TTS.Core.Simulation;

using TTS.Core.Models;
using TTS.Core.Systems;

public interface ICivilizationTurnRunner
{
    string RunnerId { get; }
    bool CanHandle(Civilization civilization, WorldState world);
    CivilizationTurnResult Run(Civilization civilization, WorldState world);
}

public readonly record struct CivilizationTurnResult(
    bool Acted,
    string Message,
    string? TechnologyId = null,
    ResearchCandidateEvaluation? Evaluation = null)
{
    public static CivilizationTurnResult Completed(
        string message,
        string? technologyId = null,
        ResearchCandidateEvaluation? evaluation = null) =>
        new(true, message, technologyId, evaluation);

    public static CivilizationTurnResult Skipped(string message) =>
        new(false, message);
}
