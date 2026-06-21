using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;

namespace TTS.Game;

internal static class MatchConsoleReporter
{
    public static void PrintStatus(MatchStatusInfo status)
    {
        Console.WriteLine($"Match: {status.ModeDisplayName} ({status.MatchId})");
        Console.WriteLine($"Status: {status.Status} — tick {status.TickCount}/{status.MaxTicks}");
        Console.WriteLine($"Simulated: {status.SimulatedNow:u}");
        Console.WriteLine($"Last tick: {status.LastTickAt:u}");
        Console.WriteLine($"Next tick: {status.NextTickAt:u} {(status.IsTickDue ? "(DUE NOW)" : "")}");

        if (status.PendingGates.Count == 0)
        {
            Console.WriteLine("Pending gates: none");
            return;
        }

        Console.WriteLine($"Pending gates: {status.PendingGates.Count}");
        foreach (var gate in status.PendingGates)
        {
            Console.WriteLine(
                $"  [{gate.Type}] {gate.CivilizationName}: {gate.Title} — expires {gate.ExpiresAt:u} (default: {gate.DefaultOptionId})");
        }
    }

    public static void PrintTurn(TurnResult result, WorldState world, GameToolSurface tools)
    {
        Console.WriteLine();
        Console.WriteLine($"--- Turn {result.Turn} ---");

        foreach (var gate in result.ActiveGates)
        {
            Console.WriteLine(
                $"  DECISION GATE [{gate.Type}]: {gate.Title} — expires {gate.ExpiresAt:u} (default: {gate.DefaultOptionId})");
            foreach (var option in gate.Options)
                Console.WriteLine($"    • {option.Id}: {option.Label}");
        }

        foreach (var decision in result.ResearchDecisions)
            SimulationReporter.PrintResearchDecision(decision, tools);

        foreach (var (civilization, outcome) in result.Outcomes)
        {
            Console.WriteLine(
                $"{civilization.Name}: TTS {(int)civilization.CurrentTier}, " +
                $"stability {civilization.AverageStability:F1}, " +
                $"techs {civilization.ResearchedTechnologyIds.Count}, " +
                $"policy {civilization.Policy.Research}");

            SimulationReporter.PrintResearchCandidates(civilization, tools);

            if (civilization.CurrentTier >= TechTier.InformationAge)
            {
                var crime = tools.GetCrimePerspective(civilization.Id);
                if (crime.Available)
                {
                    Console.WriteLine(
                        $"  Crime perspective: pressure {crime.AverageCrimePressure:F1}, " +
                        $"violent {crime.AverageViolentCrimeRate:F0}/100k, poverty {crime.AveragePovertyRate:F1}%");
                }
            }

            if (outcome.IsVictory)
                Console.WriteLine($"  VICTORY: {outcome.Message}");
            else if (outcome.IsDefeat)
                Console.WriteLine($"  DEFEAT: {outcome.Message}");
        }

        if (world.ActiveEvents.Count > 0)
            Console.WriteLine($"Active events: {string.Join(", ", world.ActiveEvents.Select(e => e.Name))}");
    }
}
