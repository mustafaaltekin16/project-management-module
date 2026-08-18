namespace Ozdilek.PM.AIGatewayService.Application.Dtos;

/// <summary>Raw shape of the RAG service's 202 response to POST /documents/upload.</summary>
public sealed record RagDocumentUploadResult(
    bool Success, string Message, string JobId, string Status, string? DocumentId, string? FileName);

/// <summary>Raw shape of GET /documents/jobs/{job_id} — polled until Status is "done" or "failed".</summary>
public sealed record RagJobStatus(
    string JobId, string SessionId, string Status, int TotalFiles, int ProcessedFiles, int FailedFiles, double ProgressPct);

/// <summary>One element of GET /documents/list's `documents` array.</summary>
public sealed record RagDocumentSummary(string FileName, string FileType, double SizeMb, int ChunksCount);

/// <summary>
/// Request body for POST /qa/ask. SessionId doubles as the RAG service's tenant key — this module uses
/// projectId.ToString() (see RagDocumentSyncService) so one project's documents are only ever retrievable
/// under that project's own questions.
/// </summary>
public sealed record RagAskRequest(
    string Question, string SessionId, string Mode, string? Model, string RetrievedContextsMode, bool UseHistory);

/// <summary>
/// Raw shape of POST /qa/ask's response. The RAG contract returns HTTP 200 even when Success is false
/// (no exception to catch) — callers must branch on Success themselves, see IRagClient.AskAsync.
/// </summary>
public sealed record RagAnswer(
    bool Success, string Message, string? Answer, string? Thinking,
    IReadOnlyList<string>? Sources, IReadOnlyList<string>? RetrievedContexts);

/// <summary>
/// Result of <see cref="Services.RagDocumentSyncService.EnsureProjectDocumentsSyncedAsync"/> — which of a
/// project's eligible documents are confirmed indexed in RAG right now vs. which were merely attempted
/// (upload/poll may have failed or timed out; see FullySynced).
/// </summary>
public sealed record RagSyncResult(
    IReadOnlyList<string> ConfirmedIndexedFileNames, IReadOnlyList<string> AttemptedFileNames, bool FullySynced);
