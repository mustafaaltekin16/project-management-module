using System.Globalization;
using System.Text.Json;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Domain;

public enum TemplateFieldKind
{
    System,
    Section,
    Custom
}

public sealed record TemplateFieldDefinition(
    string Label,
    string Hint,
    string ContentType,
    string? ListName,
    bool IsRequired,
    bool IsActive,
    TemplateFieldKind Kind,
    string? SystemKey,
    IReadOnlyCollection<string>? Options);

public class TemplateField : BaseEntity
{
    public Guid TemplateId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Hint { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string? ListName { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }
    public TemplateFieldKind Kind { get; private set; } = TemplateFieldKind.Custom;
    public string? SystemKey { get; private set; }
    public string OptionsJson { get; private set; } = "[]";
    public IReadOnlyList<string> Options =>
        JsonSerializer.Deserialize<List<string>>(OptionsJson) ?? [];

    private TemplateField() { }

    public TemplateField(
        Guid templateId,
        string label,
        string hint,
        string contentType,
        string? listName,
        bool isRequired,
        bool isActive,
        int sortOrder,
        TemplateFieldKind kind,
        string? systemKey,
        IReadOnlyCollection<string>? options)
    {
        TemplateId = templateId;
        Label = label;
        Hint = hint;
        ContentType = contentType;
        ListName = listName;
        IsRequired = isRequired;
        IsActive = isActive;
        SortOrder = sortOrder;
        Kind = kind;
        SystemKey = systemKey;
        OptionsJson = JsonSerializer.Serialize(options ?? []);
    }
}

public class ProjectTemplate : BaseEntity
{
    private readonly List<TemplateField> _fields = [];

    public string Name { get; private set; } = string.Empty;
    public ProjectType ApplicableProjectType { get; private set; }
    public IReadOnlyCollection<TemplateField> Fields => _fields.AsReadOnly();

    private ProjectTemplate() { }

    public static ProjectTemplate Create(string name, ProjectType applicableProjectType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Şablon adı zorunludur.");
        }

        return new ProjectTemplate { Name = name.Trim(), ApplicableProjectType = applicableProjectType };
    }

    public TemplateField AddField(
        string label,
        string hint,
        string contentType,
        string? listName,
        bool isRequired,
        bool isActive = true,
        TemplateFieldKind kind = TemplateFieldKind.Custom,
        string? systemKey = null,
        IReadOnlyCollection<string>? options = null)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException("Alan etiketi zorunludur.");
        }

        if (_fields.Any(field =>
                CultureInfo.GetCultureInfo("tr-TR").CompareInfo.Compare(
                    field.Label,
                    label.Trim(),
                    CompareOptions.IgnoreCase) == 0))
        {
            throw new DomainException("Aynı etikete sahip birden fazla şablon alanı eklenemez.");
        }

        if (string.Equals(contentType, "section", StringComparison.OrdinalIgnoreCase))
        {
            kind = TemplateFieldKind.Section;
        }

        if (kind == TemplateFieldKind.System && string.IsNullOrWhiteSpace(systemKey))
        {
            throw new DomainException("Sistem alanı anahtarı zorunludur.");
        }

        if (kind == TemplateFieldKind.System &&
            _fields.Any(field => string.Equals(field.SystemKey, systemKey?.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("Aynı sistem alanı şablona birden fazla kez eklenemez.");
        }

        var normalizedOptions = (options ?? [])
            .Select(option => option?.Trim())
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true))
            .Cast<string>()
            .ToList();

        if (kind == TemplateFieldKind.Section)
        {
            contentType = "section";
            isRequired = false;
            listName = null;
            normalizedOptions.Clear();
        }

        var field = new TemplateField(
            Id,
            label.Trim(),
            hint?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(contentType) ? "text" : contentType.Trim(),
            string.IsNullOrWhiteSpace(listName) ? null : listName.Trim(),
            isRequired,
            isActive,
            _fields.Count,
            kind,
            string.IsNullOrWhiteSpace(systemKey) ? null : systemKey.Trim(),
            normalizedOptions);
        _fields.Add(field);
        MarkUpdated();
        return field;
    }

    public void RemoveField(Guid fieldId)
    {
        var field = _fields.FirstOrDefault(f => f.Id == fieldId)
            ?? throw new NotFoundException("Şablon alanı bulunamadı.");

        if (field.IsRequired)
        {
            throw new DomainException("Zorunlu alanlar şablondan kaldırılamaz.");
        }

        _fields.Remove(field);
        MarkUpdated();
    }

    public void Update(
        string name,
        ProjectType applicableProjectType,
        IReadOnlyCollection<TemplateFieldDefinition> fields)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Şablon adı zorunludur.");
        }

        Name = name.Trim();
        ApplicableProjectType = applicableProjectType;
        _fields.Clear();

        foreach (var field in fields)
        {
            AddField(
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

        MarkUpdated();
    }
}
