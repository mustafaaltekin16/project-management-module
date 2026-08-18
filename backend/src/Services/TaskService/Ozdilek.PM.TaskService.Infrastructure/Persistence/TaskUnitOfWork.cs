using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.TaskService.Infrastructure.Persistence;

public sealed class TaskUnitOfWork(TaskDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
