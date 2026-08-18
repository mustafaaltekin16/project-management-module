using Microsoft.Extensions.Logging;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.SharedKernel.Exceptions;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Backs the "Proje Rehberi" chat tab with real, document-aware answers from the external RAG service —
/// replaces what used to be a pure client-side keyword matcher (ProjectGuideResponseService, now deleted
/// from the Angular app). RAG only knows about indexed DOCUMENTS, not this app's task/note data model —
/// see the plan's "task/note context gap" decision: the frontend's suggested quick-questions were
/// rephrased to be document-oriented rather than trying to smuggle task/note data into RAG's question text.
/// </summary>
public sealed class AiChatAppService(
    IRagDocumentSyncService ragDocumentSyncService,
    IRagClient ragClient,
    RagOptions ragOptions,
    ILogger<AiChatAppService> logger)
{
    private const int MinQuestionLength = 3;
    private const int MaxQuestionLength = 5000;

    public async Task<AskProjectGuideResponseDto> AskAsync(AskProjectGuideRequestDto request, CancellationToken ct = default)
    {
        var question = request.Question?.Trim() ?? string.Empty;
        if (question.Length is < MinQuestionLength or > MaxQuestionLength)
        {
            throw new DomainException($"Soru {MinQuestionLength} ile {MaxQuestionLength} karakter arasında olmalı.");
        }

        try
        {
            // Chat has no per-message document selection like İş Paketi does — sync everything eligible.
            await ragDocumentSyncService.EnsureProjectDocumentsSyncedAsync(request.ProjectId, restrictToDocumentIds: null, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Proje {ProjectId} dokümanları RAG ile senkronize edilemedi, mevcut indeksle devam ediliyor.", request.ProjectId);
        }

        RagAnswer? answer;
        try
        {
            answer = await ragClient.AskAsync(
                new RagAskRequest(
                    question, request.ProjectId.ToString(), ragOptions.DefaultMode,
                    Model: null, RetrievedContextsMode: "text", UseHistory: ragOptions.DefaultUseHistory),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RAG /qa/ask çağrısı proje {ProjectId} için başarısız oldu.", request.ProjectId);
            return new AskProjectGuideResponseDto("Şu anda dokümanlarınıza erişemiyorum, lütfen daha sonra tekrar deneyin.", [], UsedRealDocumentContext: false);
        }

        if (answer is null || !answer.Success || string.IsNullOrWhiteSpace(answer.Answer))
        {
            logger.LogWarning("RAG proje {ProjectId} için başarısız/boş yanıt döndürdü: {Message}", request.ProjectId, answer?.Message);
            var fallback = answer?.Message is { Length: > 0 } message ? message : "Bu soruya şu an yanıt veremiyorum.";
            return new AskProjectGuideResponseDto(fallback, [], UsedRealDocumentContext: false);
        }

        return new AskProjectGuideResponseDto(
            answer.Answer, answer.Sources ?? [], UsedRealDocumentContext: answer.RetrievedContexts is { Count: > 0 });
    }
}
