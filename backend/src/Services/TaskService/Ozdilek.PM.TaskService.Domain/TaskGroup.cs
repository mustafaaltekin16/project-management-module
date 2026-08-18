using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.TaskService.Domain;

/// <summary>
/// Aggregate root for a Kanban/task grouping under a project (e.g. "Fizibilite Listesi", "Fiyat Karşılaştırma").
/// Owns its tasks; dependency-cycle validation happens here because it is the transaction boundary that can
/// see every task's current dependency edges.
/// </summary>
public class TaskGroup : BaseEntity
{
    private readonly List<ProjectTaskItem> _tasks = [];

    public Guid ProjectId { get; private set; }
    public Guid? WorkPackageId { get; private set; }
    public TaskProcessType? ProcessType { get; private set; }
    public int TimelineSortOrder { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Subtitle { get; private set; } = string.Empty;

    public IReadOnlyCollection<ProjectTaskItem> Tasks => _tasks.AsReadOnly();

    private TaskGroup() { }

    public static TaskGroup Create(
        Guid projectId,
        string title,
        string subtitle,
        Guid? workPackageId = null,
        TaskProcessType? processType = null,
        int timelineSortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Görev grubu başlığı zorunludur.");
        }

        return new TaskGroup
        {
            ProjectId = projectId,
            WorkPackageId = workPackageId,
            ProcessType = processType,
            TimelineSortOrder = Math.Max(0, timelineSortOrder),
            Title = title.Trim(),
            Subtitle = subtitle
        };
    }

