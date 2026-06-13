namespace TTS.Agents.Scenarios;

using TTS.Agents.Ollama;
using TTS.Core.Models;
using TTS.Core.Systems;

public sealed class CrimePerspectiveScenario(OllamaClient ollama) : IScenario
{
    public string Id => "crime";
    public string Title => "TTS 4 Crime Perspective";
    public string Description => "Analyzes regional crime/income data and recommends policy at Information Age.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var (world, tools, _) = ScenarioWorldBuilder.CreateInformationAgeWithCrime();
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var crime = tools.GetCrimePerspective(player.Id);
        var snapshot = tools.GetCivilizationState(player.Id);

        var regionDetails = world.Regions
            .Where(r => r.CrimeProfile is not null)
            .Select(r =>
            {
                var p = r.CrimeProfile!;
                return $"{r.Name} (based on {p.SourceState} {p.DataYear}): violent {p.ViolentCrimeRate:F0}/100k, poverty {p.PovertyRate:F1}%, gini {p.GiniCoefficient:F2}, GDP/cap ${p.GdpPerCapita:F0}, pressure {p.CrimePressureIndex:F1}";
            });

        var userPrompt = $"""
            TTS 4 Information Age — Crime & socioeconomic perspective.

            Civilization: {snapshot.Name}
            Tier: TTS {(int)snapshot.CurrentTier}
            Political stability: {snapshot.PoliticalStability:F0}
            Economic stability: {snapshot.EconomicStability:F0}
            Average crime pressure: {crime.AverageCrimePressure:F1}/100
            Cybersecurity researched: {player.ResearchedTechnologyIds.Contains(CrimeSystem.CybersecurityTechId)}

            Regional data (from state_crime_income_merged.csv):
            {string.Join("\n", regionDetails)}

            As a governance advisor, explain:
            1. How crime and inequality threaten stability at TTS 4
            2. Whether to prioritize Cybersecurity Systems or social programs
            3. One concrete policy recommendation
            """;

        Console.WriteLine("Prompting Ollama crime perspective...\n");
        var reply = await ollama.ChatAsync(
            "You analyze crime, poverty, and inequality in a digital-age civilization strategy game.",
            userPrompt,
            cancellationToken);

        Console.WriteLine(reply);
    }
}
