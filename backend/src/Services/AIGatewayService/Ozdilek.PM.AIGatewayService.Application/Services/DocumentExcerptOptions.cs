namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Binds the "AiDocumentExcerpts" configuration section — controls how much of each selected document's
/// extracted text gets stuffed into the prompt (phase 1, direct inclusion; see PromptBuilder.AppendDocumentExcerpts).
/// </summary>
public sealed class DocumentExcerptOptions
{
    public const string SectionName = "AiDocumentExcerpts";

    public int MaxCharsPerDocument { get; set; } = 6000;
}
