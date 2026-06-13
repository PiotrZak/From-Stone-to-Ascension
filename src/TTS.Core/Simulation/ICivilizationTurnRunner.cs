namespace TTS.Core.Simulation;

using TTS.Core.Models;

public interface ICivilizationTurnRunner
{
    bool CanHandle(Civilization civilization, WorldState world);
    CivilizationTurnResult Run(Civilization civilization, WorldState world);
}

public readonly record struct CivilizationTurnResult(bool Acted, string Message, string? TechnologyId = null)
{
    public static CivilizationTurnResult Completed(string message, string? technologyId = null) =>
        new(true, message, technologyId);

    public static CivilizationTurnResult Skipped(string message) =>
        new(false, message);
}
