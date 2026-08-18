using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.TaskService.Domain;

public class ProjectTaskItem : BaseEntity
{
    private readonly List<TaskComment> _comments = [];

    public Guid GroupId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string AssigneeName { get; private set; } = string.Empty;
    // Real employee reference (UserDirectoryService) — added alongside the pre-existing free-text
    // AssigneeName rather than replacing it: AssigneeName stays as a resolved-at-creation-time snapshot
    // (matches how project creation already snapshots manager/department names), so task lists don't
    // need a live directory lookup just to render who a task is assigned to.
    public Guid? AssigneeEmployeeId { get; private set; }
    public string? Department { get; private set; }
    public int? EffortHours { get; private set; }
    public int Depth { get; private set; }
    public bool IsMainTask { get; private set; }
    public Guid? DependsOnTaskId { get; private set; }
    public KanbanStatus Status { get; private set; } = KanbanStatus.Todo;
    public bool IsAiGenerated { get; private set; }
    public Guid? SourceAiSuggestionItemId { get; private set; }
    public DateTimeOffset? StartDateUtc { get; private set; }
    public DateTimeOffset? DueDateUtc { get; private set; }
    public string? Category { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? CompletedBy { get; private set; }

    public IReadOnlyCollection<TaskComment> Comments => _comments.AsReadOnly();

    private ProjectTaskItem() { }

    public static ProjectTaskItem Create(
        Guid groupId, string title, string assigneeName, string? department, int? effortHours,
        int depth, bool isMainTask, Guid? dependsOnTaskId, bool isAiGenerated = false, Guid? sourceAiSuggestionItemId = null,
        Guid? assigneeEmployeeId = null, DateTimeOffset? startDateUtc = null, DateTimeOffset? dueDateUtc = null,
        string? category = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Görev başlığı zorunludur.");
        }

        return new ProjectTaskItem
        {
            GroupId = groupId,
            Title = title.Trim(),
            AssigneeName = assigneeName,
            AssigneeEmployeeId = assigneeEmployeeId,
            Department = department,
            EffortHours = effortHours,
            Depth = depth,
            IsMainTask = isMainTask,
            DependsOnTaskId = dependsOnTaskId,
            IsAiGenerated = isAiGenerated,
            SourceAiSuggestionItemId = sourceAiSuggestionItemId,
            StartDateUtc = startDateUtc,
            DueDateUtc = dueDateUtc,
            Category = category,
            Description = description
        };
    }

    public void ChangeStatus(KanbanStatus status, string changedByName)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        if (status == KanbanStatus.Done)
        {
            CompletedAtUtc = DateTimeOffset.UtcNow;
            CompletedBy = string.IsNullOrWhiteSpace(changedByName) ? AssigneeName : changedByName.Trim();
        }
        else
        {
            CompletedAtUtc = null;
            CompletedBy = null;
        }
        MarkUpdated();
    }

    public void Update(
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
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Görev başlığı zorunludur.");
        }

        if (startDateUtc.HasValue && dueDateUtc.HasValue && dueDateUtc < startDateUtc)
        {
            throw new DomainException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        Title = title.Trim();
        AssigneeName = assigneeName.Trim();
        AssigneeEmployeeId = assigneeEmployeeId;
        Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
        EffortHours = effortHours is > 0 ? effortHours : null;
        StartDateUtc = startDateUtc;
        DueDateUtc = dueDateUtc;
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated();
    }

    public void Archive()
    {
        if (ArchivedAtUtc.HasValue)
        {
            return;
        }

        ArchivedAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }

    public void Restore()
    {
        if (!ArchivedAtUtc.HasValue)
        {
            return;
        }

        ArchivedAtUtc = null;
        MarkUpdated();
    }

    public void Reassign(Guid employeeId, string employeeName, string? department)
    {
        AssigneeEmployeeId = employeeId;
        AssigneeName = employeeName;
        Department = department;
        MarkUpdated();
    }

    public void AddComment(string author, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("Yorum metni boş olamaz.");
        }

        _comments.Add(new TaskComment(Id, author, text));
        MarkUpdated();
    }
}
