using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Systems;

var world = SampleWorldFactory.Create();
var loop = new GameLoop(world);
var tools = new GameToolSurface(world);
var crimeSystem = new CrimeSystem();

Console.WriteLine("TTS: Technology Tier Simulation");
Console.WriteLine("================================");
PrintWorldSummary(world, tools);

const int turnsToSimulate = 8;
for (var i = 0; i < turnsToSimulate; i++)
{
    var result = loop.RunTurn();
    Console.WriteLine();
    Console.WriteLine($"--- Turn {result.Turn} ---");

    foreach (var (civilization, outcome) in result.Outcomes)
    {
        Console.WriteLine(
            $"{civilization.Name}: TTS {(int)civilization.CurrentTier}, " +
            $"stability {civilization.AverageStability:F1}, " +
            $"techs {civilization.ResearchedTechnologyIds.Count}, " +
            $"policy {civilization.Policy.Research}");

        if (civilization.CurrentTier >= TechTier.InformationAge)
        {
            var crime = crimeSystem.GetPerspective(civilization, world);
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

Console.WriteLine();
Console.WriteLine("Agent tool surface (MAF integration point):");
var player = world.Civilizations.First(c => c.IsPlayerControlled);
var snapshot = tools.GetCivilizationState(player.Id);
Console.WriteLine($"  {snapshot.Name} @ TTS {(int)snapshot.CurrentTier} — agent tools ready from TTS 5+");

static void PrintWorldSummary(WorldState world, GameToolSurface tools)
{
    foreach (var civilization in world.Civilizations)
    {
        var snapshot = tools.GetCivilizationState(civilization.Id);
        Console.WriteLine(
            $"{snapshot.Name} ({snapshot.Id}) — TTS {(int)snapshot.CurrentTier}, policy {civilization.Policy.Research}");
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
