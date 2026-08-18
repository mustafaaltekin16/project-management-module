using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.BuildingBlocks.Persistence;
using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Application.Interfaces;
using Ozdilek.PM.UserDirectoryService.Domain;

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Persistence;

public sealed class EmployeeRepository(UserDirectoryDbContext context) : EfRepository<Employee>(context), IEmployeeRepository
{
    public async Task<List<Employee>> SearchAsync(EmployeeListFilter filter, CancellationToken ct = default)
    {
        IQueryable<Employee> query = Set.AsNoTracking();
        if (!filter.IncludeInactive)
        {
            query = query.Where(e => e.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            query = query.Where(e => EF.Property<string>(e, "RolesCsv").Contains(filter.Role));
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var text = filter.SearchText.Trim();
            query = query.Where(e => EF.Functions.ILike(e.DisplayName, $"%{text}%"));
        }

        return await query.OrderBy(e => e.DisplayName).ToListAsync(ct);
    }

    public async Task<List<Employee>> ListByDepartmentAsync(Guid departmentId, CancellationToken ct = default) =>
        await Set.Where(e => e.IsActive && e.DepartmentId == departmentId).AsNoTracking().OrderBy(e => e.DisplayName).ToListAsync(ct);

    public async Task<int> CountByDepartmentAsync(Guid departmentId, CancellationToken ct = default) =>
        await Set.CountAsync(e => e.IsActive && e.DepartmentId == departmentId, ct);

    public async Task<int> CountAllByDepartmentAsync(Guid departmentId, CancellationToken ct = default) =>
        await Set.CountAsync(e => e.DepartmentId == departmentId, ct);

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await Set.FirstOrDefaultAsync(e => e.IsActive && e.Email == normalized, ct);
    }

    public async Task<bool> ExistsWithEmailAsync(string email, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await Set.AnyAsync(
            employee => employee.Email == normalized && (!excludeId.HasValue || employee.Id != excludeId.Value),
            ct);
    }
}
