namespace TTS.Core.Systems;

public sealed record TradeGlobeFilter(
    string? Commodity,
    string? ImportCountry,
    string? TransportMode)
{
    public bool HasAny =>
        !string.IsNullOrWhiteSpace(Commodity)
        || !string.IsNullOrWhiteSpace(ImportCountry)
        || !string.IsNullOrWhiteSpace(TransportMode);
}
