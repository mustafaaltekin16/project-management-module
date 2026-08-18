using Ozdilek.PM.AIGatewayService.Application.Dtos;

namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

/// <summary>
/// Ensures a project's eligible documents are indexed in the external RAG service before either feature
/// (İş Paketi generation, Proje Rehberi chat) asks it a question — see RagDocumentSyncService for the
/// implementation. Behind an interface (unlike the other App-layer services in this project, which are
/// concrete top-level entry points) specifically so it can be mocked when unit-testing its two
/// consumers, AiSuggestionAppService and AiChatAppService.
/// </summary>
public interface IRagDocumentSyncService
{
    /// <param name="restrictToDocumentIds">
    /// İş Paketi path: the user's selected document ids only. Chat path: null — sync every eligible
    /// project document, since chat has no per-message selection step.
    /// </param>
    Task<RagSyncResult> EnsureProjectDocumentsSyncedAsync(
        Guid projectId, IReadOnlyCollection<Guid>? restrictToDocumentIds, CancellationToken ct = default);
}
