using Ozdilek.PM.AIGatewayService.Application.Dtos;

namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

/// <summary>
/// HTTP client for the external RAG (Retrieval Augmented Generation) service — a separately-deployed
/// FastAPI backend (Weaviate + Haystack + vLLM/Qwen3-VL), not part of this module's own microservices.
/// Unlike ITaskDocumentClient/IProjectInfoClient, this does NOT forward this app's own JWT — RAG has its
/// own, unrelated optional API-key auth (see RagOptions), so no BearerTokenForwardingHandler is attached.
/// </summary>
public interface IRagClient
{
    /// <summary>POST /documents/upload — fire-and-forget; returns immediately with a job_id to poll.</summary>
    Task<RagDocumentUploadResult> UploadDocumentAsync(
        string sessionId, string fileName, byte[] content, CancellationToken ct = default);

    /// <summary>GET /documents/jobs/{job_id} — null if the job is unknown or the call itself failed.</summary>
    Task<RagJobStatus?> GetJobStatusAsync(string jobId, CancellationToken ct = default);

    /// <summary>GET /documents/list?session_id= — contract guarantees an empty list, never 404.</summary>
    Task<IReadOnlyList<RagDocumentSummary>> ListDocumentsAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// POST /qa/ask. The RAG contract returns HTTP 200 even on internal failure (Success:false) — this
    /// method does NOT throw for that case, it returns the answer as-is so the caller can branch on
    /// Success/Answer/RetrievedContexts. Returns null only if the HTTP call itself failed.
    /// </summary>
    Task<RagAnswer?> AskAsync(RagAskRequest request, CancellationToken ct = default);
}
