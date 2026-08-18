using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.BuildingBlocks.Persistence;
using Ozdilek.PM.FeasibilityService.Application.Interfaces;
using Ozdilek.PM.FeasibilityService.Domain;

namespace Ozdilek.PM.FeasibilityService.Infrastructure.Persistence;

public sealed class FeasibilityMainGroupRepository(FeasibilityDbContext context)
    : EfRepository<FeasibilityMainGroup>(context), IFeasibilityMainGroupRepository
{
    public override async Task<FeasibilityMainGroup?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(g => g.Items).ThenInclude(i => i.Steps).FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<List<FeasibilityMainGroup>> ListByProjectAsync(Guid projectId, CancellationToken ct = default) =>
        await Set.Include(g => g.Items).ThenInclude(i => i.Steps)
            .Where(g => g.ProjectId == projectId)
            .AsNoTracking()
            .ToListAsync(ct);
}
