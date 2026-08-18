using System.Text;
using Microsoft.Extensions.Logging;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <inheritdoc cref="IWorkPackageContextRetrievalService"/>
public sealed class WorkPackageContextRetrievalService(
    IRagClient ragClient,
    RagOptions ragOptions,
    WorkPackageContextRetrievalOptions options,
    ILogger<WorkPackageContextRetrievalService> logger) : IWorkPackageContextRetrievalService
{
    public Task<IReadOnlyList<string>> RetrieveExistingTaskContextAsync(
        Guid projectId, IReadOnlyList<ExistingTaskInfoDto> existingTasks, string? extraInstructions, CancellationToken ct = default)
    {
        if (existingTasks.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var documentText = SyntheticContextDocumentFormatter.FormatExistingTasks(existingTasks);
        var question = RagPromptQuestions.BuildExistingTaskRetrievalQuestion(extraInstructions);
        return RetrieveAsync(projectId, "gorev-listesi.txt", documentText, question, "mevcut görev listesi", ct);
    }

    public Task<IReadOnlyList<string>> RetrievePendingSuggestionContextAsync(
        Guid projectId, IReadOnlyList<string> pendingSuggestionTitles, string? extraInstructions, CancellationToken ct = default)
    {
        if (pendingSuggestionTitles.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var documentText = SyntheticContextDocumentFormatter.FormatPendingSuggestionTitles(pendingSuggestionTitles);
        var question = RagPromptQuestions.BuildPendingSuggestionRetrievalQuestion(extraInstructions);
        return RetrieveAsync(projectId, "bekleyen-oneriler.txt", documentText, question, "bekleyen öneri başlıkları", ct);
    }

    // Ephemeral, single-use session per call — deliberately NEVER projectId.ToString() (the real
    // per-project document/chat RAG session, see RagDocumentSyncService/AiChatAppService). Uploading a
    // task/suggestion snapshot there would permanently pollute chat's document Q&A with this app's own
    // DB data, and RAG has no delete endpoint to undo it. A fresh GUID guarantees this session is only
    // ever asked the one synthetic question it was created for.
    private async Task<IReadOnlyList<string>> RetrieveAsync(
        Guid projectId, string syntheticFileName, string documentText, string question, string label, CancellationToken ct)
    {
        var ephemeralSessionId = $"wp-ctx-{projectId:N}-{Guid.NewGuid():N}";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(documentText);
            var upload = await ragClient.UploadDocumentAsync(ephemeralSessionId, syntheticFileName, bytes, ct);
            if (!upload.Success || string.IsNullOrWhiteSpace(upload.JobId))
            {
                logger.LogWarning(
                    "Proje {ProjectId} için {Label} RAG'e kabul edilmedi: {Message}", projectId, label, upload.Message);
                return [];
            }

            var indexed = await RagJobPoller.PollUntilDoneAsync(ragClient, ragOptions, upload.JobId, label, logger, ct);
            if (!indexed)
            {
                return [];
            }

            var answer = await ragClient.AskAsync(
                new RagAskRequest(question, ephemeralSessionId, ragOptions.DefaultMode, Model: null,
                    RetrievedContextsMode: "text", UseHistory: false),
                ct);

            if (answer is null || !answer.Success || answer.RetrievedContexts is not { Count: > 0 } contexts)
            {
                logger.LogWarning(
                    "Proje {ProjectId} için {Label} RAG bağlamı döndürmedi: {Message}", projectId, label, answer?.Message);
                return [];
            }

            return contexts
                .Select(c => c.Length > options.MaxCharsPerRetrievedContext
                    ? c[..options.MaxCharsPerRetrievedContext] + "\n[...kısaltıldı...]"
                    : c)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Proje {ProjectId} için {Label} RAG bağlamı alınamadı, bu bölüm boş bırakılıyor.", projectId, label);
            return [];
        }
    }
}
