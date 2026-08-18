using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Application.Services;

public sealed class ProjectBoardAppService(
    IProjectBoardColumnRepository columns,
    IProjectRepository projects,
    IFeasibilityInfoClient feasibilityInfo,
    IUnitOfWork unitOfWork)
{
    private const decimal PositionStep = 1024m;

    public async Task<List<ProjectBoardColumnDto>> ListColumnsAsync(CancellationToken ct = default)
    {
        var result = await columns.ListAsync(column => !column.IsArchived, ct);
        return result
            .OrderBy(column => column.SortOrder)
            .ThenBy(column => column.Name)
            .Select(ToDto)
            .ToList();
    }

    public async Task<ProjectBoardColumnDto> CreateColumnAsync(
        CreateProjectBoardColumnRequest request,
        CancellationToken ct = default)
    {
        var activeColumns = await columns.ListAsync(column => !column.IsArchived, ct);
        EnsureUniqueName(activeColumns, request.Name);

        var column = ProjectBoardColumn.Create(
            request.Name,
            request.Color,
            activeColumns.Count == 0 ? 0 : activeColumns.Max(item => item.SortOrder) + 1);
        await columns.AddAsync(column, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(column);
    }

    public async Task<ProjectBoardColumnDto> UpdateColumnAsync(
        Guid id,
        UpdateProjectBoardColumnRequest request,
        CancellationToken ct = default)
    {
        var column = await GetActiveColumnAsync(id, ct);
        if (IsLifecycleColumn(column.Id))
        {
            throw new DomainException("Proje akışında kullanılan varsayılan sütun düzenlenemez.");
        }
        var activeColumns = await columns.ListAsync(item => !item.IsArchived && item.Id != id, ct);
        EnsureUniqueName(activeColumns, request.Name);

        column.Update(request.Name, request.Color);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(column);
    }

    public async Task ReorderColumnsAsync(
        ReorderProjectBoardColumnsRequest request,
        CancellationToken ct = default)
    {
        var activeColumns = await columns.ListAsync(column => !column.IsArchived, ct);
        var requestedIds = request.ColumnIds?.ToList() ?? [];
        if (requestedIds.Count != activeColumns.Count ||
            requestedIds.Distinct().Count() != requestedIds.Count ||
            requestedIds.ToHashSet().SetEquals(activeColumns.Select(column => column.Id)) is false)
        {
            throw new DomainException("Sütun sıralaması tüm aktif sütunları tam olarak bir kez içermelidir.");
        }

        var byId = activeColumns.ToDictionary(column => column.Id);
        for (var index = 0; index < requestedIds.Count; index++)
        {
            var column = byId[requestedIds[index]];
            column.Reorder(index);
            columns.Update(column);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ArchiveColumnAsync(Guid id, Guid? targetColumnId, CancellationToken ct = default)
    {
        var source = await GetActiveColumnAsync(id, ct);
        if (IsLifecycleColumn(source.Id))
        {
            throw new DomainException("Proje akışında kullanılan varsayılan sütun kaldırılamaz.");
        }

        ProjectBoardColumn? target = null;
        if (targetColumnId.HasValue)
        {
            if (targetColumnId.Value == id)
            {
                throw new DomainException("Sütun kendi üzerine taşınamaz.");
            }
            target = await GetActiveColumnAsync(targetColumnId.Value, ct);
        }

        var assignedProjects = await projects.ListByBoardColumnAsync(id, ct: ct);
        if (assignedProjects.Count > 0 && target is null)
        {
            throw new DomainException("Sütun silinmeden önce içindeki projeler için bir hedef sütun seçilmelidir.");
        }

        if (target is not null)
        {
            var targetProjects = await projects.ListByBoardColumnAsync(target.Id, ct: ct);
            var nextPosition = targetProjects.Count == 0
                ? PositionStep
                : targetProjects.Max(project => project.BoardPosition) + PositionStep;
            foreach (var project in assignedProjects.OrderBy(project => project.BoardPosition))
            {
                await ApplyLifecycleTransitionAsync(project, target.Id, ct);
                project.MoveOnBoard(target.Id, nextPosition);
                nextPosition += PositionStep;
            }
        }

        source.Archive();
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task MoveCardAsync(
        Guid projectId,
        MoveProjectBoardCardRequest request,
        CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(projectId, ct)
            ?? throw new NotFoundException("Proje bulunamadı.");
        var currentVersion = project.UpdatedAtUtc ?? project.CreatedAtUtc;
        if (currentVersion != request.ExpectedUpdatedAtUtc)
        {
            throw new DomainException("Proje kartı başka bir kullanıcı tarafından değiştirildi. Pano yenilenerek güncel hali gösterildi.");
        }

        ProjectBoardColumn? targetColumn = null;
        if (request.ColumnId.HasValue)
        {
            targetColumn = await GetActiveColumnAsync(request.ColumnId.Value, ct);
        }

        await ApplyLifecycleTransitionAsync(project, targetColumn?.Id, ct);

        var targetProjects = await projects.ListByBoardColumnAsync(request.ColumnId, projectId, ct);
        var targetIndex = targetProjects.Count;
        if (request.BeforeProjectId.HasValue)
        {
            targetIndex = targetProjects.FindIndex(item => item.Id == request.BeforeProjectId.Value);
            if (targetIndex < 0)
            {
                throw new DomainException("Hedef kart güncel sütunda bulunamadı. Pano yenilenerek güncel hali gösterildi.");
            }
        }
        else if (request.AfterProjectId.HasValue)
        {
            var afterIndex = targetProjects.FindIndex(item => item.Id == request.AfterProjectId.Value);
            if (afterIndex < 0)
            {
                throw new DomainException("Hedef kart güncel sütunda bulunamadı. Pano yenilenerek güncel hali gösterildi.");
            }
            targetIndex = afterIndex + 1;
        }

        targetProjects.Insert(targetIndex, project);

        for (var index = 0; index < targetProjects.Count; index++)
        {
            targetProjects[index].MoveOnBoard(request.ColumnId, (index + 1) * PositionStep);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ApplyLifecycleTransitionAsync(Project project, Guid? targetColumnId, CancellationToken ct)
    {
        if (project.Status == ProjectStatus.Completed && targetColumnId != ProjectBoardDefaults.CompletedProjectsColumnId)
        {
            throw new DomainException("Tamamlanan proje yalnızca Tamamlananlar sütununda tutulabilir.");
        }

        if (targetColumnId == ProjectBoardDefaults.CompletedProjectsColumnId && project.Status != ProjectStatus.Completed)
        {
            throw new DomainException("Proje, görev ilerlemesi %100 olduğunda otomatik olarak Tamamlananlar sütununa taşınır.");
        }

        if (targetColumnId == ProjectBoardDefaults.NewProjectsColumnId && project.Status != ProjectStatus.Draft)
        {
            throw new DomainException("Aktif proje Yeni Projeler sütununa geri taşınamaz.");
        }

        if (targetColumnId != ProjectBoardDefaults.OngoingProjectsColumnId || project.Status != ProjectStatus.Draft)
        {
            return;
        }

        if (project.Type == ProjectType.FeasibilityBased && !await feasibilityInfo.IsFullyApprovedAsync(project.Id, ct))
        {
            throw new DomainException("Fizibilitesi onaylanmamış proje Devam Edenler sütununa taşınamaz.");
        }

        project.Activate();
    }

    private async Task<ProjectBoardColumn> GetActiveColumnAsync(Guid id, CancellationToken ct)
    {
        var column = await columns.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Pano sütunu bulunamadı.");
        if (column.IsArchived)
        {
            throw new DomainException("Arşivlenmiş bir pano sütunu kullanılamaz.");
        }
        return column;
    }

    private static void EnsureUniqueName(IEnumerable<ProjectBoardColumn> activeColumns, string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (activeColumns.Any(column => string.Equals(column.Name, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("Aynı ada sahip aktif bir pano sütunu zaten var.");
        }
    }

    private static bool IsLifecycleColumn(Guid id) =>
        id == ProjectBoardDefaults.NewProjectsColumnId ||
        id == ProjectBoardDefaults.OngoingProjectsColumnId ||
        id == ProjectBoardDefaults.CompletedProjectsColumnId;

    private static ProjectBoardColumnDto ToDto(ProjectBoardColumn column) => new(
        column.Id,
        column.Name,
        column.Color,
        column.SortOrder,
        column.UpdatedAtUtc ?? column.CreatedAtUtc,
        IsLifecycleColumn(column.Id));
}
