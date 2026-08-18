namespace Ozdilek.PM.ProjectService.Application.Dtos;

public enum ProjectTimelineState
{
    Pending,
    Active,
    Completed,
    Blocked
}

public enum ProjectTimelineProcessType
{
    Feasibility,
    PriceComparison,
    Approval,
    Procurement
}

public sealed record ProjectTimelineProcessDto(
    ProjectTimelineProcessType Type,
    string Label,
    Guid? OwnerEmployeeId,
    string OwnerName,
    ProjectTimelineState State,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate);

public sealed record ProjectTimelineWorkPackageDto(
    Guid Id,
    string Title,
    Guid? DepartmentId,
    string DepartmentName,
    Guid? ManagerEmployeeId,
    string ManagerName,
    DateOnly StartDate,
    DateOnly EndDate,
    int DeviationDays,
    ProjectTimelineState State,
    IReadOnlyList<ProjectTimelineProcessDto> Processes);

public sealed record ProjectTimelineDto(
    Guid ProjectId,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<ProjectTimelineWorkPackageDto> WorkPackages,
    bool IsPartial,
    IReadOnlyList<string> Warnings);
