using Ozdilek.PM.AIGatewayService.Application.Dtos;

namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

/// <summary>
/// Converts this app's own already-fetched, complete lists (existing tasks, pending suggestion titles)
/// into RAG-retrieved subsets for work-package generation prompts. The caller must keep using the full,
/// original lists for anything that requires exact-match correctness (e.g. AiSuggestionAppService's
/// titlesToSkip deduplication) — these methods only decide what gets SHOWN to the LLM, never what counts
/// as "already exists". Never throws: any failure (upload/indexing/ask) yields an empty list, so the
/// corresponding prompt section is silently omitted, same convention as AiSuggestionAppService's
/// CollectRagDocumentExcerptsAsync.
/// </summary>
public interface IWorkPackageContextRetrievalService
{
    Task<IReadOnlyList<string>> RetrieveExistingTaskContextAsync(
        Guid projectId, IReadOnlyList<ExistingTaskInfoDto> existingTasks,
        string? extraInstructions, CancellationToken ct = default);

    Task<IReadOnlyList<string>> RetrievePendingSuggestionContextAsync(
        Guid projectId, IReadOnlyList<string> pendingSuggestionTitles,
        string? extraInstructions, CancellationToken ct = default);
}
