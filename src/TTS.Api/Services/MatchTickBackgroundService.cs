namespace TTS.Api.Services;

using TTS.Contracts;
using TTS.Core.Models;
using TTS.Core.Simulation;

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
                    if (!MatchSavePaths.SaveExists(match.MatchId))
                        continue;

                    if (MatchSavePaths.TryReadMatchStatus(match.MatchId, out var status)
                        && status != MatchStatus.Running)
                        continue;

                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeout.CancelAfter(TimeSpan.FromSeconds(15));

                    try
                    {
                        var result = await orleans.GetGrain(match.MatchId)
                            .AdvanceTickIfDueAsync()
                            .WaitAsync(timeout.Token);

                        if (result.Outcome == GrainTickOutcomeKind.Completed)
                            logger.LogInformation("Auto-tick completed for match {MatchId} turn {Turn}", match.MatchId, result.Turn);
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning("Auto-tick timed out for match {MatchId}", match.MatchId);
                    }
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
