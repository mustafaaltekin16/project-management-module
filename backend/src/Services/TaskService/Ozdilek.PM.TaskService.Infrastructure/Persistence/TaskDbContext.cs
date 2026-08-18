using Ozdilek.PM.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Infrastructure.Persistence;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();
    public DbSet<ProjectTaskItem> TaskItems => Set<ProjectTaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskGroup>(entity =>
        {
            entity.ToTable("task_groups");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Title).HasMaxLength(300).IsRequired();
            entity.Property(g => g.ProcessType).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(g => new { g.ProjectId, g.WorkPackageId, g.ProcessType });
            entity.Metadata.FindNavigation(nameof(TaskGroup.Tasks))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ProjectTaskItem>(entity =>
        {
            entity.ToTable("task_items");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).HasMaxLength(300).IsRequired();
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(t => t.Category).HasMaxLength(100);
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.CompletedBy).HasMaxLength(200);
            entity.HasIndex(t => new { t.GroupId, t.ArchivedAtUtc });
            entity.HasOne<TaskGroup>().WithMany(g => g.Tasks).HasForeignKey(t => t.GroupId);
            entity.Metadata.FindNavigation(nameof(ProjectTaskItem.Comments))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("task_comments");
            entity.HasKey(c => c.Id);
            entity.HasOne<ProjectTaskItem>().WithMany(t => t.Comments).HasForeignKey(c => c.TaskId);
        });

        modelBuilder.Entity<ProjectDocument>(entity =>
        {
            entity.ToTable("project_documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).HasMaxLength(300).IsRequired();
            entity.Property(d => d.Kind).HasConversion<string>().HasMaxLength(30);
            entity.Property(d => d.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(d => d.Content).IsRequired();
            entity.Property(d => d.UploadedBy).HasMaxLength(200);
        });

        modelBuilder.ConfigureBaseEntityKeys();
        base.OnModelCreating(modelBuilder);
    }
}
