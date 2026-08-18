namespace Ozdilek.PM.ProjectService.Application.Interfaces;

public sealed record TimelineTaskItemData(
    Guid Id,
    string Title,
    string AssigneeName,
    Guid? AssigneeEmployeeId,
    string? Department,
    string Status,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? DueDateUtc);

public sealed record TimelineTaskGroupData(
    Guid Id,
    Guid? WorkPackageId,
    string? ProcessType,
    int TimelineSortOrder,
    string Title,
    string Subtitle,
    IReadOnlyList<TimelineTaskItemData> Tasks);

public sealed record TimelineApprovalStepData(
    string ApproverName,
    int Order,
    string Decision,
    DateTimeOffset? DecidedAtUtc);

public sealed record TimelineFeasibilityItemData(
    string Status,
    IReadOnlyList<TimelineApprovalStepData> Steps);

public sealed record TimelineFeasibilityGroupData(
    Guid Id,
    Guid? WorkPackageId,
    int TimelineSortOrder,
    string Name,
    IReadOnlyList<TimelineFeasibilityItemData> Items);

public interface IProjectTaskTimelineClient
{
    Task<IReadOnlyList<TimelineTaskGroupData>> ListByProjectAsync(
        Guid projectId,
        CancellationToken ct = default);
}

public interface IProjectFeasibilityTimelineClient
{
    Task<IReadOnlyList<TimelineFeasibilityGroupData>> ListByProjectAsync(
        Guid projectId,
        CancellationToken ct = default);
}
