using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.AIGatewayService.Domain;

/// <summary>
/// One "generate work package suggestions" call for a project: what was asked, what was actually sent
/// to the LLM (redacted), which provider answered, and the resulting suggestion items — each of which
/// needs its own approval before TaskService turns it into a real task (see brief: "yapay zekâ
/// çıktılarının doğrudan kullanılmaması için onay mekanizması geliştirilmiştir").
/// </summary>
public class AiSuggestionRequest : BaseEntity
{
    private readonly List<AiSuggestionItem> _items = [];

    public Guid ProjectId { get; private set; }
    public string ProjectType { get; private set; } = string.Empty;
    public string? ExtraInstructions { get; private set; }
    public string RedactedPrompt { get; private set; } = string.Empty;
    public string ProviderUsed { get; private set; } = string.Empty;
    // Display-only, comma-joined snapshot of the document names used to build this request's prompt —
    // not a foreign-key list, so History still shows the right names even if a document is later renamed/deleted.
    public string? SelectedDocumentNames { get; private set; }
    // True only if RAG actually returned retrieved_contexts for this generation — false whenever RAG sync/ask
    // failed or came back empty, so the suggestions were built from project metadata alone, not real document
    // content. Persisted (not computed) so History/ListByProjectAsync reports the true state of past requests.
    public bool UsedRealDocumentContext { get; private set; }

    public IReadOnlyCollection<AiSuggestionItem> Items => _items.AsReadOnly();

    private AiSuggestionRequest() { }

    public static AiSuggestionRequest Create(
        Guid projectId, string projectType, string? extraInstructions, string redactedPrompt, string providerUsed,
        string? selectedDocumentNames = null, bool usedRealDocumentContext = false)
    {
        if (projectId == Guid.Empty)
        {
            throw new DomainException("Proje kimliği zorunludur.");
        }

        return new AiSuggestionRequest
        {
            ProjectId = projectId,
            ProjectType = projectType,
            ExtraInstructions = extraInstructions,
            RedactedPrompt = redactedPrompt,
            ProviderUsed = providerUsed,
            SelectedDocumentNames = selectedDocumentNames,
            UsedRealDocumentContext = usedRealDocumentContext
        };
    }

    // Returns the created item so the caller can immediately attach its activities (AddActivity) —
    // the item needs to exist first since activities carry the item's Id as a foreign key.
    public AiSuggestionItem AddItem(
        string title, string department, int effortHours, string? sourceDocument,
        string? description = null, string? sequenceNote = null, string? insertAfterTaskTitle = null,
        int? sequenceRank = null, bool isAtProjectStart = false)
    {
        var item = new AiSuggestionItem(
            Id, title, department, effortHours, sourceDocument, description, sequenceNote, insertAfterTaskTitle,
            sequenceRank, isAtProjectStart);
        _items.Add(item);
        MarkUpdated();
        return item;
    }

    public AiSuggestionItem ApproveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException("Öneri bulunamadı.");
        item.Approve();
        MarkUpdated();
        return item;
    }

    public void RejectItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException("Öneri bulunamadı.");
        item.Reject();
        MarkUpdated();
    }
}
