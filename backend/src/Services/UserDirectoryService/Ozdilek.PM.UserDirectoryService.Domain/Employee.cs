using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.UserDirectoryService.Domain;

/// <summary>
/// A real person with a login account — the same record serves two purposes: (1) the directory entry
/// picked as a project manager, task assignee or approver elsewhere in the module, and (2) the account
/// used to actually sign in (see AuthController.Login). There is no external identity provider for this
/// product (it's managed standalone, not behind corporate SSO), so this service owns authentication too.
/// </summary>
public class Employee : BaseEntity
{
    private List<string> _roles = [];

    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Guid? DepartmentId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();

    /// <summary>EF Core persistence-only projection of <see cref="_roles"/> as a comma-separated column, kept private so the public API stays a real collection.</summary>
    private string RolesCsv
    {
        get => string.Join(',', _roles);
        set => _roles = string.IsNullOrEmpty(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private Employee() { }

    public static Employee Create(string displayName, string email, string passwordHash, Guid? departmentId, string title, IEnumerable<string> roles)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("Çalışan adı zorunludur.");
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("E-posta zorunludur.");
        }
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Şifre zorunludur.");
        }
        ValidateProfileLengths(displayName, email, title);

        var employee = new Employee
        {
            DisplayName = displayName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            DepartmentId = departmentId,
            Title = title?.Trim() ?? string.Empty
        };
        employee._roles.AddRange(roles.Distinct());
        return employee;
    }

    public void AssignDepartment(Guid? departmentId)
    {
        DepartmentId = departmentId;
        MarkUpdated();
    }

    public void UpdateProfile(
        string displayName,
        string email,
        Guid? departmentId,
        string title,
        IEnumerable<string> roles)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("Çalışan adı zorunludur.");
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("E-posta zorunludur.");
        }
        ValidateProfileLengths(displayName, email, title);

        DisplayName = displayName.Trim();
        Email = email.Trim().ToLowerInvariant();
        DepartmentId = departmentId;
        Title = title?.Trim() ?? string.Empty;
        _roles = roles.Distinct(StringComparer.Ordinal).ToList();
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }

    public void ResetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Şifre zorunludur.");
        }

        PasswordHash = passwordHash;
        MarkUpdated();
    }

    private static void ValidateProfileLengths(string displayName, string email, string? title)
    {
        if (displayName.Trim().Length > 200)
        {
            throw new DomainException("Çalışan adı en fazla 200 karakter olabilir.");
        }
        if (email.Trim().Length > 200)
        {
            throw new DomainException("E-posta en fazla 200 karakter olabilir.");
        }
        if ((title?.Trim().Length ?? 0) > 200)
        {
            throw new DomainException("Unvan en fazla 200 karakter olabilir.");
        }
    }
}
