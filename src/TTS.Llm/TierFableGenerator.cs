namespace TTS.Llm;

public sealed class TierFableGenerator(OllamaClient? client = null)
{
    private readonly GateFableGenerator _gates = new(client);

    public Task<string?> GenerateAsync(
        string civilizationName,
        int tier,
        CancellationToken cancellationToken = default) =>
        _gates.GenerateAsync(
            civilizationName,
            TTS.Core.Models.GateType.TierAdvancement,
            $"TTS {tier} unlocked",
            $"Your civilization reached tier {tier}. How do you embrace the new era?",
            tier,
            cancellationToken);
}
