using System.Globalization;
using System.Text;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;

namespace Ozdilek.PM.ProjectService.Application.Services;

/// <summary>
/// Builds the project-detail timeline as a backend read model. Explicit work-package/process links are
/// authoritative; normalized title matching exists only as a migration bridge for legacy records.
/// </summary>
public sealed class ProjectTimelineAppService(
    IProjectRepository projects,
    IProjectTaskTimelineClient taskTimeline,
    IProjectFeasibilityTimelineClient feasibilityTimeline)
{
    private sealed record WorkPackageSource(
        Guid Id,
        string Title,
        Guid? DepartmentId,
        string DepartmentName,
        Guid? ManagerEmployeeId,
        string ManagerName,
        DateOnly StartDate,
        DateOnly EndDate);

    private static readonly IReadOnlyList<(ProjectTimelineProcessType Type, string Label)> ProcessDefinitions =
    [
        (ProjectTimelineProcessType.Feasibility, "Fizibilite Listesi"),
        (ProjectTimelineProcessType.PriceComparison, "Fiyat Karşılaştırma"),
        (ProjectTimelineProcessType.Approval, "Onay Süreci"),
        (ProjectTimelineProcessType.Procurement, "Satın Alma Süreci")
    ];

    public async Task<ProjectTimelineDto> GetAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(projectId, ct)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var warnings = new List<string>();
        var taskGroups = await LoadTasksAsync(projectId, warnings, ct);
        var feasibilityGroups = await LoadFeasibilityAsync(projectId, warnings, ct);
        var workPackages = CreateWorkPackages(project);
        var taskMap = MapTaskGroups(workPackages, taskGroups);
        var feasibilityMap = MapFeasibilityGroups(workPackages, feasibilityGroups);

        var result = workPackages.Select(workPackage =>
        {
            var packageTasks = taskMap.GetValueOrDefault(workPackage.Id) ?? [];
            var packageFeasibility = feasibilityMap.GetValueOrDefault(workPackage.Id) ?? [];
            var processes = ProcessDefinitions
                .Select(definition => BuildProcess(
                    definition.Type,
                    definition.Label,
                    workPackage,
                    packageTasks,
                    packageFeasibility))
                .Where(process => process is not null)
                .Select(process => process!)
                .ToList();
            var state = ResolveWorkPackageState(project.Status, processes);
            var deviation = state == ProjectTimelineState.Completed
                ? 0
                : Math.Min(0, workPackage.EndDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber);

            return new ProjectTimelineWorkPackageDto(
                workPackage.Id,
                workPackage.Title,
                workPackage.DepartmentId,
                workPackage.DepartmentName,
                workPackage.ManagerEmployeeId,
                workPackage.ManagerName,
                workPackage.StartDate,
                workPackage.EndDate,
                deviation,
                state,
                processes);
        }).ToList();

        if (project.Status == ProjectStatus.Completed &&
            result.Any(workPackage => workPackage.State != ProjectTimelineState.Completed))
        {
            warnings.Add("Proje tamamlandı görünüyor ancak bağlı süreçlerin bir bölümü henüz tamamlanmamış.");
        }

        return new ProjectTimelineDto(
            project.Id,
            project.StartDate,
            project.EndDate,
            result,
            warnings.Count > 0,
            warnings);
    }

    private async Task<IReadOnlyList<TimelineTaskGroupData>> LoadTasksAsync(
        Guid projectId,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            return await taskTimeline.ListByProjectAsync(projectId, ct);
        }
        catch
        {
            warnings.Add("Görev süreçleri şu anda alınamadı.");
            return [];
        }
    }

    private async Task<IReadOnlyList<TimelineFeasibilityGroupData>> LoadFeasibilityAsync(
        Guid projectId,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            return await feasibilityTimeline.ListByProjectAsync(projectId, ct);
        }
        catch
        {
            warnings.Add("Fizibilite ve onay süreçleri şu anda alınamadı.");
            return [];
        }
    }

    private static IReadOnlyList<WorkPackageSource> CreateWorkPackages(Project project)
    {
        if (project.Departments.Count == 0)
        {
            return
            [
                new WorkPackageSource(
                    project.Id,
                    string.IsNullOrWhiteSpace(project.Unit) ? project.Name : project.Unit,
                    project.UnitDepartmentId,
                    project.Unit,
                    project.ManagerEmployeeId,
                    project.ManagerName,
                    project.StartDate,
                    project.EndDate)
            ];
        }

        return project.Departments
            .OrderBy(item => item.StartDate ?? project.StartDate)
            .ThenBy(item => item.CreatedAtUtc)
            .Select(item => new WorkPackageSource(
                item.Id,
                string.IsNullOrWhiteSpace(item.Title) ? item.DepartmentName : item.Title,
                item.DepartmentId,
                item.DepartmentName,
                item.ManagerEmployeeId,
                item.ManagerName,
                item.StartDate ?? project.StartDate,
                item.EndDate ?? project.EndDate))
            .ToList();
    }

    private static Dictionary<Guid, List<TimelineTaskGroupData>> MapTaskGroups(
        IReadOnlyList<WorkPackageSource> workPackages,
        IReadOnlyList<TimelineTaskGroupData> groups)
    {
        var result = workPackages.ToDictionary(item => item.Id, _ => new List<TimelineTaskGroupData>());
        foreach (var group in groups)
        {
            var target = group.WorkPackageId is { } linkedId && result.ContainsKey(linkedId)
                ? workPackages.First(item => item.Id == linkedId)
                : FindLegacyWorkPackage(workPackages, $"{group.Title} {group.Subtitle}");

            if (target is not null)
            {
                result[target.Id].Add(group);
            }
        }

        return result;
    }

    private static Dictionary<Guid, List<TimelineFeasibilityGroupData>> MapFeasibilityGroups(
        IReadOnlyList<WorkPackageSource> workPackages,
        IReadOnlyList<TimelineFeasibilityGroupData> groups)
    {
        var result = workPackages.ToDictionary(item => item.Id, _ => new List<TimelineFeasibilityGroupData>());
        foreach (var group in groups)
        {
            var target = group.WorkPackageId is { } linkedId && result.ContainsKey(linkedId)
                ? workPackages.First(item => item.Id == linkedId)
                : FindLegacyWorkPackage(workPackages, group.Name);

            if (target is not null)
            {
                result[target.Id].Add(group);
            }
        }

        return result;
    }

    private static WorkPackageSource? FindLegacyWorkPackage(
        IReadOnlyList<WorkPackageSource> workPackages,
        string sourceText)
    {
        if (workPackages.Count == 1)
        {
            return workPackages[0];
        }

        var normalizedSource = Normalize(sourceText);
        return workPackages.FirstOrDefault(item =>
            Overlaps(normalizedSource, Normalize(item.Title)) ||
            Overlaps(normalizedSource, Normalize(item.DepartmentName)));
    }

    private static ProjectTimelineProcessDto? BuildProcess(
        ProjectTimelineProcessType type,
        string label,
        WorkPackageSource workPackage,
        IReadOnlyList<TimelineTaskGroupData> taskGroups,
        IReadOnlyList<TimelineFeasibilityGroupData> feasibilityGroups)
    {
        var matchingTaskGroups = taskGroups
            .Where(group => IsProcess(group, type))
            .OrderBy(group => group.TimelineSortOrder)
            .ToList();
        var tasks = matchingTaskGroups.SelectMany(group => group.Tasks).ToList();
        var feasibilityItems = feasibilityGroups.SelectMany(group => group.Items).ToList();
        var approvalSteps = feasibilityItems.SelectMany(item => item.Steps).ToList();
        var hasPersistedProcessData = tasks.Count > 0 || type switch
        {
            ProjectTimelineProcessType.Feasibility => feasibilityItems.Count > 0,
            ProjectTimelineProcessType.Approval => approvalSteps.Count > 0,
            _ => false
        };
        if (!hasPersistedProcessData)
        {
            return null;
        }

        var taskState = ResolveTaskState(tasks);
        ProjectTimelineState? feasibilityState = type switch
        {
            ProjectTimelineProcessType.Feasibility => ResolveFeasibilityState(feasibilityGroups),
            ProjectTimelineProcessType.Approval => ResolveApprovalState(feasibilityGroups),
            _ => null
        };
        var state = CombineStates(
            matchingTaskGroups.Count > 0 ? taskState : (ProjectTimelineState?)null,
            feasibilityGroups.Count > 0 ? feasibilityState : null);
        var owner = ResolveOwner(type, tasks, feasibilityGroups, workPackage);
        var start = tasks
            .Where(task => task.StartDateUtc.HasValue)
            .Select(task => DateOnly.FromDateTime(task.StartDateUtc!.Value.UtcDateTime))
            .DefaultIfEmpty(workPackage.StartDate)
            .Min();
        var end = tasks
            .Where(task => task.DueDateUtc.HasValue)
            .Select(task => DateOnly.FromDateTime(task.DueDateUtc!.Value.UtcDateTime))
            .DefaultIfEmpty(workPackage.EndDate)
            .Max();

        return new ProjectTimelineProcessDto(
            type,
            label,
            owner.EmployeeId,
            owner.Name,
            state,
            start,
            end);
    }

    private static bool IsProcess(TimelineTaskGroupData group, ProjectTimelineProcessType type)
    {
        if (Enum.TryParse<ProjectTimelineProcessType>(group.ProcessType, true, out var explicitType))
        {
            return explicitType == type;
        }

        var text = Normalize($"{group.Title} {group.Subtitle} {string.Join(' ', group.Tasks.Select(task => task.Title))}");
        return type switch
        {
            ProjectTimelineProcessType.Feasibility => ContainsAny(text, "fizibilite"),
            ProjectTimelineProcessType.PriceComparison => ContainsAny(text, "fiyat karsilastirma", "teklif karsilastirma"),
            ProjectTimelineProcessType.Approval => ContainsAny(text, "onay"),
            ProjectTimelineProcessType.Procurement => ContainsAny(text, "satin alma", "tedarik"),
            _ => false
        };
    }

    private static ProjectTimelineState ResolveTaskState(IReadOnlyList<TimelineTaskItemData> tasks)
    {
        if (tasks.Count == 0) return ProjectTimelineState.Pending;
        if (tasks.All(task => task.Status.Equals("Done", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectTimelineState.Completed;
        }

        return tasks.Any(task =>
            task.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) ||
            task.Status.Equals("Done", StringComparison.OrdinalIgnoreCase))
            ? ProjectTimelineState.Active
            : ProjectTimelineState.Pending;
    }

    private static ProjectTimelineState ResolveFeasibilityState(
        IReadOnlyList<TimelineFeasibilityGroupData> groups)
    {
        var items = groups.SelectMany(group => group.Items).ToList();
        if (items.Count == 0) return ProjectTimelineState.Pending;
        if (items.Any(item => item.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectTimelineState.Blocked;
        }
        if (items.All(item => item.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectTimelineState.Completed;
        }
        return items.Any(item =>
            item.Status.Equals("PendingApproval", StringComparison.OrdinalIgnoreCase) ||
            item.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            ? ProjectTimelineState.Active
            : ProjectTimelineState.Pending;
    }

    private static ProjectTimelineState ResolveApprovalState(
        IReadOnlyList<TimelineFeasibilityGroupData> groups)
    {
        var steps = groups.SelectMany(group => group.Items).SelectMany(item => item.Steps).ToList();
        if (steps.Count == 0) return ProjectTimelineState.Pending;
        if (steps.Any(step => step.Decision.Equals("Rejected", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectTimelineState.Blocked;
        }
        if (steps.All(step => step.Decision.Equals("Approved", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectTimelineState.Completed;
        }
        return steps.Any(step => step.Decision.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            ? ProjectTimelineState.Active
            : ProjectTimelineState.Pending;
    }

    private static ProjectTimelineState CombineStates(params ProjectTimelineState?[] states)
    {
        var available = states.Where(state => state.HasValue).Select(state => state!.Value).ToList();
        if (available.Count == 0) return ProjectTimelineState.Pending;
        if (available.Contains(ProjectTimelineState.Blocked)) return ProjectTimelineState.Blocked;
        if (available.All(state => state == ProjectTimelineState.Completed)) return ProjectTimelineState.Completed;
        if (available.Any(state => state is ProjectTimelineState.Active or ProjectTimelineState.Completed))
        {
            return ProjectTimelineState.Active;
        }
        return ProjectTimelineState.Pending;
    }

    private static (Guid? EmployeeId, string Name) ResolveOwner(
        ProjectTimelineProcessType type,
        IReadOnlyList<TimelineTaskItemData> tasks,
        IReadOnlyList<TimelineFeasibilityGroupData> groups,
        WorkPackageSource workPackage)
    {
        var taskOwner = tasks
            .OrderBy(task => task.Status.Equals("Done", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(task =>
                task.AssigneeEmployeeId.HasValue &&
                !string.IsNullOrWhiteSpace(task.AssigneeName));
        if (taskOwner is not null)
        {
            return (taskOwner.AssigneeEmployeeId, taskOwner.AssigneeName);
        }

        if (type is ProjectTimelineProcessType.Feasibility or ProjectTimelineProcessType.Approval)
        {
            var steps = groups.SelectMany(group => group.Items).SelectMany(item => item.Steps).ToList();
            var approver = steps
                .Where(step => step.Decision.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                .OrderBy(step => step.Order)
                .FirstOrDefault()
                ?? steps.OrderByDescending(step => step.DecidedAtUtc).FirstOrDefault();
            if (approver is not null && !string.IsNullOrWhiteSpace(approver.ApproverName))
            {
                return (null, approver.ApproverName);
            }
        }

        return (workPackage.ManagerEmployeeId, workPackage.ManagerName);
    }

    private static ProjectTimelineState ResolveWorkPackageState(
        ProjectStatus projectStatus,
        IReadOnlyList<ProjectTimelineProcessDto> processes)
    {
        if (projectStatus == ProjectStatus.Cancelled) return ProjectTimelineState.Blocked;
        if (processes.Count == 0)
        {
            return projectStatus switch
            {
                ProjectStatus.Active => ProjectTimelineState.Active,
                ProjectStatus.Completed => ProjectTimelineState.Completed,
                _ => ProjectTimelineState.Pending
            };
        }
        if (processes.Any(process => process.State == ProjectTimelineState.Blocked))
        {
            return ProjectTimelineState.Blocked;
        }
        if (processes.All(process => process.State == ProjectTimelineState.Completed))
        {
            return ProjectTimelineState.Completed;
        }
        if (processes.Any(process =>
            process.State is ProjectTimelineState.Active or ProjectTimelineState.Completed))
        {
            return ProjectTimelineState.Active;
        }
        if (projectStatus == ProjectStatus.Completed)
        {
            // Legacy completed projects may have no stage records at all. Only use the project status
            // as a fallback when every process is still empty/pending; never mask active child work.
            return ProjectTimelineState.Completed;
        }
        return ProjectTimelineState.Pending;
    }

    private static bool ContainsAny(string source, params string[] candidates) =>
        candidates.Any(source.Contains);

    private static bool Overlaps(string left, string right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        (left.Contains(right, StringComparison.Ordinal) || right.Contains(left, StringComparison.Ordinal));

    private static string Normalize(string value)
    {
        var decomposed = value.ToLower(new CultureInfo("tr-TR")).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
