using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Application.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<List<Project>> SearchAsync(ProjectListFilter filter, CancellationToken ct = default);
    Task<List<Project>> ListByBoardColumnAsync(Guid? columnId, Guid? excludedProjectId = null, CancellationToken ct = default);
}

public interface IProjectTemplateRepository : IRepository<ProjectTemplate>;

public interface IProjectBoardColumnRepository : IRepository<ProjectBoardColumn>;
