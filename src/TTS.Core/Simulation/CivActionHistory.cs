namespace TTS.Core.Simulation;

using TTS.Core.Models;

public static class CivActionHistory
{
    public static string? GetLastAction(string civilizationId, IReadOnlyList<TurnSnapshot> history)
    {
        foreach (var snapshot in history.OrderByDescending(s => s.Turn))
        {
            if (!snapshot.ResearchedThisTurn.TryGetValue(civilizationId, out var techs) || techs.Count == 0)
                continue;

            return techs.Count == 1
                ? $"Researched {techs[0]}"
                : $"Researched {techs[^1]} (+{techs.Count - 1} more)";
        }

        return null;
    }
}
