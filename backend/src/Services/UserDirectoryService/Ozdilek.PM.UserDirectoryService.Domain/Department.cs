using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.UserDirectoryService.Domain;

/// <summary>
/// A real, manageable department/unit — the counterpart to the free-text department names project
/// creation used to hardcode. Owns which employee is its head; employees reference it by id
/// (<see cref="Employee.DepartmentId"/>) instead of duplicating the department name as a string.
/// </summary>
public class Department : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid? HeadEmployeeId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Department() { }

    public static Department Create(string name, Guid? headEmployeeId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Departman adı zorunludur.");
        }
        EnsureNameLength(name);

        return new Department { Name = name.Trim(), HeadEmployeeId = headEmployeeId };
    }

    public void AssignHead(Guid? headEmployeeId)
    {
        HeadEmployeeId = headEmployeeId;
        MarkUpdated();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Departman adı zorunludur.");
        }
        EnsureNameLength(name);

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        if (!isActive)
        {
            HeadEmployeeId = null;
        }
        MarkUpdated();
    }

    private static void EnsureNameLength(string name)
    {
        if (name.Trim().Length > 200)
        {
            throw new DomainException("Departman adı en fazla 200 karakter olabilir.");
        }
    }
}
