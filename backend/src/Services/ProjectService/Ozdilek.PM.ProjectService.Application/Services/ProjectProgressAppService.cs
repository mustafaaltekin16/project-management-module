using Microsoft.Extensions.Logging;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Application.Services;

/// <summary>
/// Re-derives a project's ProgressPercent/DeviationDays from CURRENT TaskService/FeasibilityService data
/// (via <see cref="ProjectProgressCalculator"/>) and persists it — the single entry point that replaces
/// the old design where the Angular Detail Page computed these numbers client-side and PUT them back only
/// when someone happened to visit that project's page. Triggered by
/// <see cref="ProjectProgressInputsChangedConsumer"/> (reactive, near-instant) and
/// <see cref="ProjectProgressRecomputeJob"/> (nightly safety net for any missed/failed event) — and,
/// synchronously, whenever the frontend calls the recompute endpoint for instant feedback right after a
/// user action, instead of waiting for the async event round-trip.
/// </summary>
public sealed class ProjectProgressAppService(
    IProjectRepository projects,
    IProjectTaskTimelineClient taskTimeline,
    IProjectFeasibilityTimelineClient feasibilityTimeline,
    IUnitOfWork unitOfWork,
    ILogger<ProjectProgressAppService> logger)
{
    public async Task<ProjectDetailDto> RecomputeProgressAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(projectId, ct) ?? throw new NotFoundException("Proje bulunamadı.");

        // Same guard as the old client-side syncProgress(): a terminal project's progress is frozen —
        // Project.UpdateProgress would throw for Completed/Cancelled anyway, so this is a silent no-op
        // rather than a hard failure (recompute can be triggered by an event that races a project's
        // completion/cancellation without anything having gone wrong).
        if (project.Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
        {
            return ProjectAppService.ToDetailDto(project);
        }

        IReadOnlyList<TimelineTaskGroupData> taskGroups;
        try
        {
            taskGroups = await taskTimeline.ListByProjectAsync(projectId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Proje {ProjectId} için görev verisi alınamadı, ilerleme mevcut görev verisi olmadan hesaplanıyor.", projectId);
            taskGroups = [];
        }

        // Manuel "Aktifleştir" veya pano sürüklemesi beklemeden: bir Taslak projede herhangi bir görev
        // Todo dışında bir duruma (Devam Ediyor/Tamamlandı) geçtiği anda proje fiilen başlamış sayılır.
        if (project.Status == ProjectStatus.Draft && HasStartedWork(taskGroups))
        {
            project.Activate();
        }

        // Feasibility data is only meaningful (and only fetched from FeasibilityService) for
        // FeasibilityBased projects — same gate ProjectProgressCalculator itself applies.
        IReadOnlyList<TimelineFeasibilityGroupData> feasibilityGroups = [];
        if (project.Type == ProjectType.FeasibilityBased)
        {
            try
            {
                feasibilityGroups = await feasibilityTimeline.ListByProjectAsync(projectId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Proje {ProjectId} için fizibilite verisi alınamadı, ilerleme fizibilite verisi olmadan hesaplanıyor.", projectId);
                feasibilityGroups = [];
            }
        }

        var result = ProjectProgressCalculator.Calculate(
            project.Type,
            project.StartDate,
            project.EndDate,
            DateOnly.FromDateTime(DateTime.UtcNow),
            taskGroups,
            feasibilityGroups);

        project.UpdateProgress(result.ProgressPercent, result.DeviationDays);
        await unitOfWork.SaveChangesAsync(ct);
        return ProjectAppService.ToDetailDto(project);
    }

    /// <summary>
    /// Nightly safety net (see <see cref="ProjectProgressRecomputeJob"/>) — catches any project whose
    /// progress drifted because a ProjectProgressInputsChangedEvent was missed/failed to process. Scoped
    /// to Draft/Active only, mirroring RecomputeProgressAsync's terminal-status no-op.
    /// </summary>
    public async Task RecomputeAllActiveAsync(CancellationToken ct = default)
    {
        var activeProjects = await projects.ListAsync(
            p => p.Status == ProjectStatus.Active || p.Status == ProjectStatus.Draft, ct);

        foreach (var project in activeProjects)
        {
            try
            {
                await RecomputeProgressAsync(project.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Proje {ProjectId} için gece yeniden hesaplaması başarısız oldu.", project.Id);
            }
        }
    }

    private static bool HasStartedWork(IReadOnlyList<TimelineTaskGroupData> taskGroups) =>
        taskGroups
            .SelectMany(group => group.Tasks)
            .Any(task => !task.Status.Equals("Todo", StringComparison.OrdinalIgnoreCase));
}
