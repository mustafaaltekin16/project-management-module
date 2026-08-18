using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Application.Dtos;

public sealed record TaskCommentDto(Guid Id, string Author, string Text, DateTimeOffset CreatedAtUtc);

public sealed record TaskItemDto(
    Guid Id,
    string Title,
    string AssigneeName,
    Guid? AssigneeEmployeeId,
    string? Department,
    int? EffortHours,
    int Depth,
    bool IsMainTask,
    Guid? DependsOnTaskId,
    KanbanStatus Status,
    bool IsAiGenerated,
    IReadOnlyList<TaskCommentDto> Comments,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? DueDateUtc,
    string? Category,
    string? Description,
    DateTimeOffset? CompletedAtUtc,
    string? CompletedBy);

public sealed record TaskGroupDto(
    Guid Id,
    Guid ProjectId,
    Guid? WorkPackageId,
    TaskProcessType? ProcessType,
    int TimelineSortOrder,
    string Title,
    string Subtitle,
    IReadOnlyList<TaskItemDto> Tasks,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateTaskGroupRequest(
    Guid ProjectId,
    string Title,
    string Subtitle,
    Guid? WorkPackageId = null,
    TaskProcessType? ProcessType = null,
    int TimelineSortOrder = 0);

public sealed record UpdateTaskGroupRequest(string Title, string Subtitle);

public sealed record ConfigureTaskGroupTimelineRequest(
    Guid? WorkPackageId,
    TaskProcessType? ProcessType,
    int TimelineSortOrder);

public sealed record CreateTaskRequest(
    string Title, string AssigneeName, string? Department, int? EffortHours, bool IsMainTask, Guid? DependsOnTaskId,
    Guid? AssigneeEmployeeId = null, DateTimeOffset? StartDateUtc = null, DateTimeOffset? DueDateUtc = null,
    string? Category = null, string? Description = null);

public sealed record ChangeTaskStatusRequest(KanbanStatus Status);

public sealed record UpdateTaskRequest(
    string Title, string AssigneeName, Guid? AssigneeEmployeeId, string? Department, int? EffortHours,
    DateTimeOffset? StartDateUtc, DateTimeOffset? DueDateUtc, string? Category, string? Description);

public sealed record ReassignTaskRequest(Guid AssigneeEmployeeId, string AssigneeName, string? Department, string ChangedByName);

public sealed record AddCommentRequest(string Author, string Text);

public sealed record ArchiveTaskResult(TaskGroupDto Group, int ArchivedTaskCount);

public sealed record ArchivedTaskDto(
    Guid TaskId, Guid GroupId, string Title, bool IsMainTask, bool IsAiGenerated,
    string AssigneeName, int ArchivedSubtaskCount, DateTimeOffset ArchivedAtUtc);

public sealed record RestoreTaskResult(TaskGroupDto Group, int RestoredTaskCount);

public sealed record CopyTaskResult(TaskGroupDto Group, int CopiedTaskCount);

/// <summary>A flattened task row used by the "my tasks across all projects" screen.</summary>
public sealed record MyTaskDto(Guid TaskId, Guid ProjectId, Guid GroupId, string Title, KanbanStatus Status, bool IsAiGenerated);
