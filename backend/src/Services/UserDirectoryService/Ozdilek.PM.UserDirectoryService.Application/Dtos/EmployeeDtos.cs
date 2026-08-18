namespace Ozdilek.PM.UserDirectoryService.Application.Dtos;

public sealed record EmployeeDto(
    Guid Id,
    string DisplayName,
    string Email,
    Guid? DepartmentId,
    string? DepartmentName,
    string Title,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool IsSelectable,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record EmployeeListFilter(string? Role, string? SearchText, Guid? DepartmentId, bool IncludeInactive = false);

public sealed record AssignEmployeeDepartmentRequest(Guid? DepartmentId);

public sealed record UpdateEmployeeRequest(
    string DisplayName,
    string Email,
    Guid? DepartmentId,
    string Title,
    List<string> Roles);

public sealed record SetEmployeeStatusRequest(bool IsActive);

public sealed record ResetEmployeePasswordRequest(string NewPassword);
