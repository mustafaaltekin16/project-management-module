using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Application.Interfaces;
using Ozdilek.PM.UserDirectoryService.Domain;

namespace Ozdilek.PM.UserDirectoryService.Application.Services;

public sealed class DepartmentAppService(IDepartmentRepository departments, IEmployeeRepository employees, IUnitOfWork unitOfWork)
{
    public async Task<List<DepartmentDto>> ListAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var all = await departments.ListAllAsync(includeInactive, ct);
        var result = new List<DepartmentDto>();

        foreach (var department in all)
        {
            result.Add(await ToDtoAsync(department, ct));
        }

        return result.OrderBy(d => d.Name).ToList();
    }

    public async Task<DepartmentDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var department = await departments.GetByIdAsync(id, ct) ?? throw new NotFoundException("Departman bulunamadı.");
        var members = await employees.ListByDepartmentAsync(id, ct);
        var headName = await ResolveHeadNameAsync(department.HeadEmployeeId, ct);

        return new DepartmentDetailDto(
            department.Id,
            department.Name,
            department.HeadEmployeeId,
            headName,
            department.IsActive,
            members.Select(m => new EmployeeDto(
                m.Id,
                m.DisplayName,
                m.Email,
                m.DepartmentId,
                department.Name,
                m.Title,
                m.Roles.ToList(),
                m.IsActive,
                !m.Roles.Contains("Admin"),
                m.CreatedAtUtc,
                m.UpdatedAtUtc)).ToList());
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken ct = default)
    {
        if (await departments.ExistsWithNameAsync(request.Name, null, ct))
        {
            throw new DomainException("Bu isimde bir departman zaten var.");
        }

        Employee? head = null;
        if (request.HeadEmployeeId.HasValue &&
            (head = await employees.GetByIdAsync(request.HeadEmployeeId.Value, ct)) is null)
        {
            throw new NotFoundException("Sorumlu olarak seçilen çalışan bulunamadı.");
        }

        var department = Department.Create(request.Name, request.HeadEmployeeId);
        if (head is not null)
        {
            await EnsureEmployeeCanHeadDepartmentAsync(head, department.Id, ct);
        }
        await departments.AddAsync(department, ct);
        head?.AssignDepartment(department.Id);
        await unitOfWork.SaveChangesAsync(ct);

        return await ToDtoAsync(department, ct);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct = default)
    {
        var department = await departments.GetByIdAsync(id, ct) ?? throw new NotFoundException("Departman bulunamadı.");
        if (await departments.ExistsWithNameAsync(request.Name, id, ct))
        {
            throw new DomainException("Bu isimde bir departman zaten var.");
        }

        Employee? head = null;
        if (request.UpdateHead)
        {
            if (!department.IsActive && request.HeadEmployeeId.HasValue)
            {
                throw new DomainException("Arşivlenmiş departmana sorumlu atanamaz. Önce departmanı aktifleştirin.");
            }
            if (request.HeadEmployeeId.HasValue &&
                (head = await employees.GetByIdAsync(request.HeadEmployeeId.Value, ct)) is null)
            {
                throw new NotFoundException("Sorumlu olarak seçilen çalışan bulunamadı.");
            }
            if (head is not null)
            {
                await EnsureEmployeeCanHeadDepartmentAsync(head, department.Id, ct);
            }
        }

        department.Rename(request.Name);
        if (request.UpdateHead)
        {
            department.AssignHead(request.HeadEmployeeId);
            head?.AssignDepartment(department.Id);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDtoAsync(department, ct);
    }

    public async Task<DepartmentDto> AssignHeadAsync(Guid id, AssignDepartmentHeadRequest request, CancellationToken ct = default)
    {
        var department = await departments.GetByIdAsync(id, ct) ?? throw new NotFoundException("Departman bulunamadı.");
        if (!department.IsActive)
        {
            throw new DomainException("Arşivlenmiş departmana sorumlu atanamaz. Önce departmanı aktifleştirin.");
        }

        Employee? head = null;
        if (request.HeadEmployeeId.HasValue &&
            (head = await employees.GetByIdAsync(request.HeadEmployeeId.Value, ct)) is null)
        {
            throw new NotFoundException("Sorumlu olarak seçilen çalışan bulunamadı.");
        }

        if (head is not null)
        {
            await EnsureEmployeeCanHeadDepartmentAsync(head, department.Id, ct);
        }
        department.AssignHead(request.HeadEmployeeId);
        head?.AssignDepartment(department.Id);
        await unitOfWork.SaveChangesAsync(ct);

        return await ToDtoAsync(department, ct);
    }

    public async Task<DepartmentDto> SetStatusAsync(
        Guid id,
        SetDepartmentStatusRequest request,
        CancellationToken ct = default)
    {
        var department = await departments.GetByIdAsync(id, ct) ?? throw new NotFoundException("Departman bulunamadı.");
        if (!request.IsActive && await employees.CountAllByDepartmentAsync(id, ct) > 0)
        {
            throw new DomainException("Aktif veya pasif çalışanı bulunan departman arşivlenemez. Önce tüm çalışanları başka departmana taşıyın.");
        }

        department.SetActive(request.IsActive);
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDtoAsync(department, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var department = await departments.GetByIdAsync(id, ct) ?? throw new NotFoundException("Departman bulunamadı.");
        if (department.IsActive)
        {
            throw new DomainException("Departman kalıcı olarak silinmeden önce arşivlenmelidir.");
        }
        if (await employees.CountAllByDepartmentAsync(id, ct) > 0)
        {
            throw new DomainException("Aktif veya pasif çalışanı bulunan departman kalıcı olarak silinemez.");
        }

        departments.Remove(department);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<DepartmentDto> ToDtoAsync(Department department, CancellationToken ct)
    {
        var headName = await ResolveHeadNameAsync(department.HeadEmployeeId, ct);
        var memberCount = await employees.CountByDepartmentAsync(department.Id, ct);
        return new DepartmentDto(
            department.Id,
            department.Name,
            department.HeadEmployeeId,
            headName,
            memberCount,
            department.IsActive,
            department.CreatedAtUtc,
            department.UpdatedAtUtc);
    }

    private async Task<string?> ResolveHeadNameAsync(Guid? headEmployeeId, CancellationToken ct)
    {
        if (!headEmployeeId.HasValue)
        {
            return null;
        }

        var head = await employees.GetByIdAsync(headEmployeeId.Value, ct);
        return head?.DisplayName;
    }

    private async Task EnsureEmployeeCanHeadDepartmentAsync(Employee employee, Guid targetDepartmentId, CancellationToken ct)
    {
        if (!employee.IsActive)
        {
            throw new DomainException("Pasif çalışan departman sorumlusu olarak atanamaz.");
        }
        if (employee.Roles.Contains("Admin"))
        {
            throw new DomainException("Sistem yöneticisi departman sorumlusu olarak atanamaz.");
        }

        var otherHeadAssignments = (await departments.ListByHeadEmployeeIdAsync(employee.Id, ct))
            .Where(department => department.Id != targetDepartmentId)
            .ToList();
        if (otherHeadAssignments.Count > 0)
        {
            throw new DomainException(
                "Çalışan başka bir departmanın sorumlusudur. Önce mevcut sorumluluk kaldırılmalıdır.");
        }
    }
}
