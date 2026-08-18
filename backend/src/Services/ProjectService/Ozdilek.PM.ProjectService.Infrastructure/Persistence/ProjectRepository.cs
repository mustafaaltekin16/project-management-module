using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.BuildingBlocks.Persistence;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Domain;

namespace Ozdilek.PM.ProjectService.Infrastructure.Persistence;

public sealed class ProjectRepository(ProjectDbContext context) : EfRepository<Project>(context), IProjectRepository
{
    public override async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set
            .Include(p => p.Departments)
            .Include(p => p.Notes)
            .Include(p => p.TemplateValues)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<Project>> SearchAsync(ProjectListFilter filter, CancellationToken ct = default)
    {
        IQueryable<Project> query = Set.AsNoTracking();

        if (filter.Type is not null)
        {
            query = query.Where(p => p.Type == filter.Type);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var text = filter.SearchText.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{text}%") ||
                EF.Functions.ILike(p.ManagerName, $"%{text}%") ||
                EF.Functions.ILike(p.Unit, $"%{text}%"));
        }

        return await query
            .OrderByDescending(project => project.CreatedAtUtc)
            .ThenBy(project => project.Name)
            .ToListAsync(ct);
    }

    public async Task<List<Project>> ListByBoardColumnAsync(
        Guid? columnId,
        Guid? excludedProjectId = null,
        CancellationToken ct = default)
    {
        IQueryable<Project> query = Set.Where(project => project.BoardColumnId == columnId);
        if (excludedProjectId.HasValue)
        {
            query = query.Where(project => project.Id != excludedProjectId.Value);
        }

        return await query
            .OrderBy(project => project.BoardPosition)
            .ThenBy(project => project.CreatedAtUtc)
            .ToListAsync(ct);
    }
}

public sealed class ProjectBoardColumnRepository(ProjectDbContext context)
    : EfRepository<ProjectBoardColumn>(context), IProjectBoardColumnRepository;

public sealed class ProjectTemplateRepository(ProjectDbContext context) : EfRepository<ProjectTemplate>(context), IProjectTemplateRepository
{
    public override async Task<ProjectTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(t => t.Fields).FirstOrDefaultAsync(t => t.Id == id, ct);

    public override async Task<List<ProjectTemplate>> ListAsync(
        System.Linq.Expressions.Expression<Func<ProjectTemplate, bool>>? predicate = null, CancellationToken ct = default)
    {
        IQueryable<ProjectTemplate> query = Set.Include(t => t.Fields).AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }
        return await query.ToListAsync(ct);
    }
}
