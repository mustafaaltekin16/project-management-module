using MassTransit;
using Microsoft.Extensions.Logging;
using Ozdilek.PM.Contracts.Events;
using Ozdilek.PM.ProjectService.Application.Services;

namespace Ozdilek.PM.ProjectService.Infrastructure.Messaging;

/// <summary>
/// Reacts to a task or feasibility-item change (published by TaskService/FeasibilityService — see
/// <see cref="ProjectProgressInputsChangedEvent"/>) by re-deriving the project's ProgressPercent/
/// DeviationDays from current data. This is what makes progress/deviation stay correct without depending
/// on a specific browser tab being open — the previous design computed these client-side in the Angular
/// Detail Page and only refreshed them as a side effect of visiting that exact page.
/// </summary>
public sealed class ProjectProgressInputsChangedConsumer(
    ProjectProgressAppService progressAppService,
    ILogger<ProjectProgressInputsChangedConsumer> logger) : IConsumer<ProjectProgressInputsChangedEvent>
{
    public async Task Consume(ConsumeContext<ProjectProgressInputsChangedEvent> context)
    {
        var message = context.Message;
        await progressAppService.RecomputeProgressAsync(message.ProjectId, context.CancellationToken);

        logger.LogInformation(
            "ProjectProgressInputsChangedEvent processed for project {ProjectId}: progress recomputed.",
            message.ProjectId);
    }
}
