using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Infrastructure.Persistence;

public sealed class ProjectUnitOfWork(ProjectDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
