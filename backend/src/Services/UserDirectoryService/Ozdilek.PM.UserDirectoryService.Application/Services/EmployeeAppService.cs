using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Application.Interfaces;
using Ozdilek.PM.UserDirectoryService.Domain;

namespace Ozdilek.PM.UserDirectoryService.Application.Services;

public sealed class EmployeeAppService(
    IEmployeeRepository employees,
    IDepartmentRepository departments,
    IPasswordHasherService passwordHasher,
    IUnitOfWork unitOfWork)
{
    private static readonly HashSet<string> AllowedRoles =
        ["Admin", "ProjectManager", "Approver", "Member"];

    public async Task<EmployeeDto> CreateAsync(
        CreateEmployeeRequest request,
        bool allowElevatedRoles = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
        {
            throw new DomainException("Şifre en az 4 karakter olmalıdır.");
        }

        if (await employees.ExistsWithEmailAsync(request.Email, null, ct))
        {
            throw new DomainException("Bu e-posta ile kayıtlı bir çalışan zaten var.");
        }

        var roles = ValidateRoles(request.Roles, allowElevatedRoles);
        EnsureSystemAccountIsUnassigned(roles, request.DepartmentId);

        await EnsureDepartmentIsActiveAsync(request.DepartmentId, ct);

        var employee = Employee.Create(
            request.DisplayName,
            request.Email,
            passwordHasher.Hash(request.Password),
            request.DepartmentId,
            request.Title,
            roles);

        await employees.AddAsync(employee, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var departmentNames = await GetDepartmentNameLookupAsync(ct);
        return ToDto(employee, departmentNames);
    }

    public async Task<EmployeeDto> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        bool allowElevatedRoles,
        CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(id, ct) ?? throw new NotFoundException("Çalışan bulunamadı.");
        EnsureCallerCanManageEmployee(employee, allowElevatedRoles);
        if (await employees.ExistsWithEmailAsync(request.Email, id, ct))
        {
            throw new DomainException("Bu e-posta ile kayıtlı bir çalışan zaten var.");
        }

        var roles = ValidateRoles(request.Roles, allowElevatedRoles);
        EnsureAdminRoleIsPreserved(employee, roles);
        EnsureSystemAccountIsUnassigned(roles, request.DepartmentId);
        await EnsureDepartmentIsActiveAsync(request.DepartmentId, ct);

        var headedDepartments = await departments.ListByHeadEmployeeIdAsync(id, ct);
        if (headedDepartments.Any(department => department.Id != request.DepartmentId))
        {
            throw new DomainException(
                "Departman sorumlusu başka bir departmana taşınmadan önce sorumluluğu kaldırılmalıdır.");
        }

        employee.UpdateProfile(
            request.DisplayName,
            request.Email,
            request.DepartmentId,
            request.Title,
            roles);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDto(employee, await GetDepartmentNameLookupAsync(ct));
    }

    public async Task<List<EmployeeDto>> SearchAsync(EmployeeListFilter filter, CancellationToken ct = default)
    {
        var result = await employees.SearchAsync(filter, ct);
        var departmentNames = await GetDepartmentNameLookupAsync(ct);
        return result.Select(e => ToDto(e, departmentNames)).ToList();
    }

    public async Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(id, ct) ?? throw new NotFoundException("Çalışan bulunamadı.");
        var departmentNames = await GetDepartmentNameLookupAsync(ct);
        return ToDto(employee, departmentNames);
    }

    public async Task<EmployeeDto> AssignDepartmentAsync(
        Guid employeeId,
        AssignEmployeeDepartmentRequest request,
        bool allowElevatedAccounts,
        CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct) ?? throw new NotFoundException("Çalışan bulunamadı.");
        EnsureCallerCanManageEmployee(employee, allowElevatedAccounts);
        EnsureSystemAccountIsUnassigned(employee.Roles, request.DepartmentId);

        await EnsureDepartmentIsActiveAsync(request.DepartmentId, ct);

        var headedDepartments = await departments.ListByHeadEmployeeIdAsync(employeeId, ct);
        if (headedDepartments.Any(department => department.Id != request.DepartmentId))
        {
            throw new DomainException(
                "Departman sorumlusu başka bir departmana taşınmadan önce sorumluluğu kaldırılmalıdır.");
        }

        employee.AssignDepartment(request.DepartmentId);
        await unitOfWork.SaveChangesAsync(ct);

        var departmentNames = await GetDepartmentNameLookupAsync(ct);
        return ToDto(employee, departmentNames);
    }

    public async Task<EmployeeDto> SetStatusAsync(
        Guid id,
        SetEmployeeStatusRequest request,
        CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(id, ct) ?? throw new NotFoundException("Çalışan bulunamadı.");
        if (!request.IsActive)
        {
            if (employee.Roles.Contains("Admin"))
            {
                throw new DomainException("Admin hesabı pasife alınamaz.");
            }
            if ((await departments.ListByHeadEmployeeIdAsync(id, ct)).Count > 0)
            {
                throw new DomainException("Departman sorumlusu pasife alınmadan önce sorumluluğu kaldırılmalıdır.");
            }
        }
        else
        {
            await EnsureDepartmentIsActiveAsync(employee.DepartmentId, ct);
        }

        employee.SetActive(request.IsActive);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(employee, await GetDepartmentNameLookupAsync(ct));
    }

    public async Task ResetPasswordAsync(
        Guid id,
        ResetEmployeePasswordRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 4)
        {
            throw new DomainException("Şifre en az 4 karakter olmalıdır.");
        }

        var employee = await employees.GetByIdAsync(id, ct) ?? throw new NotFoundException("Çalışan bulunamadı.");
        employee.ResetPassword(passwordHasher.Hash(request.NewPassword));
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(id, ct) ?? throw new NotFoundException("Çalışan bulunamadı.");
        if (employee.Roles.Contains("Admin"))
        {
            throw new DomainException("Admin hesabı kalıcı olarak silinemez.");
        }
        if (employee.IsActive)
        {
            throw new DomainException("Çalışan kalıcı olarak silinmeden önce pasife alınmalıdır.");
        }
        if ((await departments.ListByHeadEmployeeIdAsync(id, ct)).Count > 0)
        {
            throw new DomainException("Departman sorumlusu kalıcı olarak silinmeden önce sorumluluğu kaldırılmalıdır.");
        }

        employees.Remove(employee);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<Guid, string>> GetDepartmentNameLookupAsync(CancellationToken ct) =>
        (await departments.ListAllAsync(true, ct)).ToDictionary(d => d.Id, d => d.Name);

    private async Task EnsureDepartmentIsActiveAsync(Guid? departmentId, CancellationToken ct)
    {
        if (!departmentId.HasValue)
        {
            return;
        }

        var department = await departments.GetByIdAsync(departmentId.Value, ct)
            ?? throw new NotFoundException("Departman bulunamadı.");
        if (!department.IsActive)
        {
            throw new DomainException("Arşivlenmiş departmana çalışan atanamaz.");
        }
    }

    private static List<string> ValidateRoles(IEnumerable<string>? requestedRoles, bool allowElevatedRoles)
    {
        var roles = (requestedRoles ?? []).Distinct(StringComparer.Ordinal).ToList();
        if (roles.Count == 0 || roles.Any(role => !AllowedRoles.Contains(role)))
        {
            throw new DomainException("Çalışan için geçerli en az bir rol seçilmelidir.");
        }
        if (!allowElevatedRoles && roles.Any(role => role is "Admin" or "ProjectManager"))
        {
            throw new DomainException("Admin ve Proje Yöneticisi rolleri yalnızca Admin tarafından atanabilir.");
        }
        return roles;
    }

    private static void EnsureCallerCanManageEmployee(Employee employee, bool allowElevatedAccounts)
    {
        if (!allowElevatedAccounts &&
            employee.Roles.Any(role => role is "Admin" or "ProjectManager"))
        {
            throw new DomainException("Admin ve Proje Yöneticisi hesapları yalnızca Admin tarafından düzenlenebilir.");
        }
    }

    private static void EnsureAdminRoleIsPreserved(Employee employee, IReadOnlyCollection<string> requestedRoles)
    {
        if (employee.Roles.Contains("Admin") && !requestedRoles.Contains("Admin"))
        {
            throw new DomainException("Admin hesabının Admin rolü kaldırılamaz.");
        }
    }

    private static void EnsureSystemAccountIsUnassigned(
        IReadOnlyCollection<string> roles,
        Guid? departmentId)
    {
        if (roles.Contains("Admin") && departmentId.HasValue)
        {
            throw new DomainException("Sistem Admin hesabı bir departmana bağlanamaz.");
        }
    }

    private static EmployeeDto ToDto(Employee employee, IReadOnlyDictionary<Guid, string> departmentNames) => new(
        employee.Id,
        employee.DisplayName,
        employee.Email,
        employee.DepartmentId,
        employee.DepartmentId.HasValue && departmentNames.TryGetValue(employee.DepartmentId.Value, out var name) ? name : null,
        employee.Title,
        employee.Roles.ToList(),
        employee.IsActive,
        !employee.Roles.Contains("Admin"),
        employee.CreatedAtUtc,
        employee.UpdatedAtUtc);
}
