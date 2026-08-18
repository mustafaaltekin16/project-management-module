using Ozdilek.PM.Contracts.Events;
using Ozdilek.PM.SharedKernel.Events;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.TaskService.Application.Dtos;
using Ozdilek.PM.TaskService.Application.Interfaces;
using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Application.Services;

public sealed class TaskAppService(ITaskGroupRepository groups, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
{
    // Published after any mutation that changes a project's task-completion ratio (new task, status
    // change, archive/restore/copy — all change either the numerator or denominator) so ProjectService
    // can re-derive ProgressPercent/DeviationDays (see ProjectProgressInputsChangedEvent). Not published
    // from RenameGroupAsync/ConfigureTimelineAsync/UpdateTaskAsync/ReassignTaskAsync/AddCommentAsync —
    // none of those change completion state.
    private Task PublishProgressInputsChangedAsync(Guid projectId, CancellationToken ct) =>
        eventPublisher.PublishAsync(new ProjectProgressInputsChangedEvent { ProjectId = projectId }, ct);

    public async Task<TaskGroupDto> CreateGroupAsync(CreateTaskGroupRequest request, CancellationToken ct = default)
    {
        var group = TaskGroup.Create(
            request.ProjectId,
            request.Title,
            request.Subtitle,
            request.WorkPackageId,
            request.ProcessType,
            request.TimelineSortOrder);
        await groups.AddAsync(group, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(group);
    }

    public async Task<List<TaskGroupDto>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var result = await groups.ListByProjectAsync(projectId, ct);
        return result.Select(ToDto).ToList();
    }

    public async Task<TaskGroupDto> RenameGroupAsync(Guid groupId, UpdateTaskGroupRequest request, CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        group.Rename(request.Title, request.Subtitle);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(group);
    }

    public async Task<TaskGroupDto> ConfigureTimelineAsync(
        Guid groupId,
        ConfigureTaskGroupTimelineRequest request,
        CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        group.ConfigureTimeline(request.WorkPackageId, request.ProcessType, request.TimelineSortOrder);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(group);
    }

    public async Task<TaskGroupDto> AddTaskAsync(Guid groupId, CreateTaskRequest request, CancellationToken ct = default)
    {
        // No repository Update() call: `group` is already tracked (loaded via a tracking query), so EF
        // Core's change tracker picks up the newly added task on its own. Calling Update() again on a
        // graph that just gained a brand-new child (client-generated Guid key) makes EF treat that new
        // child as an existing row to UPDATE instead of INSERT — fails with "0 rows affected".
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        group.AddTask(
            request.Title, request.AssigneeName, request.Department, request.EffortHours, request.IsMainTask, request.DependsOnTaskId,
            assigneeEmployeeId: request.AssigneeEmployeeId, startDateUtc: request.StartDateUtc, dueDateUtc: request.DueDateUtc,
            category: request.Category, description: request.Description);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishProgressInputsChangedAsync(group.ProjectId, ct);
        return ToDto(group);
    }

    public async Task<TaskGroupDto> ChangeTaskStatusAsync(
        Guid groupId,
        Guid taskId,
        ChangeTaskStatusRequest request,
        string changedByName,
        bool canManageAllTasks,
        CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        var task = group.Tasks.FirstOrDefault(t => t.Id == taskId && !t.ArchivedAtUtc.HasValue)
            ?? throw new NotFoundException("Görev bulunamadı.");
        if (!canManageAllTasks && !string.Equals(task.AssigneeName, changedByName, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Yalnızca görev sorumlusu veya proje yöneticisi görev durumunu değiştirebilir.");
        }

        group.ChangeTaskStatus(taskId, request.Status, changedByName);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishProgressInputsChangedAsync(group.ProjectId, ct);
        return ToDto(group);
    }

    public async Task<TaskGroupDto> UpdateTaskAsync(Guid groupId, Guid taskId, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        group.UpdateTask(
            taskId, request.Title, request.AssigneeName, request.AssigneeEmployeeId, request.Department,
            request.EffortHours, request.StartDateUtc, request.DueDateUtc, request.Category, request.Description);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(group);
    }

    public async Task<ArchiveTaskResult> ArchiveTaskAsync(Guid groupId, Guid taskId, CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        var archivedTaskCount = group.ArchiveTask(taskId);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishProgressInputsChangedAsync(group.ProjectId, ct);
        return new ArchiveTaskResult(ToDto(group), archivedTaskCount);
    }

    public async Task<List<ArchivedTaskDto>> ListArchivedTasksAsync(Guid projectId, CancellationToken ct = default)
    {
        var projectGroups = await groups.ListByProjectAsync(projectId, ct);
        var result = new List<ArchivedTaskDto>();

        foreach (var group in projectGroups)
        {
            var archived = group.Tasks.Where(t => t.ArchivedAtUtc.HasValue).ToList();
            var archivedIds = archived.Select(t => t.Id).ToHashSet();
            foreach (var task in archived.Where(t => t.IsMainTask || !t.DependsOnTaskId.HasValue || !archivedIds.Contains(t.DependsOnTaskId.Value)))
            {
                var subtaskCount = task.IsMainTask
                    ? archived.Count(t => !t.IsMainTask && t.DependsOnTaskId == task.Id)
                    : 0;
                result.Add(new ArchivedTaskDto(
                    task.Id, group.Id, task.Title, task.IsMainTask, task.IsAiGenerated,
                    task.AssigneeName, subtaskCount, task.ArchivedAtUtc!.Value));
            }
        }

        return result.OrderByDescending(t => t.ArchivedAtUtc).ToList();
    }

    public async Task<RestoreTaskResult> RestoreTaskAsync(Guid groupId, Guid taskId, CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        var restoredTaskCount = group.RestoreTask(taskId);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishProgressInputsChangedAsync(group.ProjectId, ct);
        return new RestoreTaskResult(ToDto(group), restoredTaskCount);
    }

    public async Task<CopyTaskResult> CopyTaskAsync(Guid groupId, Guid taskId, CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        var copiedTaskCount = group.CopyTask(taskId);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishProgressInputsChangedAsync(group.ProjectId, ct);
        return new CopyTaskResult(ToDto(group), copiedTaskCount);
    }

    public async Task<TaskGroupDto> ReassignTaskAsync(Guid groupId, Guid taskId, ReassignTaskRequest request, CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        group.ReassignTask(taskId, request.AssigneeEmployeeId, request.AssigneeName, request.Department, request.ChangedByName);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(group);
    }

    public async Task<TaskGroupDto> AddCommentAsync(Guid groupId, Guid taskId, AddCommentRequest request, CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct) ?? throw new NotFoundException("Görev grubu bulunamadı.");
        group.AddCommentToTask(taskId, request.Author, request.Text);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(group);
    }

    public async Task<List<MyTaskDto>> ListMyTasksAsync(string assigneeName, CancellationToken ct = default)
    {
        var groupsWithTasks = await groups.ListGroupsWithAssigneeTasksAsync(assigneeName, ct);
        return groupsWithTasks
            .SelectMany(g => g.Tasks.Where(t => !t.ArchivedAtUtc.HasValue && t.AssigneeName == assigneeName)
                .Select(t => new MyTaskDto(t.Id, g.ProjectId, g.Id, t.Title, t.Status, t.IsAiGenerated)))
            .ToList();
    }

    private static TaskGroupDto ToDto(TaskGroup group) => new(
        group.Id, group.ProjectId, group.WorkPackageId, group.ProcessType, group.TimelineSortOrder, group.Title, group.Subtitle,
        group.Tasks.Where(t => !t.ArchivedAtUtc.HasValue).Select(t => new TaskItemDto(
            t.Id, t.Title, t.AssigneeName, t.AssigneeEmployeeId, t.Department, t.EffortHours, t.Depth, t.IsMainTask, t.DependsOnTaskId, t.Status, t.IsAiGenerated,
            t.Comments.Select(c => new TaskCommentDto(c.Id, c.Author, c.Text, c.CreatedAtUtc)).ToList(),
            t.CreatedAtUtc, t.UpdatedAtUtc, t.StartDateUtc, t.DueDateUtc, t.Category, t.Description,
            t.CompletedAtUtc, t.CompletedBy))
            .ToList(),
        group.CreatedAtUtc);
}
