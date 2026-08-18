using Ozdilek.PM.UserDirectoryService.Domain;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.UserDirectoryService.Application.Interfaces;

public interface IDepartmentRepository : IRepository<Department>
{
    Task<List<Department>> ListAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<List<Department>> ListByHeadEmployeeIdAsync(Guid employeeId, CancellationToken ct = default);
    Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
