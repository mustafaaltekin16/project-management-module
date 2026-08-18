using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Domain;

/// <summary>A single department/unit row under a multi-unit project (title, owning department, manager, window).</summary>
public class ProjectDepartmentAssignment : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Guid? DepartmentId { get; private set; }
    public string DepartmentName { get; private set; } = string.Empty;
    public Guid? ManagerEmployeeId { get; private set; }
    public string ManagerName { get; private set; } = string.Empty;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    private ProjectDepartmentAssignment() { }

    public ProjectDepartmentAssignment(
        Guid projectId,
        string title,
        string departmentName,
        string managerName,
        DateOnly? startDate,
        DateOnly? endDate,
        Guid? departmentId = null,
        Guid? managerEmployeeId = null)
    {
        ProjectId = projectId;
        Title = title;
        DepartmentId = departmentId;
        DepartmentName = departmentName;
        ManagerEmployeeId = managerEmployeeId;
        ManagerName = managerName;
        StartDate = startDate;
        EndDate = endDate;
    }
}
