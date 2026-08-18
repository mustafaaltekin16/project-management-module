using System.Text;
using Microsoft.Extensions.Logging;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Application.Services;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Providers;

/// <summary>
/// Generates work packages using the same RAG service already used for document Q&amp;A/retrieval
/// (Weaviate + Haystack + vLLM/Qwen3-VL — see IRagClient) instead of a paid external LLM API. RAG's
/// /qa/ask only answers questions about documents already indexed in a session, so the (already fully
/// assembled, PII-redacted) work-package prompt is itself uploaded as a throwaway synthetic document to
/// a fresh, single-use session — the same "upload a synthetic .txt, poll, then ask" pattern
/// WorkPackageContextRetrievalService already uses for existing-task/pending-suggestion context, just
/// applied to the final generation step instead of a context-gathering sub-step.
/// </summary>
public sealed class RagLlmProvider(IRagClient ragClient, RagOptions ragOptions, ILogger<RagLlmProvider> logger) : ILlmProvider
{
    private const string SyntheticFileName = "is-paketi-talimatlari.txt";
    private const string GenerationQuestion =
        "Yukarıdaki talimatları dikkatlice ve eksiksiz uygula. Yanıt SADECE JSON dizisi olsun; açıklama, " +
        "markdown veya kaynak alıntısı ekleme. Her nesnede title ve department dolu metin olmalı. " +
        "activities bir nesne dizisi olmalı ve her faaliyet {\"title\":\"...\",\"effortHours\":8} " +
        "biçiminde, title alanı dolu olmalı. Zorunlu alanları atlama.";

    public string Name => "RAG";

    public async Task<string> GenerateWorkPackagesJsonAsync(string prompt, CancellationToken ct = default)
    {
        var sessionId = $"wp-gen-{Guid.NewGuid():N}";

        var upload = await ragClient.UploadDocumentAsync(sessionId, SyntheticFileName, Encoding.UTF8.GetBytes(prompt), ct);
        if (!upload.Success || string.IsNullOrWhiteSpace(upload.JobId))
        {
            throw new InvalidOperationException($"RAG iş paketi talimatlarını kabul etmedi: {upload.Message}");
        }

        var indexed = await RagJobPoller.PollUntilDoneAsync(ragClient, ragOptions, upload.JobId, "İş paketi talimatları", logger, ct);
        if (!indexed)
        {
            throw new InvalidOperationException("RAG iş paketi talimatlarını indeksleyemedi (bkz. önceki uyarı logu).");
        }

        var answer = await ragClient.AskAsync(
            new RagAskRequest(GenerationQuestion, sessionId, ragOptions.DefaultMode, Model: null, RetrievedContextsMode: "text", UseHistory: false),
            ct);

        if (answer is null || !answer.Success || string.IsNullOrWhiteSpace(answer.Answer))
        {
            throw new InvalidOperationException($"RAG iş paketi üretemedi: {answer?.Message ?? "yanıt alınamadı"}");
        }

        return answer.Answer;
    }
}
