using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Domain;

public static class ProjectBoardDefaults
{
    public static readonly Guid NewProjectsColumnId = Guid.Parse("70000000-0000-0000-0000-000000000001");
    public static readonly Guid OngoingProjectsColumnId = Guid.Parse("70000000-0000-0000-0000-000000000002");
    public static readonly Guid CompletedProjectsColumnId = Guid.Parse("70000000-0000-0000-0000-000000000003");
}

public sealed class ProjectBoardColumn : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#4B7DD8";
    public int SortOrder { get; private set; }
    public bool IsArchived { get; private set; }

    private ProjectBoardColumn() { }

    public static ProjectBoardColumn Create(string name, string color, int sortOrder)
    {
        Validate(name, color);
        return new ProjectBoardColumn
        {
            Name = name.Trim(),
            Color = color.ToUpperInvariant(),
            SortOrder = sortOrder
        };
    }

    public void Update(string name, string color)
    {
        Validate(name, color);
        Name = name.Trim();
        Color = color.ToUpperInvariant();
        MarkUpdated();
    }

    public void Reorder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainException("Sütun sırası negatif olamaz.");
        }

        SortOrder = sortOrder;
        MarkUpdated();
    }

    public void Archive()
    {
        if (IsArchived)
        {
            throw new DomainException("Sütun zaten arşivlenmiş.");
        }

        IsArchived = true;
        MarkUpdated();
    }

    private static void Validate(string name, string color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Sütun adı zorunludur.");
        }

        if (name.Trim().Length > 100)
        {
            throw new DomainException("Sütun adı en fazla 100 karakter olabilir.");
        }

        if (string.IsNullOrWhiteSpace(color) ||
            color.Length != 7 ||
            color[0] != '#' ||
            !color[1..].All(Uri.IsHexDigit))
        {
            throw new DomainException("Sütun rengi #RRGGBB biçiminde olmalıdır.");
        }
    }
}
