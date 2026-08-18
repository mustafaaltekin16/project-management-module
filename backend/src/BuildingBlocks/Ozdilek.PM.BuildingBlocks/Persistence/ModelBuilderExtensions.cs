using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.BuildingBlocks.Persistence;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Every <see cref="BaseEntity"/> generates its own Guid Id client-side (see the property
    /// initializer). Without this, EF Core's default convention for Guid keys is
    /// ValueGeneratedOnAdd — so when a brand-new child entity (already carrying a real, non-empty Id)
    /// is discovered via a tracked parent's collection navigation during SaveChanges' automatic
    /// DetectChanges (rather than an explicit context.Add call), EF assumes a non-empty "store
    /// generated" key must mean the row already exists, and emits an UPDATE instead of an INSERT —
    /// which then fails with "0 rows affected" (DbUpdateConcurrencyException). Marking the key
    /// ValueGeneratedNever removes that ambiguity: any non-empty key just means "this is the entity's
    /// permanent identity, assigned by the app," so newly discovered untracked entities are always
    /// treated as Added.
    /// </summary>
    public static void ConfigureBaseEntityKeys(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity.Id)).ValueGeneratedNever();
            }
        }
    }
}
