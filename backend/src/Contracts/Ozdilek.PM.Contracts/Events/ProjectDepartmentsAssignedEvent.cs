namespace Ozdilek.PM.Contracts.Events;

/// <summary>
/// Published by ProjectService right after a project is created with department/work-package rows.
/// Consumed by TaskService, which creates one real TaskGroup per row — so AI-approved suggestions
/// (see <see cref="WorkPackageApprovedEvent"/>) have a real, matching group to be routed into instead
/// of always falling back to a generic bucket group.
/// </summary>
public sealed record ProjectDepartmentsAssignedEvent
{
    public required Guid ProjectId { get; init; }
    public required IReadOnlyList<ProjectDepartmentAssignmentItem> Departments { get; init; }
}

public sealed record ProjectDepartmentAssignmentItem
{
    public required string Title { get; init; }
    public required string DepartmentName { get; init; }
}
