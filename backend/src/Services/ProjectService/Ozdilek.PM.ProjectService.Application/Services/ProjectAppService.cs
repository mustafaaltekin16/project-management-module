using Ozdilek.PM.Contracts.Events;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Events;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Application.Services;

public sealed class ProjectAppService(
    IProjectRepository projects,
    IProjectTemplateRepository templates,
    IUnitOfWork unitOfWork,
    IFeasibilityInfoClient feasibilityInfo,
    IUserDirectoryClient userDirectory,
    IEventPublisher eventPublisher)
{
    public async Task<ProjectDetailDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        if (!request.ManagerEmployeeId.HasValue || !request.UnitDepartmentId.HasValue)
        {
            throw new DomainException("Proje yöneticisi ve sorumlu birim dizinden seçilmelidir.");
        }

        var normalizedName = (request.Name ?? string.Empty).Trim().ToLowerInvariant();
        var duplicateNameMatches = await projects.ListAsync(p => p.Name.ToLower() == normalizedName, ct);
        if (duplicateNameMatches.Count > 0)
        {
            throw new DomainException("Bu isimde bir proje zaten mevcut.");
        }

        var manager = await userDirectory.GetEmployeeAsync(request.ManagerEmployeeId.Value, ct);
        EnsureEmployeeIsSelectable(manager, "Proje yöneticisi");
        if (!manager.Roles.Contains("ProjectManager"))
        {
            throw new DomainException("Seçilen çalışan proje yöneticisi rolüne sahip değildir.");
        }

        var unit = await userDirectory.GetDepartmentAsync(request.UnitDepartmentId.Value, ct);
        EnsureDepartmentIsActive(unit);
        DirectoryEmployee? secondManager = null;
        if (request.SecondManagerEmployeeId.HasValue)
        {
            secondManager = await userDirectory.GetEmployeeAsync(request.SecondManagerEmployeeId.Value, ct);
            EnsureEmployeeIsSelectable(secondManager, "İkinci yönetici");
            if (!secondManager.Roles.Contains("ProjectManager"))
            {
                throw new DomainException("İkinci yönetici proje yöneticisi rolüne sahip değildir.");
            }
        }

        ProjectTemplate? template = null;
        if (request.TemplateId is not null)
        {
            template = await templates.GetByIdAsync(request.TemplateId.Value, ct)
                ?? throw new NotFoundException("Seçilen proje şablonu bulunamadı.");

            var isLegacyMultiUnitTemplate =
                request.Type == ProjectType.MultiUnit &&
                template.ApplicableProjectType == ProjectType.FeasibilityBased;
            if (template.ApplicableProjectType != request.Type && !isLegacyMultiUnitTemplate)
            {
                throw new DomainException("Seçilen şablon bu proje türüyle kullanılamaz.");
            }
        }
        else if (request.TemplateValues?.Count > 0)
        {
            throw new DomainException("Şablon alanları yalnızca bir proje şablonu seçildiğinde gönderilebilir.");
        }

        var project = Project.Create(
            request.Name,
            request.Description,
            manager.DisplayName,
            secondManager?.DisplayName,
            unit.Name,
            request.Type,
            request.Budget,
            request.Currency,
            request.StartDate,
            request.EndDate,
            request.TemplateId,
            request.EnabledComponents ?? [],
            template?.Name,
            manager.Id,
            secondManager?.Id,
            unit.Id);

        if (template is not null)
        {
            ApplyTemplateValues(project, template, request.TemplateValues ?? []);
        }

        var assignedDepartments = new List<ProjectDepartmentAssignmentItem>();
        foreach (var department in request.Departments ?? [])
        {
            var resolved = await ResolveDepartmentAssignmentAsync(department, ct);
            project.AddDepartment(
                department.Title,
                resolved.Department.Name,
                resolved.Manager.DisplayName,
                department.StartDate,
                department.EndDate,
                resolved.Department.Id,
                resolved.Manager.Id);
            assignedDepartments.Add(new ProjectDepartmentAssignmentItem
            {
                Title = department.Title,
                DepartmentName = resolved.Department.Name
            });
        }

        await projects.AddAsync(project, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // TaskService has no concept of this project until it reacts to this event — it creates one
        // real TaskGroup per row, so AI-approved suggestions (see WorkPackageApprovedEvent) have an
        // actual matching group to land in instead of always falling back to a generic bucket.
        if (assignedDepartments.Count > 0)
        {
            await eventPublisher.PublishAsync(new ProjectDepartmentsAssignedEvent
            {
                ProjectId = project.Id,
                Departments = assignedDepartments
            }, ct);
        }

        return ToDetailDto(project);
    }

    public async Task<ProjectDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Proje bulunamadı.");
        return ToDetailDto(project);
    }

    public async Task<List<ProjectListItemDto>> SearchAsync(ProjectListFilter filter, CancellationToken ct = default)
    {
        var result = await projects.SearchAsync(filter, ct);
        return result.Select(ToListItemDto).ToList();
    }

    public async Task<ProjectDetailDto> AddDepartmentAsync(Guid id, AddDepartmentRequest request, CancellationToken ct = default)
    {
        // Note: no repository Update() call here — `project` came from a tracking query, so EF Core's
        // change tracker already knows about it. Calling Update() on an already-tracked graph that just
        // gained a brand-new child (with a client-generated Guid key) confuses EF into treating that new
        // child as an existing row to UPDATE instead of INSERT, which fails with 0 rows affected.
        var project = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Proje bulunamadı.");
        var resolved = await ResolveDepartmentAssignmentAsync(request, ct);
        project.AddDepartment(
            request.Title,
            resolved.Department.Name,
            resolved.Manager.DisplayName,
            request.StartDate,
            request.EndDate,
            resolved.Department.Id,
            resolved.Manager.Id);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(project);
    }

    public async Task<ProjectDetailDto> UpdateTemplateValuesAsync(Guid id, UpdateTemplateValuesRequest request, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Proje bulunamadı.");

        var values = request.Values ?? [];
        var duplicate = values
            .GroupBy(value => value.FieldId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new DomainException("Aynı şablon alanı için birden fazla değer gönderilemez.");
        }

        var existingIds = project.TemplateValues.Select(value => value.TemplateFieldId).ToHashSet();
        if (values.Any(value => !existingIds.Contains(value.FieldId)))
        {
            throw new DomainException("Projede bulunmayan bir şablon alanı güncellenemez.");
        }

        foreach (var value in values)
        {
            project.UpdateTemplateValue(value.FieldId, value.Value);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Proje bulunamadı.");
        projects.Remove(project);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<ProjectDetailDto> AddNoteAsync(Guid id, AddNoteRequest request, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Proje bulunamadı.");
        project.AddNote(request.Author, request.Text);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(project);
    }

    public async Task<ProjectDetailDto> UpdateNoteAsync(Guid id, Guid noteId, UpdateNoteRequest request, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Proje bulunamadı.");
        project.UpdateNote(noteId, request.Author, request.Text);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailDto(project);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Proje bulunamadı.");

        // Cross-service rule: a feasibility-based project may only go live once its feasibility is fully
        // approved. FeasibilityService owns that data, so it's checked via a synchronous call rather than
        // duplicating approval state here.
        if (project.Type == ProjectType.FeasibilityBased && !await feasibilityInfo.IsFullyApprovedAsync(id, ct))
        {
            throw new DomainException("Fizibilitesi onaylanmamış bir proje aktifleştirilemez.");
        }

        project.Activate();
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Proje bulunamadı.");
        project.Cancel();
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static ProjectListItemDto ToListItemDto(Project project) => new(
        project.Id, project.Name, project.ManagerName, project.Unit, project.ProgressPercent, project.DeviationDays,
        project.Budget, project.Currency, project.Type, project.Status, project.StartDate, project.EndDate,
        project.ManagerEmployeeId, project.UnitDepartmentId, project.UpdatedAtUtc ?? project.CreatedAtUtc,
        project.BoardColumnId, project.BoardPosition);

    /// <summary>internal, not private: also called by <see cref="ProjectProgressAppService"/> after a progress recompute.</summary>
    internal static ProjectDetailDto ToDetailDto(Project project) => new(
        project.Id, project.Name, project.Description, project.ManagerName, project.SecondManagerName, project.Unit,
        project.Type, project.Status, project.Budget, project.Currency, project.ProgressPercent, project.DeviationDays,
        project.StartDate, project.EndDate, project.TemplateId,
        project.TemplateName,
        project.EnabledComponents.ToList(),
        project.TemplateValues
            .OrderBy(value => value.SortOrder)
            .Select(value => new ProjectTemplateFieldValueDto(
                value.TemplateFieldId,
                value.Label,
                value.Hint,
                value.ContentType,
                value.ListName,
                value.IsRequired,
                value.Options,
                value.Value,
                value.SortOrder))
            .ToList(),
        project.Departments.Select(d => new DepartmentAssignmentDto(
            d.Id,
            d.Title,
            d.DepartmentName,
            d.ManagerName,
            d.StartDate,
            d.EndDate,
            d.DepartmentId,
            d.ManagerEmployeeId)).ToList(),
        project.Notes.OrderBy(n => n.CreatedAtUtc).Select(n => new ProjectNoteDto(n.Id, n.Author, n.Text, n.CreatedAtUtc)).ToList(),
        project.ManagerEmployeeId,
        project.SecondManagerEmployeeId,
        project.UnitDepartmentId);

    private async Task<(DirectoryDepartment Department, DirectoryEmployee Manager)> ResolveDepartmentAssignmentAsync(
        AddDepartmentRequest request,
        CancellationToken ct)
    {
        if (!request.DepartmentId.HasValue || !request.ManagerEmployeeId.HasValue)
        {
            throw new DomainException("Departman ve departman yöneticisi dizinden seçilmelidir.");
        }

        var department = await userDirectory.GetDepartmentAsync(request.DepartmentId.Value, ct);
        var manager = await userDirectory.GetEmployeeAsync(request.ManagerEmployeeId.Value, ct);
        EnsureDepartmentIsActive(department);
        EnsureEmployeeIsSelectable(manager, "Departman yöneticisi");
        if (manager.DepartmentId != department.Id && department.HeadEmployeeId != manager.Id)
        {
            throw new DomainException("Departman yöneticisi seçilen departmana bağlı değildir.");
        }

        return (department, manager);
    }

    private static void EnsureEmployeeIsSelectable(DirectoryEmployee employee, string fieldName)
    {
        if (!employee.IsActive)
        {
            throw new DomainException($"{fieldName} olarak pasif bir çalışan seçilemez.");
        }
        if (!employee.IsSelectable)
        {
            throw new DomainException($"{fieldName} olarak sistem hesabı seçilemez.");
        }
    }

    private static void EnsureDepartmentIsActive(DirectoryDepartment department)
    {
        if (!department.IsActive)
        {
            throw new DomainException("Arşivlenmiş departman projede kullanılamaz.");
        }
    }

    private static void ApplyTemplateValues(
        Project project,
        ProjectTemplate template,
        IReadOnlyCollection<TemplateFieldValueRequest> submittedValues)
    {
        var duplicate = submittedValues
            .GroupBy(value => value.FieldId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new DomainException("Aynı şablon alanı için birden fazla değer gönderilemez.");
        }

        var activeFields = template.Fields
            .Where(field => field.IsActive && field.Kind == TemplateFieldKind.Custom)
            .ToList();
        var activeIds = activeFields.Select(field => field.Id).ToHashSet();
        if (submittedValues.Any(value => !activeIds.Contains(value.FieldId)))
        {
            throw new DomainException("Şablonda bulunmayan veya aktif olmayan bir alan gönderildi.");
        }

        var valuesByField = submittedValues.ToDictionary(value => value.FieldId, value => value.Value);
        foreach (var field in activeFields.OrderBy(field => field.SortOrder))
        {
            valuesByField.TryGetValue(field.Id, out var value);
            var isMissing = string.IsNullOrWhiteSpace(value) ||
                (string.Equals(field.ContentType, "checkbox", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

            if (field.IsRequired && isMissing)
            {
                throw new DomainException($"'{field.Label}' alanı zorunludur.");
            }

            project.AddTemplateValue(
                field.Id,
                field.Label,
                field.Hint,
                field.ContentType,
                field.ListName,
                field.IsRequired,
                field.Options,
                value?.Trim(),
                field.SortOrder);
        }
    }
}
