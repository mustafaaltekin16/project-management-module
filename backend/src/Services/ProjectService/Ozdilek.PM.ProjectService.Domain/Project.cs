using System.Text.Json;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Domain;

public class Project : BaseEntity
{
    private readonly List<ProjectDepartmentAssignment> _departments = [];
    private readonly List<ProjectNote> _notes = [];
    private readonly List<ProjectTemplateFieldValue> _templateValues = [];
    private List<string> _enabledComponents = [];

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? ManagerEmployeeId { get; private set; }
    public string ManagerName { get; private set; } = string.Empty;
    public Guid? SecondManagerEmployeeId { get; private set; }
    public string? SecondManagerName { get; private set; }
    public Guid? UnitDepartmentId { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public ProjectType Type { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Draft;
    public decimal Budget { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public int ProgressPercent { get; private set; }
    public int DeviationDays { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public Guid? TemplateId { get; private set; }
    public string? TemplateName { get; private set; }
    public Guid? BoardColumnId { get; private set; }
    public decimal BoardPosition { get; private set; }

    public IReadOnlyCollection<ProjectDepartmentAssignment> Departments => _departments.AsReadOnly();
    public IReadOnlyCollection<ProjectNote> Notes => _notes.AsReadOnly();
    public IReadOnlyCollection<ProjectTemplateFieldValue> TemplateValues => _templateValues.AsReadOnly();
    public IReadOnlyCollection<string> EnabledComponents => _enabledComponents.AsReadOnly();

    /// <summary>EF Core persistence-only projection of <see cref="_enabledComponents"/> as a comma-separated column, kept private so the public API stays a real collection.</summary>
    private string EnabledComponentsCsv
    {
        get => string.Join(',', _enabledComponents);
        set => _enabledComponents = string.IsNullOrEmpty(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private Project() { }

    public static Project Create(
        string name,
        string description,
        string managerName,
        string? secondManagerName,
        string unit,
        ProjectType type,
        decimal budget,
        string currency,
        DateOnly startDate,
        DateOnly endDate,
        Guid? templateId,
        IEnumerable<string> enabledComponents,
        string? templateName = null,
        Guid? managerEmployeeId = null,
        Guid? secondManagerEmployeeId = null,
        Guid? unitDepartmentId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Proje adı zorunludur.");
        }

        if (endDate < startDate)
        {
            throw new DomainException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        var project = new Project
        {
            Name = name.Trim(),
            Description = description,
            ManagerEmployeeId = managerEmployeeId,
            ManagerName = managerName,
            SecondManagerEmployeeId = secondManagerEmployeeId,
            SecondManagerName = secondManagerName,
            UnitDepartmentId = unitDepartmentId,
            Unit = unit,
            Type = type,
            Budget = budget,
            Currency = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency,
            StartDate = startDate,
            EndDate = endDate,
            TemplateId = templateId,
            TemplateName = string.IsNullOrWhiteSpace(templateName) ? null : templateName.Trim(),
            Status = ProjectStatus.Draft,
            BoardColumnId = ProjectBoardDefaults.NewProjectsColumnId,
            BoardPosition = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        project._enabledComponents.AddRange(enabledComponents.Distinct());
        return project;
    }

    public void AddTemplateValue(
        Guid templateFieldId,
        string label,
        string hint,
        string contentType,
        string? listName,
        bool isRequired,
        IReadOnlyCollection<string>? options,
        string? value,
        int sortOrder)
    {
        if (TemplateId is null)
        {
            throw new DomainException("Şablonsuz bir projeye şablon alanı eklenemez.");
        }

        if (_templateValues.Any(item => item.TemplateFieldId == templateFieldId))
        {
            throw new DomainException("Aynı şablon alanı için birden fazla değer eklenemez.");
        }

        _templateValues.Add(new ProjectTemplateFieldValue(
            Id,
            templateFieldId,
            label,
            hint,
            contentType,
            listName,
            isRequired,
            options,
            value,
            sortOrder));
    }

    /// <summary>
    /// Şablon alan değerleri proje oluşturulduğunda şablondan kopyalanan bağımsız bir anlık görüntüdür
    /// (bkz. <see cref="AddTemplateValue"/>) — bu yüzden burada gönderilen fieldId'nin canlı şablonda hâlâ
    /// var olması aranmaz, sadece projenin kendi snapshot'ında zaten var olan bir alan güncellenebilir.
    /// </summary>
    public void UpdateTemplateValue(Guid templateFieldId, string? value)
    {
        var existing = _templateValues.FirstOrDefault(item => item.TemplateFieldId == templateFieldId)
            ?? throw new NotFoundException("Projede bulunmayan bir şablon alanı güncellenemez.");

        var trimmed = value?.Trim();
        var isMissing = string.IsNullOrWhiteSpace(trimmed) ||
            (string.Equals(existing.ContentType, "checkbox", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase));

        if (existing.IsRequired && isMissing)
        {
            throw new DomainException($"'{existing.Label}' alanı zorunludur.");
        }

        existing.UpdateValue(trimmed);
        MarkUpdated();
    }

    public void AddDepartment(
        string title,
        string departmentName,
        string managerName,
        DateOnly? startDate,
        DateOnly? endDate,
        Guid? departmentId = null,
        Guid? managerEmployeeId = null)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            throw new DomainException("İş paketi bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        if ((startDate.HasValue && startDate.Value < StartDate) ||
            (endDate.HasValue && endDate.Value > EndDate))
        {
            throw new DomainException("İş paketi tarihleri proje tarih aralığı içinde olmalıdır.");
        }

        _departments.Add(new ProjectDepartmentAssignment(
            Id,
            title,
            departmentName,
            managerName,
            startDate,
            endDate,
            departmentId,
            managerEmployeeId));
        MarkUpdated();
    }

    public void AddNote(string author, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("Not metni zorunludur.");
        }

        _notes.Add(new ProjectNote(Id, author, text.Trim()));
        MarkUpdated();
    }

    public void UpdateNote(Guid noteId, string requestingAuthor, string text)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId)
            ?? throw new NotFoundException("Not bulunamadı.");

        if (!string.Equals(note.Author, requestingAuthor, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Sadece kendi notunuzu düzenleyebilirsiniz.");
        }

        note.UpdateText(text);
        MarkUpdated();
    }

    public void RemoveDepartment(Guid departmentAssignmentId)
    {
        var toRemove = _departments.FirstOrDefault(d => d.Id == departmentAssignmentId)
            ?? throw new NotFoundException("Departman ataması bulunamadı.");
        _departments.Remove(toRemove);
        MarkUpdated();
    }

    public void UpdateProgress(int progressPercent, int deviationDays)
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
        {
            throw new DomainException("Tamamlanmış veya iptal edilmiş bir projenin ilerlemesi güncellenemez.");
        }

        if (progressPercent is < 0 or > 100)
        {
            throw new DomainException("İlerleme oranı 0-100 arasında olmalıdır.");
        }

        ProgressPercent = progressPercent;
        DeviationDays = deviationDays;
        MarkUpdated();

        if (progressPercent == 100 && Status == ProjectStatus.Active)
        {
            Status = ProjectStatus.Completed;
            // Kullanıcı kartı elle özel bir sütuna (lifecycle dışı bir kategoriye) taşımışsa bu
            // otomatik geçiş onu oradan çekip almaz — sadece hâlâ varsayılan "Devam Edenler"
            // sütunundaysa Tamamlananlar'a taşınır.
            if (BoardColumnId == ProjectBoardDefaults.OngoingProjectsColumnId)
            {
                BoardColumnId = ProjectBoardDefaults.CompletedProjectsColumnId;
                BoardPosition = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
    }

    public void Activate()
    {
        if (Status != ProjectStatus.Draft)
        {
            throw new DomainException("Sadece taslak durumundaki projeler aktifleştirilebilir.");
        }

        Status = ProjectStatus.Active;
        // Aynı mantık: proje hâlâ varsayılan "Yeni Projeler" sütunundaysa Devam Edenler'e taşınır;
        // kullanıcı onu elle özel bir sütuna (kategoriye) koymuşsa dokunulmaz.
        if (BoardColumnId == ProjectBoardDefaults.NewProjectsColumnId)
        {
            BoardColumnId = ProjectBoardDefaults.OngoingProjectsColumnId;
            BoardPosition = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
        {
            throw new DomainException("Tamamlanmış veya iptal edilmiş bir proje yeniden iptal edilemez.");
        }

        Status = ProjectStatus.Cancelled;
        MarkUpdated();
    }

    public void MoveOnBoard(Guid? columnId, decimal position)
    {
        if (position < 0)
        {
            throw new DomainException("Kart sırası negatif olamaz.");
        }

        BoardColumnId = columnId;
        BoardPosition = position;
        MarkUpdated();
    }
}

public class ProjectTemplateFieldValue : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid TemplateFieldId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Hint { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string? ListName { get; private set; }
    public bool IsRequired { get; private set; }
    public string? Value { get; private set; }
    public int SortOrder { get; private set; }
    public string OptionsJson { get; private set; } = "[]";
    public IReadOnlyList<string> Options =>
        JsonSerializer.Deserialize<List<string>>(OptionsJson) ?? [];

    private ProjectTemplateFieldValue() { }

    public ProjectTemplateFieldValue(
        Guid projectId,
        Guid templateFieldId,
        string label,
        string hint,
        string contentType,
        string? listName,
        bool isRequired,
        IReadOnlyCollection<string>? options,
        string? value,
        int sortOrder)
    {
        ProjectId = projectId;
        TemplateFieldId = templateFieldId;
        Label = label;
        Hint = hint;
        ContentType = contentType;
        ListName = listName;
        IsRequired = isRequired;
        OptionsJson = JsonSerializer.Serialize(options ?? []);
        Value = value;
        SortOrder = sortOrder;
    }

    public void UpdateValue(string? value)
    {
        Value = value;
    }
}