    public void Rename(string title, string subtitle)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Görev grubu başlığı zorunludur.");
        }

        Title = title.Trim();
        Subtitle = subtitle;
        MarkUpdated();
    }

    public void ConfigureTimeline(Guid? workPackageId, TaskProcessType? processType, int sortOrder)
    {
        WorkPackageId = workPackageId;
        ProcessType = processType;
        TimelineSortOrder = Math.Max(0, sortOrder);
        MarkUpdated();
    }

    public ProjectTaskItem AddTask(
        string title, string assigneeName, string? department, int? effortHours,
        bool isMainTask, Guid? dependsOnTaskId, bool isAiGenerated = false, Guid? sourceAiSuggestionItemId = null,
        Guid? assigneeEmployeeId = null, DateTimeOffset? startDateUtc = null, DateTimeOffset? dueDateUtc = null,
        string? category = null, string? description = null)
    {
        if (dependsOnTaskId is { } dependsOn)
        {
            var parent = _tasks.FirstOrDefault(t => t.Id == dependsOn && !t.ArchivedAtUtc.HasValue)
                ?? throw new DomainException("Bağımlı olunan görev bu grupta bulunamadı.");

            if (!parent.IsMainTask)
            {
                throw new DomainException(
                    "Bir alt görev başka bir görevin altına bağlanamaz — yalnızca ana görevlere bağlanılabilir.");
            }

            var edges = _tasks.ToDictionary(t => t.Id, t => t.DependsOnTaskId);
            var candidateId = Guid.NewGuid();
            edges[candidateId] = dependsOn;

            if (TaskDependencyValidator.WouldCreateCycle(edges, candidateId, dependsOn))
            {
                throw new DomainException("Bu bağımlılık bir döngüye (circular dependency) yol açar.");
            }
        }

        var parentDepth = dependsOnTaskId is { } parentId
            ? _tasks.First(t => t.Id == parentId).Depth
            : -1;

        var task = ProjectTaskItem.Create(
            Id, title, assigneeName, department, effortHours,
            isMainTask ? 0 : parentDepth + 1,
            isMainTask,
            dependsOnTaskId, isAiGenerated, sourceAiSuggestionItemId,
            assigneeEmployeeId, startDateUtc, dueDateUtc, category, description);

        _tasks.Add(task);
        MarkUpdated();
        return task;
    }

    public void ChangeTaskStatus(Guid taskId, KanbanStatus status, string changedByName)
    {
        var task = ActiveTask(taskId);
        if ((status is KanbanStatus.InProgress or KanbanStatus.Done) &&
            (!task.AssigneeEmployeeId.HasValue || string.IsNullOrWhiteSpace(task.AssigneeName) || task.AssigneeName.Equals("Atanmamış", StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("Atanmamış görev başlatılamaz veya tamamlanamaz. Önce bir sorumlu atayın.");
        }

        if (status == KanbanStatus.Done && task.IsMainTask)
        {
            var openSubtasks = _tasks
                .Where(t => !t.ArchivedAtUtc.HasValue && !t.IsMainTask && t.DependsOnTaskId == task.Id && t.Status != KanbanStatus.Done)
                .Select(t => t.Title)
                .ToList();
            if (openSubtasks.Count > 0)
            {
                throw new DomainException($"Ana görev tamamlanamaz. Önce açık alt görevleri tamamlayın: {string.Join(", ", openSubtasks)}");
            }
        }

        var previousStatus = task.Status;
        task.ChangeStatus(status, changedByName);
        if (previousStatus != status)
        {
            task.AddComment(changedByName, $"Görev durumu {StatusLabel(previousStatus)} durumundan {StatusLabel(status)} durumuna getirildi.");
        }
        MarkUpdated();
    }

    public void UpdateTask(
        Guid taskId,
        string title,
        string assigneeName,
        Guid? assigneeEmployeeId,
        string? department,
        int? effortHours,
        DateTimeOffset? startDateUtc,
        DateTimeOffset? dueDateUtc,
        string? category,
        string? description)
    {
        var task = ActiveTask(taskId);
        task.Update(
            title, assigneeName, assigneeEmployeeId, department, effortHours,
            startDateUtc, dueDateUtc, category, description);
        MarkUpdated();
    }

    public int ArchiveTask(Guid taskId)
    {
        var task = ActiveTask(taskId);
        var archiveIds = new HashSet<Guid> { task.Id };

        if (task.IsMainTask)
        {
            foreach (var child in _tasks.Where(t => !t.ArchivedAtUtc.HasValue && t.DependsOnTaskId == task.Id && !t.IsMainTask))
            {
                archiveIds.Add(child.Id);
            }
        }

        var blockingDependents = _tasks
            .Where(t => !t.ArchivedAtUtc.HasValue && !archiveIds.Contains(t.Id) && t.DependsOnTaskId.HasValue && archiveIds.Contains(t.DependsOnTaskId.Value))
            .Select(t => t.Title)
            .ToList();

        if (blockingDependents.Count > 0)
        {
            throw new DomainException($"Görev arşivlenemedi. Önce bağlı görevleri taşıyın: {string.Join(", ", blockingDependents)}");
        }

        foreach (var item in _tasks.Where(t => archiveIds.Contains(t.Id)))
        {
            item.Archive();
        }

        MarkUpdated();
        return archiveIds.Count;
    }

    public int RestoreTask(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId && t.ArchivedAtUtc.HasValue)
            ?? throw new NotFoundException("Arşivlenmiş görev bulunamadı.");

        if (!task.IsMainTask && task.DependsOnTaskId is { } parentId)
        {
            var parent = _tasks.FirstOrDefault(t => t.Id == parentId);
            if (parent?.ArchivedAtUtc.HasValue == true)
            {
                throw new DomainException("Önce alt görevin bağlı olduğu ana görevi geri yükleyin.");
            }
        }

        var restoreIds = new HashSet<Guid> { task.Id };
        if (task.IsMainTask)
        {
            foreach (var child in _tasks.Where(t => t.ArchivedAtUtc.HasValue && !t.IsMainTask && t.DependsOnTaskId == task.Id))
            {
                restoreIds.Add(child.Id);
            }
        }

        foreach (var item in _tasks.Where(t => restoreIds.Contains(t.Id)))
        {
            item.Restore();
        }

        MarkUpdated();
        return restoreIds.Count;
    }

    public int CopyTask(Guid taskId)
    {
        var source = ActiveTask(taskId);
        var copied = AddTask(
            $"{source.Title} (Kopya)", source.AssigneeName, source.Department, source.EffortHours,
            source.IsMainTask, source.IsMainTask ? null : source.DependsOnTaskId,
            assigneeEmployeeId: source.AssigneeEmployeeId,
            startDateUtc: source.StartDateUtc, dueDateUtc: source.DueDateUtc,
            category: source.Category, description: source.Description);
        var copiedCount = 1;

        if (!source.IsMainTask)
        {
            return copiedCount;
        }

        var children = _tasks
            .Where(t => t.Id != copied.Id && !t.ArchivedAtUtc.HasValue && !t.IsMainTask && t.DependsOnTaskId == source.Id)
            .ToList();
        foreach (var child in children)
        {
            AddTask(
                child.Title, child.AssigneeName, child.Department, child.EffortHours,
                false, copied.Id,
                assigneeEmployeeId: child.AssigneeEmployeeId,
                startDateUtc: child.StartDateUtc, dueDateUtc: child.DueDateUtc,
                category: child.Category, description: child.Description);
            copiedCount++;
        }

        return copiedCount;
    }

    // Records the handoff as a normal task comment rather than a separate audit table — comments are
    // already surfaced both in the task's own thread and in the project's Akış (activity) feed, so this
    // gives free traceability (who reassigned, from whom, to whom, when) without new schema.
    public void ReassignTask(Guid taskId, Guid newAssigneeEmployeeId, string newAssigneeName, string? newDepartment, string changedByName)
    {
        var task = ActiveTask(taskId);
        var previousAssigneeName = task.AssigneeName;
        task.Reassign(newAssigneeEmployeeId, newAssigneeName, newDepartment);
        task.AddComment(changedByName, $"Görev \"{previousAssigneeName}\" kişisinden \"{newAssigneeName}\" kişisine devredildi.");
        MarkUpdated();
    }

    public void AddCommentToTask(Guid taskId, string author, string text)
    {
        var task = ActiveTask(taskId);
        task.AddComment(author, text);
        MarkUpdated();
    }

    private ProjectTaskItem ActiveTask(Guid taskId) =>
        _tasks.FirstOrDefault(t => t.Id == taskId && !t.ArchivedAtUtc.HasValue)
        ?? throw new NotFoundException("Görev bulunamadı.");

    private static string StatusLabel(KanbanStatus status) => status switch
    {
        KanbanStatus.InProgress => "Devam Ediyor",
        KanbanStatus.Done => "Tamamlandı",
        _ => "Bekliyor"
    };
}
