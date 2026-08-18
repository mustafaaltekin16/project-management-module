using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Application.Services;

public sealed class ProjectTemplateAppService(IProjectTemplateRepository templates, IUnitOfWork unitOfWork)
{
    public async Task<TemplateDto> CreateAsync(CreateTemplateRequest request, CancellationToken ct = default)
    {
        var normalizedName = (request.Name ?? string.Empty).Trim().ToLowerInvariant();
        var duplicateNameMatches = await templates.ListAsync(t => t.Name.ToLower() == normalizedName, ct);
        if (duplicateNameMatches.Count > 0)
        {
            throw new DomainException("Bu isimde bir şablon zaten mevcut.");
        }

        ValidateSchema(request.ApplicableProjectType, request.Fields);
        var template = ProjectTemplate.Create(request.Name, request.ApplicableProjectType);
        foreach (var field in request.Fields)
        {
            template.AddField(
                field.Label,
                field.Hint,
                field.ContentType,
                field.ListName,
                field.IsRequired,
                field.IsActive,
                field.Kind,
                field.SystemKey,
                field.Options);
        }

        await templates.AddAsync(template, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(template);
    }

    public async Task<TemplateDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var template = await templates.GetByIdAsync(id, ct) ?? throw new NotFoundException("Şablon bulunamadı.");
        return ToDto(template);
    }

    public async Task<List<TemplateDto>> ListAsync(CancellationToken ct = default)
    {
        var result = await templates.ListAsync(ct: ct);
        return result.Select(ToDto).ToList();
    }

    public async Task<TemplateDto> UpdateAsync(Guid id, UpdateTemplateRequest request, CancellationToken ct = default)
    {
        var normalizedName = (request.Name ?? string.Empty).Trim().ToLowerInvariant();
        var duplicateNameMatches = await templates.ListAsync(t => t.Id != id && t.Name.ToLower() == normalizedName, ct);
        if (duplicateNameMatches.Count > 0)
        {
            throw new DomainException("Bu isimde bir şablon zaten mevcut.");
        }

        ValidateSchema(request.ApplicableProjectType, request.Fields);
        var template = await templates.GetByIdAsync(id, ct) ?? throw new NotFoundException("Şablon bulunamadı.");
        template.Update(
            request.Name,
            request.ApplicableProjectType,
            request.Fields.Select(field => new TemplateFieldDefinition(
                field.Label,
                field.Hint,
                field.ContentType,
                field.ListName,
                field.IsRequired,
                field.IsActive,
                field.Kind,
                field.SystemKey,
                field.Options)).ToList());
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(template);
    }

    private static void ValidateSchema(
        ProjectType projectType,
        IReadOnlyCollection<CreateTemplateFieldRequest> fields)
    {
        var requiredSystemFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["projectName"] = "text",
            ["unit"] = "text",
            ["startDate"] = "date",
            ["endDate"] = "date",
            ["manager"] = "employee"
        };
        if (projectType != ProjectType.Simple)
        {
            requiredSystemFields["budget"] = "currency";
        }

        foreach (var required in requiredSystemFields)
        {
            var field = fields.FirstOrDefault(item =>
                item.Kind == TemplateFieldKind.System &&
                string.Equals(item.SystemKey, required.Key, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                throw new DomainException($"'{required.Key}' sistem alanı şablonda bulunmalıdır.");
            }
            if (!field.IsActive || !field.IsRequired)
            {
                throw new DomainException($"'{field.Label}' sistem alanı aktif ve zorunlu olmalıdır.");
            }
            if (!string.Equals(field.ContentType, required.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException($"'{field.Label}' sistem alanının içerik tipi değiştirilemez.");
            }
        }

        var supportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "section", "text", "textarea", "number", "date", "datetime", "select",
            "checkbox", "yesNo", "employee", "department", "attachment", "currency",
            "checklist", "table", "formGroup", "image", "signature"
        };
        foreach (var field in fields)
        {
            if (!supportedTypes.Contains(field.ContentType))
            {
                throw new DomainException($"'{field.Label}' alanının içerik tipi desteklenmiyor.");
            }
            if (field.Kind == TemplateFieldKind.Custom &&
                string.Equals(field.ContentType, "select", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(field.ListName, "manual", StringComparison.OrdinalIgnoreCase) &&
                (field.Options is null || field.Options.All(string.IsNullOrWhiteSpace)))
            {
                throw new DomainException($"'{field.Label}' alanı için en az bir liste seçeneği zorunludur.");
            }
            if (field.Kind == TemplateFieldKind.Custom &&
                (string.Equals(field.ContentType, "checklist", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(field.ContentType, "table", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(field.ContentType, "formGroup", StringComparison.OrdinalIgnoreCase)) &&
                (field.Options is null || field.Options.All(string.IsNullOrWhiteSpace)))
            {
                throw new DomainException($"'{field.Label}' alanı için en az bir başlık veya madde zorunludur.");
            }
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await templates.GetByIdAsync(id, ct) ?? throw new NotFoundException("Şablon bulunamadı.");
        templates.Remove(template);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<TemplateDto> RemoveFieldAsync(Guid templateId, Guid fieldId, CancellationToken ct = default)
    {
        var template = await templates.GetByIdAsync(templateId, ct) ?? throw new NotFoundException("Şablon bulunamadı.");
        template.RemoveField(fieldId);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(template);
    }

    private static TemplateDto ToDto(ProjectTemplate template) => new(
        template.Id, template.Name, template.ApplicableProjectType,
        template.Fields.OrderBy(f => f.SortOrder)
            .Select(f => new TemplateFieldDto(
                f.Id,
                f.Label,
                f.Hint,
                f.ContentType,
                f.ListName,
                f.IsRequired,
                f.IsActive,
                f.SortOrder,
                f.Kind,
                f.SystemKey,
                f.Options))
            .ToList());
}
