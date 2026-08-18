using Microsoft.Extensions.Logging;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Domain;
using Ozdilek.PM.AIGatewayService.Infrastructure.Persistence;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Security;

public sealed class PromptAuditLogger(AIGatewayDbContext context, ILogger<PromptAuditLogger> logger) : IPromptAuditLogger
{
    public async Task LogAsync(Guid projectId, string providerUsed, string redactedPrompt, IReadOnlyList<string> detectedPiiCategories, CancellationToken ct = default)
    {
        var entry = new PromptAuditLogEntry(projectId, providerUsed, redactedPrompt, string.Join(",", detectedPiiCategories));
        context.PromptAuditLog.Add(entry);
        await context.SaveChangesAsync(ct);

        if (detectedPiiCategories.Count > 0)
        {
            logger.LogWarning(
                "Prompt for project {ProjectId} contained sensitive data categories [{Categories}] — redacted before being sent to {Provider}.",
                projectId, string.Join(",", detectedPiiCategories), providerUsed);
        }
    }
}
