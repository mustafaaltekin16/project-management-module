namespace Ozdilek.PM.UserDirectoryService.Application.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record CreateEmployeeRequest(
    string DisplayName,
    string Email,
    string Password,
    Guid? DepartmentId,
    string Title,
    List<string> Roles);
