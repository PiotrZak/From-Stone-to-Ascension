namespace TTS.Api.Services;

public sealed class MatchRecoveryHostedService(
    MatchRegistry registry,
    ILogger<MatchRecoveryHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        registry.ReconcileWithSavedMatches();
        logger.LogInformation("Match registry reconciled with on-disk saves");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
