using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.BuildingBlocks.Persistence;
using Ozdilek.PM.UserDirectoryService.Application.Interfaces;
using Ozdilek.PM.UserDirectoryService.Domain;

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Persistence;

public sealed class DepartmentRepository(UserDirectoryDbContext context) : EfRepository<Department>(context), IDepartmentRepository
{
    public async Task<List<Department>> ListAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        IQueryable<Department> query = Set.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(department => department.IsActive);
        }
        return await query.ToListAsync(ct);
    }

    public async Task<List<Department>> ListByHeadEmployeeIdAsync(Guid employeeId, CancellationToken ct = default) =>
        await Set.Where(department => department.HeadEmployeeId == employeeId).ToListAsync(ct);

    public async Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default) =>
        await Set.AnyAsync(
            department =>
                department.Name.ToLower() == name.Trim().ToLower() &&
                (!excludeId.HasValue || department.Id != excludeId.Value),
            ct);
}
