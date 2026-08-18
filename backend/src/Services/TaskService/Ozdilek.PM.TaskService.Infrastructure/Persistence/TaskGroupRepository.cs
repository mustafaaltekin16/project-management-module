using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.BuildingBlocks.Persistence;
using Ozdilek.PM.TaskService.Application.Interfaces;
using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Infrastructure.Persistence;

public sealed class TaskGroupRepository(TaskDbContext context) : EfRepository<TaskGroup>(context), ITaskGroupRepository
{
    public override async Task<TaskGroup?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(g => g.Tasks).ThenInclude(t => t.Comments).FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<List<TaskGroup>> ListByProjectAsync(Guid projectId, CancellationToken ct = default) =>
        await Set.Include(g => g.Tasks).ThenInclude(t => t.Comments)
            .Where(g => g.ProjectId == projectId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<List<TaskGroup>> ListGroupsWithAssigneeTasksAsync(string assigneeName, CancellationToken ct = default) =>
        await Set.Include(g => g.Tasks).ThenInclude(t => t.Comments)
            .Where(g => g.Tasks.Any(t => t.AssigneeName == assigneeName))
            .AsNoTracking()
            .ToListAsync(ct);
}

public sealed class ProjectDocumentRepository(TaskDbContext context) : EfRepository<ProjectDocument>(context), IProjectDocumentRepository
{
    public async Task<List<ProjectDocument>> ListByProjectAsync(Guid projectId, CancellationToken ct = default) =>
        await Set.Where(d => d.ProjectId == projectId).AsNoTracking().ToListAsync(ct);
}
