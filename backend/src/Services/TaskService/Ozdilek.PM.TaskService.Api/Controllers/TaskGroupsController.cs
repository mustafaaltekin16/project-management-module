using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.TaskService.Application.Dtos;
using Ozdilek.PM.TaskService.Application.Services;

namespace Ozdilek.PM.TaskService.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class TaskGroupsController(TaskAppService appService) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/task-groups")]
    public async Task<ActionResult<ApiResponse<List<TaskGroupDto>>>> ListForProject(Guid projectId, CancellationToken ct)
    {
        var result = await appService.ListByProjectAsync(projectId, ct);
        return Ok(ApiResponse<List<TaskGroupDto>>.Ok(result));
    }

    [HttpPost("task-groups")]
    public async Task<ActionResult<ApiResponse<TaskGroupDto>>> CreateGroup(CreateTaskGroupRequest request, CancellationToken ct)
    {
        var result = await appService.CreateGroupAsync(request, ct);
        return Ok(ApiResponse<TaskGroupDto>.Ok(result));
    }

    [HttpPut("task-groups/{groupId:guid}")]
    public async Task<ActionResult<ApiResponse<TaskGroupDto>>> Rename(Guid groupId, UpdateTaskGroupRequest request, CancellationToken ct)
    {
        var result = await appService.RenameGroupAsync(groupId, request, ct);
        return Ok(ApiResponse<TaskGroupDto>.Ok(result));
    }

    [HttpPut("task-groups/{groupId:guid}/timeline")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<TaskGroupDto>>> ConfigureTimeline(
        Guid groupId,
        ConfigureTaskGroupTimelineRequest request,
        CancellationToken ct)
    {
        var result = await appService.ConfigureTimelineAsync(groupId, request, ct);
        return Ok(ApiResponse<TaskGroupDto>.Ok(result));
    }

    [HttpPost("task-groups/{groupId:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<TaskGroupDto>>> AddTask(Guid groupId, CreateTaskRequest request, CancellationToken ct)
    {
        var result = await appService.AddTaskAsync(groupId, request, ct);
        return Ok(ApiResponse<TaskGroupDto>.Ok(result));
    }

    [HttpPut("task-groups/{groupId:guid}/tasks/{taskId:guid}/status")]
    public async Task<ActionResult<ApiResponse<TaskGroupDto>>> ChangeStatus(Guid groupId, Guid taskId, ChangeTaskStatusRequest request, CancellationToken ct)
    {
        var currentUserName = User.FindFirst("name")?.Value ?? User.FindFirst("sub")?.Value ?? string.Empty;
        var canManageAllTasks = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.ProjectManager);
        var result = await appService.ChangeTaskStatusAsync(groupId, taskId, request, currentUserName, canManageAllTasks, ct);
        return Ok(ApiResponse<TaskGroupDto>.Ok(result));
    }

    [HttpPut("task-groups/{groupId:guid}/tasks/{taskId:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<TaskGroupDto>>> UpdateTask(Guid groupId, Guid taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        var result = await appService.UpdateTaskAsync(groupId, taskId, request, ct);
        return Ok(ApiResponse<TaskGroupDto>.Ok(result));
    }

    [HttpDelete("task-groups/{groupId:guid}/tasks/{taskId:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<ArchiveTaskResult>>> ArchiveTask(Guid groupId, Guid taskId, CancellationToken ct)
    {
        var result = await appService.ArchiveTaskAsync(groupId, taskId, ct);
        return Ok(ApiResponse<ArchiveTaskResult>.Ok(result));
    }

    [HttpGet("projects/{projectId:guid}/archived-tasks")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<List<ArchivedTaskDto>>>> ListArchivedTasks(Guid projectId, CancellationToken ct)
    {
        var result = await appService.ListArchivedTasksAsync(projectId, ct);
        return Ok(ApiResponse<List<ArchivedTaskDto>>.Ok(result));
    }

    [HttpPut("task-groups/{groupId:guid}/tasks/{taskId:guid}/restore")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<RestoreTaskResult>>> RestoreTask(Guid groupId, Guid taskId, CancellationToken ct)
    {
        var result = await appService.RestoreTaskAsync(groupId, taskId, ct);
        return Ok(ApiResponse<RestoreTaskResult>.Ok(result));
    }

    [HttpPost("task-groups/{groupId:guid}/tasks/{taskId:guid}/copy")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<CopyTaskResult>>> CopyTask(Guid groupId, Guid taskId, CancellationToken ct)
    {
        var result = await appService.CopyTaskAsync(groupId, taskId, ct);
        return Ok(ApiResponse<CopyTaskResult>.Ok(result));
    }

    // Deliberately narrower than most task actions here (which are just [Authorize], open to any
    // project member) — reassigning someone else's task is a management decision, not an operational
    // one like completing your own work, so it's gated to Admin/ProjectManager.
    [HttpPut("task-groups/{groupId:guid}/tasks/{taskId:guid}/assignee")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<TaskGroupDto>>> Reassign(Guid groupId, Guid taskId, ReassignTaskRequest request, CancellationToken ct)
    {
        var result = await appService.ReassignTaskAsync(groupId, taskId, request, ct);
        return Ok(ApiResponse<TaskGroupDto>.Ok(result));
    }

    [HttpPost("task-groups/{groupId:guid}/tasks/{taskId:guid}/comments")]
    public async Task<ActionResult<ApiResponse<TaskGroupDto>>> AddComment(Guid groupId, Guid taskId, AddCommentRequest request, CancellationToken ct)
    {
        var result = await appService.AddCommentAsync(groupId, taskId, request, ct);
        return Ok(ApiResponse<TaskGroupDto>.Ok(result));
    }

    [HttpGet("tasks/mine")]
    public async Task<ActionResult<ApiResponse<List<MyTaskDto>>>> MyTasks(CancellationToken ct)
    {
        var currentUserName = User.FindFirst("name")?.Value ?? User.FindFirst("sub")?.Value ?? string.Empty;
        var result = await appService.ListMyTasksAsync(currentUserName, ct);
        return Ok(ApiResponse<List<MyTaskDto>>.Ok(result));
    }
}
