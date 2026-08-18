using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Application.Interfaces;

public interface ITaskGroupRepository : IRepository<TaskGroup>
{
    Task<List<TaskGroup>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Every task the given assignee owns, across every project — backs the "Görevlerim" screen.</summary>
    Task<List<TaskGroup>> ListGroupsWithAssigneeTasksAsync(string assigneeName, CancellationToken ct = default);
}

public interface IProjectDocumentRepository : IRepository<ProjectDocument>
{
    Task<List<ProjectDocument>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
}
