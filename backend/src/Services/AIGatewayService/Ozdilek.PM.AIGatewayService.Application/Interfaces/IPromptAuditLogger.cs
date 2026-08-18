namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

public interface IPromptAuditLogger
{
    Task LogAsync(Guid projectId, string providerUsed, string redactedPrompt, IReadOnlyList<string> detectedPiiCategories, CancellationToken ct = default);
}
