using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.TaskService.Domain;

public class TaskComment : BaseEntity
{
    public Guid TaskId { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;

    private TaskComment() { }

    public TaskComment(Guid taskId, string author, string text)
    {
        TaskId = taskId;
        Author = author;
        Text = text;
    }
}
