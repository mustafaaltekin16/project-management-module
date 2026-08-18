using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.BuildingBlocks.Persistence;

/// <summary>EF Core implementation of <see cref="IRepository{TEntity}"/> shared by every service's Infrastructure layer.</summary>
public class EfRepository<TEntity>(DbContext context) : IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly DbContext Context = context;
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);

    public virtual async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        IQueryable<TEntity> query = Set.AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }
        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(TEntity entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);

    public void Update(TEntity entity) => Set.Update(entity);

    public void Remove(TEntity entity) => Set.Remove(entity);
}
