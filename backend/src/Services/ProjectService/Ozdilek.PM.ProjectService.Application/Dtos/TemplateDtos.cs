using Ozdilek.PM.ProjectService.Domain;

namespace Ozdilek.PM.ProjectService.Application.Dtos;

public sealed record TemplateFieldDto(
    Guid Id,
    string Label,
    string Hint,
    string ContentType,
    string? ListName,
    bool IsRequired,
    bool IsActive,
    int SortOrder,
    TemplateFieldKind Kind,
    string? SystemKey,
    IReadOnlyList<string> Options);

public sealed record TemplateDto(Guid Id, string Name, ProjectType ApplicableProjectType, IReadOnlyList<TemplateFieldDto> Fields);

public sealed record CreateTemplateFieldRequest(
    string Label,
    string Hint,
    string ContentType,
    string? ListName,
    bool IsRequired,
    bool IsActive = true,
    TemplateFieldKind Kind = TemplateFieldKind.Custom,
    string? SystemKey = null,
    List<string>? Options = null);

public sealed record CreateTemplateRequest(string Name, ProjectType ApplicableProjectType, List<CreateTemplateFieldRequest> Fields);
public sealed record UpdateTemplateRequest(string Name, ProjectType ApplicableProjectType, List<CreateTemplateFieldRequest> Fields);
