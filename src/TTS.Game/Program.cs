using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Game;

var services = new SimulationServices();
var world = SampleWorldFactory.Create(withDemoGate: true);
var loop = services.CreateGameLoop(world);
var tools = services.CreateToolSurface(world);
var player = world.Civilizations.First(c => c.IsPlayerControlled);

Console.WriteLine("TTS: Technology Tier Simulation");
Console.WriteLine("================================");
PrintWorldSummary(world, tools);
SimulationReporter.PrintTechCatalog(world.Technologies, tools);

const int turnsToSimulate = 8;
for (var i = 0; i < turnsToSimulate; i++)
{
    if (i == 1)
    {
        var pending = tools.GetPendingDecisions(player.Id);
        if (pending.Count > 0)
        {
            var gate = pending[0];
            var resolved = tools.ResolveDecision(player.Id, gate.Id, "invest");
            Console.WriteLine($"  Resolved gate '{gate.Title}' → {resolved.OptionId}");
        }
    }

    var result = loop.RunTurn();
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

if (world.Turn > 2)
{
    Console.WriteLine();
    Console.WriteLine(tools.GetAwaySummary(1, world.Turn - 1).Format(world));
}

Console.WriteLine();
Console.WriteLine("Agent tool surface (MAF integration point):");
var snapshot = tools.GetCivilizationState(player.Id);
var analysis = tools.GetPolicyResearchAnalysis(player.Id);
Console.WriteLine($"  {snapshot.Name} @ TTS {(int)snapshot.CurrentTier} — agent tools ready from TTS 5+");
if (analysis.Recommended is { } next)
{
    Console.WriteLine(
        $"  Next policy pick: {next.Name} ({next.Category}→{next.Branch}, score {next.TotalScore:F1})");
}

static void PrintWorldSummary(WorldState world, GameToolSurface tools)
{
    foreach (var civilization in world.Civilizations)
    {
        var snapshot = tools.GetCivilizationState(civilization.Id);
        Console.WriteLine($"{snapshot.Name} ({snapshot.Id}) — TTS {(int)snapshot.CurrentTier}");
        SimulationReporter.PrintPolicyBranches(civilization, tools);
    }

    Console.WriteLine($"Technologies loaded: {world.Technologies.Count}");
    Console.WriteLine($"Knowledge links: {world.KnowledgeNetworks.Count}");

    foreach (var region in world.Regions.Where(r => r.CrimeProfile is not null))
    {
        var p = region.CrimeProfile!;
        Console.WriteLine(
            $"Crime data [{region.Name}]: {p.SourceState} {p.DataYear} — pressure {p.CrimePressureIndex:F1} (TTS 4+)");
    }
}
