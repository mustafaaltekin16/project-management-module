namespace Ozdilek.PM.ProjectService.Application.Interfaces;

public sealed record DirectoryEmployee(
    Guid Id,
    string DisplayName,
    Guid? DepartmentId,
    IReadOnlyCollection<string> Roles,
    bool IsActive = true,
    bool IsSelectable = true);

public sealed record DirectoryDepartment(
    Guid Id,
    string Name,
    Guid? HeadEmployeeId,
    bool IsActive = true);

/// <summary>
/// Resolves authoritative employee and department identities owned by UserDirectoryService.
/// ProjectService stores their IDs plus display-name snapshots, never client-supplied names alone.
/// </summary>
public interface IUserDirectoryClient
{
    Task<DirectoryEmployee> GetEmployeeAsync(Guid id, CancellationToken ct = default);
    Task<DirectoryDepartment> GetDepartmentAsync(Guid id, CancellationToken ct = default);
}
