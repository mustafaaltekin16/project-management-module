using Ozdilek.PM.ProjectService.Domain;

namespace Ozdilek.PM.ProjectService.Application.Dtos;

public sealed record DepartmentAssignmentDto(
    Guid Id,
    string Title,
    string DepartmentName,
    string ManagerName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid? DepartmentId,
    Guid? ManagerEmployeeId);

public sealed record ProjectNoteDto(Guid Id, string Author, string Text, DateTimeOffset CreatedAtUtc);

public sealed record ProjectTemplateFieldValueDto(
    Guid TemplateFieldId,
    string Label,
    string Hint,
    string ContentType,
    string? ListName,
    bool IsRequired,
    IReadOnlyList<string> Options,
    string? Value,
    int SortOrder);

public sealed record ProjectListItemDto(
    Guid Id,
    string Name,
    string ManagerName,
    string Unit,
    int ProgressPercent,
    int DeviationDays,
    decimal Budget,
    string Currency,
    ProjectType Type,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ManagerEmployeeId,
    Guid? UnitDepartmentId,
    DateTimeOffset UpdatedAtUtc,
    Guid? BoardColumnId,
    decimal BoardPosition);

public sealed record ProjectBoardColumnDto(
    Guid Id,
    string Name,
    string Color,
    int SortOrder,
    DateTimeOffset UpdatedAtUtc,
    bool IsProtected);

public sealed record CreateProjectBoardColumnRequest(string Name, string Color);

public sealed record UpdateProjectBoardColumnRequest(string Name, string Color);

public sealed record ReorderProjectBoardColumnsRequest(IReadOnlyList<Guid> ColumnIds);

public sealed record MoveProjectBoardCardRequest(
    Guid? ColumnId,
    Guid? BeforeProjectId,
    Guid? AfterProjectId,
    DateTimeOffset ExpectedUpdatedAtUtc);

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string Description,
    string ManagerName,
    string? SecondManagerName,
    string Unit,
    ProjectType Type,
    ProjectStatus Status,
    decimal Budget,
    string Currency,
    int ProgressPercent,
    int DeviationDays,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TemplateId,
    string? TemplateName,
    IReadOnlyList<string> EnabledComponents,
    IReadOnlyList<ProjectTemplateFieldValueDto> TemplateValues,
    IReadOnlyList<DepartmentAssignmentDto> Departments,
    IReadOnlyList<ProjectNoteDto> Notes,
    Guid? ManagerEmployeeId,
    Guid? SecondManagerEmployeeId,
    Guid? UnitDepartmentId);

public sealed record CreateProjectRequest(
    string Name,
    string Description,
    string ManagerName,
    string? SecondManagerName,
    string Unit,
    ProjectType Type,
    decimal Budget,
    string Currency,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TemplateId,
    List<string>? EnabledComponents,
    List<TemplateFieldValueRequest>? TemplateValues,
    List<AddDepartmentRequest>? Departments,
    Guid? ManagerEmployeeId = null,
    Guid? SecondManagerEmployeeId = null,
    Guid? UnitDepartmentId = null);

public sealed record TemplateFieldValueRequest(Guid FieldId, string? Value);

public sealed record UpdateTemplateValuesRequest(List<TemplateFieldValueRequest>? Values);

public sealed record AddDepartmentRequest(
    string Title,
    string DepartmentName,
    string ManagerName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid? DepartmentId = null,
    Guid? ManagerEmployeeId = null);

public sealed record AddNoteRequest(string Author, string Text);

public sealed record UpdateNoteRequest(string Author, string Text);

public sealed record ProjectListFilter(ProjectType? Type, string? SearchText);
