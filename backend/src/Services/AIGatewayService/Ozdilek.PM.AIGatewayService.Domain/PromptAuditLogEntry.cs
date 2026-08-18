using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.AIGatewayService.Domain;

/// <summary>
/// Immutable record of every prompt actually sent to an LLM provider — content is always the redacted
/// version (see KVKK filter), never the raw pre-redaction text, so this log itself cannot leak PII.
/// </summary>
public class PromptAuditLogEntry : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string ProviderUsed { get; private set; } = string.Empty;
    public string RedactedPrompt { get; private set; } = string.Empty;
    public string DetectedPiiCategories { get; private set; } = string.Empty;

    private PromptAuditLogEntry() { }

    public PromptAuditLogEntry(Guid projectId, string providerUsed, string redactedPrompt, string detectedPiiCategories)
    {
        ProjectId = projectId;
        ProviderUsed = providerUsed;
        RedactedPrompt = redactedPrompt;
        DetectedPiiCategories = detectedPiiCategories;
    }
}
