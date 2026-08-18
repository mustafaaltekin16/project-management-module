namespace Ozdilek.PM.UserDirectoryService.Application.Dtos;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    Guid? HeadEmployeeId,
    string? HeadDisplayName,
    int MemberCount,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record DepartmentDetailDto(
    Guid Id,
    string Name,
    Guid? HeadEmployeeId,
    string? HeadDisplayName,
    bool IsActive,
    IReadOnlyList<EmployeeDto> Members);

public sealed record CreateDepartmentRequest(string Name, Guid? HeadEmployeeId);

public sealed record UpdateDepartmentRequest(
    string Name,
    Guid? HeadEmployeeId = null,
    bool UpdateHead = false);

public sealed record AssignDepartmentHeadRequest(Guid? HeadEmployeeId);

public sealed record SetDepartmentStatusRequest(bool IsActive);
