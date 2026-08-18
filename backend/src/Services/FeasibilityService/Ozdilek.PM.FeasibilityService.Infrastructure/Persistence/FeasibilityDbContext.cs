using Ozdilek.PM.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.FeasibilityService.Domain;

namespace Ozdilek.PM.FeasibilityService.Infrastructure.Persistence;

public class FeasibilityDbContext(DbContextOptions<FeasibilityDbContext> options) : DbContext(options)
{
    public DbSet<FeasibilityMainGroup> MainGroups => Set<FeasibilityMainGroup>();
    public DbSet<FeasibilityItem> Items => Set<FeasibilityItem>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeasibilityMainGroup>(entity =>
        {
            entity.ToTable("feasibility_main_groups");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).HasMaxLength(300).IsRequired();
            entity.HasIndex(g => new { g.ProjectId, g.WorkPackageId });
            entity.Ignore(g => g.TotalRequestedAmount);
            entity.Ignore(g => g.TotalApprovedAmount);
            entity.Metadata.FindNavigation(nameof(FeasibilityMainGroup.Items))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<FeasibilityItem>(entity =>
        {
            entity.ToTable("feasibility_items");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Amount).HasColumnType("numeric(18,2)");
            entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne<FeasibilityMainGroup>().WithMany(g => g.Items).HasForeignKey(i => i.MainGroupId);
            entity.Metadata.FindNavigation(nameof(FeasibilityItem.Steps))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ApprovalStep>(entity =>
        {
            entity.ToTable("approval_steps");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Decision).HasConversion<string>().HasMaxLength(30);
            entity.HasOne<FeasibilityItem>().WithMany(i => i.Steps).HasForeignKey(s => s.FeasibilityItemId);
        });

        modelBuilder.ConfigureBaseEntityKeys();
        base.OnModelCreating(modelBuilder);
    }
}
