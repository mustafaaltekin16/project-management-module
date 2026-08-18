using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ozdilek.PM.ProjectService.Application.Services;

/// <summary>
/// Nightly safety net for <see cref="ProjectProgressInputsChangedConsumer"/>: recomputes progress/deviation
/// for every non-terminal project, catching any drift caused by a missed/failed event (a message that
/// never arrived, a transient TaskService/FeasibilityService outage at the time it was processed, etc.).
/// The event-driven path is expected to keep things fresh in near-real-time; this job exists purely so a
/// single missed event can't leave a project's numbers stale forever, mirroring the old (much weaker)
/// guarantee — the Angular Detail Page's "self-heals on every visit" comment — without depending on anyone
/// actually visiting that page.
/// </summary>
public sealed class ProjectProgressRecomputeJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ProjectProgressRecomputeJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var progressAppService = scope.ServiceProvider.GetRequiredService<ProjectProgressAppService>();
                await progressAppService.RecomputeAllActiveAsync(stoppingToken);
                logger.LogInformation("Gece proje ilerleme/sapma yeniden hesaplaması tamamlandı.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Gece proje ilerleme/sapma yeniden hesaplaması başarısız oldu.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
