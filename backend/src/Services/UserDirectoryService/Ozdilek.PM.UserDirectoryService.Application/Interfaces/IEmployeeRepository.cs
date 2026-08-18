using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Domain;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.UserDirectoryService.Application.Interfaces;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<List<Employee>> SearchAsync(EmployeeListFilter filter, CancellationToken ct = default);
    Task<List<Employee>> ListByDepartmentAsync(Guid departmentId, CancellationToken ct = default);
    Task<int> CountByDepartmentAsync(Guid departmentId, CancellationToken ct = default);
    Task<int> CountAllByDepartmentAsync(Guid departmentId, CancellationToken ct = default);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsWithEmailAsync(string email, Guid? excludeId = null, CancellationToken ct = default);
}
