using Ozdilek.PM.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.AIGatewayService.Domain;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Persistence;

public class AIGatewayDbContext(DbContextOptions<AIGatewayDbContext> options) : DbContext(options)
{
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<AiSuggestionRequest> SuggestionRequests => Set<AiSuggestionRequest>();
    public DbSet<AiSuggestionItem> SuggestionItems => Set<AiSuggestionItem>();
    public DbSet<AiSuggestionActivity> SuggestionActivities => Set<AiSuggestionActivity>();
    public DbSet<PromptAuditLogEntry> PromptAuditLog => Set<PromptAuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PromptTemplate>(entity =>
        {
            entity.ToTable("prompt_templates");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.ProjectType).HasMaxLength(60).IsRequired();
            entity.Property(t => t.TemplateText).IsRequired();
        });

        modelBuilder.Entity<AiSuggestionRequest>(entity =>
        {
            entity.ToTable("ai_suggestion_requests");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ProviderUsed).HasMaxLength(60);
            entity.Metadata.FindNavigation(nameof(AiSuggestionRequest.Items))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<AiSuggestionItem>(entity =>
        {
            entity.ToTable("ai_suggestion_items");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Decision).HasConversion<string>().HasMaxLength(30);
            entity.HasOne<AiSuggestionRequest>().WithMany(r => r.Items).HasForeignKey(i => i.RequestId);
            entity.Metadata.FindNavigation(nameof(AiSuggestionItem.Activities))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<AiSuggestionActivity>(entity =>
        {
            entity.ToTable("ai_suggestion_activities");
            entity.HasKey(a => a.Id);
            entity.HasOne<AiSuggestionItem>().WithMany(i => i.Activities).HasForeignKey(a => a.ItemId);
        });

        modelBuilder.Entity<PromptAuditLogEntry>(entity =>
        {
            entity.ToTable("prompt_audit_log");
            entity.HasKey(l => l.Id);
        });

        modelBuilder.ConfigureBaseEntityKeys();
        base.OnModelCreating(modelBuilder);
    }
}
