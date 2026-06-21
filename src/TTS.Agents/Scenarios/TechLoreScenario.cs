namespace TTS.Agents.Scenarios;

using TTS.Llm;
using TTS.Core.Models;

public sealed class TechLoreScenario(OllamaClient ollama) : IScenario
{
    public string Id => "tech-lore";
    public string Title => "Tech Fusion Lore";
    public string Description => "Generates flavor text for a fusion technology node.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var parents = new[]
        {
            new Technology("tech-agi", "Artificial General Intelligence", TechTier.EarlyAI, TechCategory.ArtificialIntelligence, fusionTags: ["ai"]),
            new Technology("tech-genome", "Genome Editing", TechTier.BioNano, TechCategory.Biology, fusionTags: ["biology"])
        };

        var userPrompt = $"""
            Generate a fusion technology for a civilization game (TTS 5-6).

            Parent A: {parents[0].Name} (tags: {string.Join(", ", parents[0].FusionTags)})
            Parent B: {parents[1].Name} (tags: {string.Join(", ", parents[1].FusionTags)})

            Output:
            NAME: <technology name>
            TIER: TTS 6
            RISK: <0-100>
            DESCRIPTION: <2 sentences in-game flavor>
            EVENT_HOOK: <one possible crisis or bonus event>
            """;

        Console.WriteLine("Prompting Ollama tech generator...\n");
        var reply = await ollama.ChatAsync(
            "You design technologies for a sci-fi civilization game. Stay within AI + biology fusion theme.",
            userPrompt,
            cancellationToken);

        Console.WriteLine(reply);
    }
}
