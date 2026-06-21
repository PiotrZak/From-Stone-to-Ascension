namespace TTS.Api.Services;

using TTS.Contracts;

/// <summary>Falls back when grains are inactive; primary scheduling is grain timers on WorldGrain.</summary>
public sealed class MatchTickBackgroundService(
    MatchRegistry registry,
    OrleansMatchService orleans,
    ILogger<MatchTickBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var match in registry.List())
                {
                    var result = await orleans.GetGrain(match.MatchId).AdvanceTickIfDueAsync();
                    if (result.Outcome == GrainTickOutcomeKind.Completed)
                        logger.LogInformation("Auto-tick completed for match {MatchId} turn {Turn}", match.MatchId, result.Turn);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Auto-tick loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
