using Ozdilek.PM.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.ProjectService.Domain;

namespace Ozdilek.PM.ProjectService.Infrastructure.Persistence;

public class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectDepartmentAssignment> ProjectDepartmentAssignments => Set<ProjectDepartmentAssignment>();
    public DbSet<ProjectNote> ProjectNotes => Set<ProjectNote>();
    public DbSet<ProjectTemplateFieldValue> ProjectTemplateFieldValues => Set<ProjectTemplateFieldValue>();
    public DbSet<ProjectTemplate> ProjectTemplates => Set<ProjectTemplate>();
    public DbSet<TemplateField> TemplateFields => Set<TemplateField>();
    public DbSet<ProjectBoardColumn> ProjectBoardColumns => Set<ProjectBoardColumn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(300).IsRequired();
            entity.Property(p => p.Budget).HasColumnType("numeric(18,2)");
            entity.Property(p => p.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(p => p.BoardPosition).HasColumnType("numeric(20,4)");
            entity.HasOne<ProjectBoardColumn>()
                .WithMany()
                .HasForeignKey(p => p.BoardColumnId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => new { p.BoardColumnId, p.BoardPosition });

            entity.Metadata.FindNavigation(nameof(Project.Departments))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
            entity.Metadata.FindNavigation(nameof(Project.Notes))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
            entity.Metadata.FindNavigation(nameof(Project.TemplateValues))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);

            entity.Property<string>("EnabledComponentsCsv").HasColumnName("enabled_components_csv").HasMaxLength(500);
            entity.Property(p => p.TemplateName).HasMaxLength(200);
        });

        modelBuilder.Entity<ProjectDepartmentAssignment>(entity =>
        {
            entity.ToTable("project_department_assignments");
            entity.HasKey(d => d.Id);
            entity.HasOne<Project>().WithMany(p => p.Departments).HasForeignKey(d => d.ProjectId);
        });

        modelBuilder.Entity<ProjectNote>(entity =>
        {
            entity.ToTable("project_notes");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Text).HasMaxLength(4000).IsRequired();
            entity.HasOne<Project>().WithMany(p => p.Notes).HasForeignKey(n => n.ProjectId);
        });

        modelBuilder.Entity<ProjectTemplateFieldValue>(entity =>
        {
            entity.ToTable("project_template_field_values");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Label).HasMaxLength(200).IsRequired();
            entity.Property(value => value.Hint).HasMaxLength(500);
            entity.Property(value => value.ContentType).HasMaxLength(60).IsRequired();
            entity.Property(value => value.ListName).HasMaxLength(200);
            entity.Property(value => value.Value).HasMaxLength(4000);
            entity.Property(value => value.OptionsJson).HasMaxLength(4000);
            entity.HasOne<Project>().WithMany(project => project.TemplateValues).HasForeignKey(value => value.ProjectId);
        });

        modelBuilder.Entity<ProjectTemplate>(entity =>
        {
            entity.ToTable("project_templates");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.Property(t => t.ApplicableProjectType).HasConversion<string>().HasMaxLength(40);

            entity.Metadata.FindNavigation(nameof(ProjectTemplate.Fields))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TemplateField>(entity =>
        {
            entity.ToTable("template_fields");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Label).HasMaxLength(200).IsRequired();
            entity.Property(f => f.Hint).HasMaxLength(500);
            entity.Property(f => f.ContentType).HasMaxLength(60).IsRequired();
            entity.Property(f => f.ListName).HasMaxLength(200);
            entity.Property(f => f.SystemKey).HasMaxLength(80);
            entity.Property(f => f.OptionsJson).HasMaxLength(4000);
            entity.HasOne<ProjectTemplate>().WithMany(t => t.Fields).HasForeignKey(f => f.TemplateId);
        });

        modelBuilder.Entity<ProjectBoardColumn>(entity =>
        {
            entity.ToTable("project_board_columns");
            entity.HasKey(column => column.Id);
            entity.Property(column => column.Name).HasMaxLength(100).IsRequired();
            entity.Property(column => column.Color).HasMaxLength(7).IsRequired();
            entity.HasIndex(column => column.SortOrder);
            entity.HasIndex(column => column.Name);

            var seedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            entity.HasData(
                new
                {
                    Id = ProjectBoardDefaults.NewProjectsColumnId,
                    Name = "Yeni Projeler",
                    Color = "#4B7DD8",
                    SortOrder = 0,
                    IsArchived = false,
                    CreatedAtUtc = seedDate,
                    UpdatedAtUtc = (DateTimeOffset?)null
                },
                new
                {
                    Id = ProjectBoardDefaults.OngoingProjectsColumnId,
                    Name = "Devam Edenler",
                    Color = "#2F9E68",
                    SortOrder = 1,
                    IsArchived = false,
                    CreatedAtUtc = seedDate,
                    UpdatedAtUtc = (DateTimeOffset?)null
                },
                new
                {
                    Id = ProjectBoardDefaults.CompletedProjectsColumnId,
                    Name = "Tamamlananlar",
                    Color = "#697386",
                    SortOrder = 2,
                    IsArchived = false,
                    CreatedAtUtc = seedDate,
                    UpdatedAtUtc = (DateTimeOffset?)null
                });
        });

        modelBuilder.ConfigureBaseEntityKeys();
        base.OnModelCreating(modelBuilder);
    }
}
