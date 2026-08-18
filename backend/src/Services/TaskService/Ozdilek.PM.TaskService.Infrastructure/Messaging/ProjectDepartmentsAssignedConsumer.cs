using MassTransit;
using Microsoft.Extensions.Logging;
using Ozdilek.PM.Contracts.Events;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.TaskService.Application.Interfaces;
using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Infrastructure.Messaging;

/// <summary>
/// Reacts to a project's department/work-package rows (published by ProjectService right after project
/// creation) by creating one real TaskGroup per row — Title = the row's work-package title, Subtitle =
/// the department name. This gives <see cref="WorkPackageApprovedConsumer"/> an actual matching group to
/// route AI-approved suggestions into, instead of always falling back to the generic bucket group.
/// </summary>
public sealed class ProjectDepartmentsAssignedConsumer(
    ITaskGroupRepository groups,
    IUnitOfWork unitOfWork,
    ILogger<ProjectDepartmentsAssignedConsumer> logger) : IConsumer<ProjectDepartmentsAssignedEvent>
{
    public async Task Consume(ConsumeContext<ProjectDepartmentsAssignedEvent> context)
    {
        var message = context.Message;

        // No-tracking read, just to check which titles already exist for this project (message
        // redelivery safety) — same rationale as WorkPackageApprovedConsumer.
        var existingGroups = await groups.ListByProjectAsync(message.ProjectId, context.CancellationToken);
        var existingTitles = existingGroups.Select(g => g.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;
        foreach (var department in message.Departments)
        {
            if (existingTitles.Contains(department.Title))
            {
                continue;
            }

            var group = TaskGroup.Create(message.ProjectId, department.Title, department.DepartmentName);
            await groups.AddAsync(group, context.CancellationToken);
            existingTitles.Add(department.Title);
            created++;
        }

        if (created > 0)
        {
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
        }

        logger.LogInformation(
            "ProjectDepartmentsAssignedEvent processed for project {ProjectId}: {Created} task group(s) created.",
            message.ProjectId, created);
    }
}
