using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Persistence;

public sealed class UserDirectoryUnitOfWork(UserDirectoryDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
