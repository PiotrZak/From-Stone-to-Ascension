using TTS.Agents.Ollama;
using TTS.Agents.Scenarios;

using var ollama = new OllamaClient();
IScenario[] scenarios =
[
    new PingScenario(ollama),
    new AdvisorScenario(ollama),
    new CrisisScenario(ollama),
    new RivalTurnScenario(ollama),
    new TechLoreScenario(ollama),
    new FactionDebateScenario(ollama),
    new CrimePerspectiveScenario(ollama)
];

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "list";

if (command is "list" or "help" or "-h" or "--help")
{
    PrintHelp(scenarios);
    return;
}

if (command == "all")
{
    foreach (var scenario in scenarios.Where(s => s.Id != "ping"))
    {
        await RunScenario(scenario);
    }
    return;
}

var selected = scenarios.FirstOrDefault(s => s.Id == command);
if (selected is null)
{
    Console.WriteLine($"Unknown scenario: {command}");
    PrintHelp(scenarios);
    return;
}

await RunScenario(selected);

static async Task RunScenario(IScenario scenario)
{
    Console.WriteLine();
    Console.WriteLine($"═══ {scenario.Title} ({scenario.Id}) ═══");
    Console.WriteLine(scenario.Description);
    Console.WriteLine(new string('─', 50));

    try
    {
        await scenario.RunAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }
}

static void PrintHelp(IEnumerable<IScenario> scenarios)
{
    Console.WriteLine("TTS Ollama Scenarios");
    Console.WriteLine("====================");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run --project src/TTS.Agents -- <scenario>");
    Console.WriteLine();
    Console.WriteLine("Environment (optional):");
    Console.WriteLine("  OLLAMA_BASE_URL  default http://localhost:11434");
    Console.WriteLine("  OLLAMA_MODEL     default llama3.2");
    Console.WriteLine();
    Console.WriteLine("Scenarios:");
    foreach (var s in scenarios)
        Console.WriteLine($"  {s.Id,-16} {s.Title} — {s.Description}");
    Console.WriteLine("  all              Run all scenarios (except ping)");
    Console.WriteLine();
    Console.WriteLine("First time: ollama pull llama3.2");
}
