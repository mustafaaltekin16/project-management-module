using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.FeasibilityService.Infrastructure.Persistence;

public sealed class FeasibilityUnitOfWork(FeasibilityDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
