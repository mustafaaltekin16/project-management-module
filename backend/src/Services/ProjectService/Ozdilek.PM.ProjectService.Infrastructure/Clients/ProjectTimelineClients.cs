using Newtonsoft.Json;
using Ozdilek.PM.ProjectService.Application.Interfaces;

namespace Ozdilek.PM.ProjectService.Infrastructure.Clients;

internal sealed class TimelineEnvelope<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
}

internal sealed class TaskTimelineGroupResponse
{
    public Guid Id { get; set; }
    public Guid? WorkPackageId { get; set; }
    public string? ProcessType { get; set; }
    public int TimelineSortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public List<TaskTimelineItemResponse> Tasks { get; set; } = [];
}

internal sealed class TaskTimelineItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public Guid? AssigneeEmployeeId { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? StartDateUtc { get; set; }
    public DateTimeOffset? DueDateUtc { get; set; }
}

internal sealed class FeasibilityTimelineGroupResponse
{
    public Guid Id { get; set; }
    public Guid? WorkPackageId { get; set; }
    public int TimelineSortOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FeasibilityTimelineItemResponse> Items { get; set; } = [];
}

internal sealed class FeasibilityTimelineItemResponse
{
    public string Status { get; set; } = string.Empty;
    public List<ApprovalTimelineStepResponse> Steps { get; set; } = [];
}

internal sealed class ApprovalTimelineStepResponse
{
    public string ApproverName { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Decision { get; set; } = string.Empty;
    public DateTimeOffset? DecidedAtUtc { get; set; }
}

public sealed class ProjectTaskTimelineClient(HttpClient httpClient) : IProjectTaskTimelineClient
{
    public async Task<IReadOnlyList<TimelineTaskGroupData>> ListByProjectAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/api/projects/{projectId}/task-groups", ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);
        var groups = JsonConvert
            .DeserializeObject<TimelineEnvelope<List<TaskTimelineGroupResponse>>>(body)?.Data ?? [];

        return groups.Select(group => new TimelineTaskGroupData(
            group.Id,
            group.WorkPackageId,
            group.ProcessType,
            group.TimelineSortOrder,
            group.Title,
            group.Subtitle,
            group.Tasks.Select(task => new TimelineTaskItemData(
                task.Id,
                task.Title,
                task.AssigneeName,
                task.AssigneeEmployeeId,
                task.Department,
                task.Status,
                task.StartDateUtc,
                task.DueDateUtc)).ToList())).ToList();
    }
}

public sealed class ProjectFeasibilityTimelineClient(HttpClient httpClient) : IProjectFeasibilityTimelineClient
{
    public async Task<IReadOnlyList<TimelineFeasibilityGroupData>> ListByProjectAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/api/projects/{projectId}/feasibility-groups", ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);
        var groups = JsonConvert
            .DeserializeObject<TimelineEnvelope<List<FeasibilityTimelineGroupResponse>>>(body)?.Data ?? [];

        return groups.Select(group => new TimelineFeasibilityGroupData(
            group.Id,
            group.WorkPackageId,
            group.TimelineSortOrder,
            group.Name,
            group.Items.Select(item => new TimelineFeasibilityItemData(
                item.Status,
                item.Steps.Select(step => new TimelineApprovalStepData(
                    step.ApproverName,
                    step.Order,
                    step.Decision,
                    step.DecidedAtUtc)).ToList())).ToList())).ToList();
    }
}
