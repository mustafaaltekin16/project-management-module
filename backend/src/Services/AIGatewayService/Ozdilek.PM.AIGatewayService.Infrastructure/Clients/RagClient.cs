using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Clients;

// Raw shapes matching the RAG service's actual (snake_case, Python/FastAPI) JSON — kept internal and
// separate from this app's own Rag*Dto records (Application/Dtos/RagDtos.cs) so a future wire-format
// change on RAG's side only touches this file, never the Application layer's contract.
internal sealed class RagUploadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public string? Status { get; set; }
    public string? DocumentId { get; set; }
    public string? Filename { get; set; }
}

internal sealed class RagJobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FailedFiles { get; set; }
    public double ProgressPct { get; set; }
}

internal sealed class RagDocumentListResponse
{
    public bool Success { get; set; }
    public int TotalDocuments { get; set; }
    public List<RagDocumentListItem>? Documents { get; set; }
}

internal sealed class RagDocumentListItem
{
    public string Filename { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public double SizeMb { get; set; }
    public int ChunksCount { get; set; }
}

internal sealed class RagAskRequestBody
{
    public string Question { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Mode { get; set; } = "strict";
    public string? Model { get; set; }
    public string RetrievedContextsMode { get; set; } = "text";
    public bool UseHistory { get; set; }
}

internal sealed class RagAskResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public string? Thinking { get; set; }
    public List<string>? Sources { get; set; }
    public List<string>? RetrievedContexts { get; set; }
}

public sealed class RagClient(HttpClient httpClient) : IRagClient
{
    // RAG's JSON is snake_case (Python/FastAPI) — one shared naming strategy for both serializing
    // outgoing request bodies and deserializing incoming responses, instead of [JsonProperty] on every field.
    private static readonly JsonSerializerSettings SnakeCaseSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    public async Task<RagDocumentUploadResult> UploadDocumentAsync(
        string sessionId, string fileName, byte[] content, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        // MultipartFormDataContent.Add(content, name, fileName) RFC-5987-encodes non-ASCII filenames
        // (emits only filename*=utf-8''... , no plain filename=) — confirmed live against RunPod that RAG's
        // multipart parser only reads the plain parameter, so any Turkish filename (ö/ş/ç/ğ/ı/ü, e.g.
        // "Özveri...docx") silently loses its filename server-side, RAG then rejects it as
        // "Unsupported file format: " (empty extension). Setting the header manually with a plain quoted
        // UTF-8 filename matches what RAG's parser (and curl, verified working) actually expect.
        fileContent.Headers.TryAddWithoutValidation(
            "Content-Disposition", $"form-data; name=\"file\"; filename=\"{EscapeHeaderValue(fileName)}\"");
        form.Add(fileContent);
        form.Add(new StringContent(sessionId), "session_id");

        using var response = await httpClient.PostAsync("/documents/upload", form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonConvert.DeserializeObject<RagUploadResponse>(body, SnakeCaseSettings)
            ?? new RagUploadResponse { Success = false, Message = "RAG servisinden boş yanıt alındı." };

        // FastAPI validation errors (HTTP 4xx) come back as {"detail": ...}, a shape RagUploadResponse
        // doesn't map — without this fallback Message stays empty and the real failure reason is lost.
        var message = !response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(parsed.Message) ? body : parsed.Message;

        return new RagDocumentUploadResult(
            parsed.Success, message, parsed.JobId ?? string.Empty, parsed.Status ?? string.Empty,
            parsed.DocumentId, parsed.Filename);
    }

    public async Task<RagJobStatus?> GetJobStatusAsync(string jobId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/documents/jobs/{jobId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonConvert.DeserializeObject<RagJobStatusResponse>(body, SnakeCaseSettings);
        return parsed is null
            ? null
            : new RagJobStatus(
                parsed.JobId, parsed.SessionId, parsed.Status, parsed.TotalFiles, parsed.ProcessedFiles,
                parsed.FailedFiles, parsed.ProgressPct);
    }

    public async Task<IReadOnlyList<RagDocumentSummary>> ListDocumentsAsync(string sessionId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/documents/list?session_id={Uri.EscapeDataString(sessionId)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonConvert.DeserializeObject<RagDocumentListResponse>(body, SnakeCaseSettings);
        return parsed?.Documents?
            .Select(d => new RagDocumentSummary(d.Filename, d.FileType, d.SizeMb, d.ChunksCount))
            .ToList() ?? [];
    }

    public async Task<RagAnswer?> AskAsync(RagAskRequest request, CancellationToken ct = default)
    {
        var requestBody = new RagAskRequestBody
        {
            Question = request.Question,
            SessionId = request.SessionId,
            Mode = request.Mode,
            Model = request.Model,
            RetrievedContextsMode = request.RetrievedContextsMode,
            UseHistory = request.UseHistory
        };
        var json = JsonConvert.SerializeObject(requestBody, SnakeCaseSettings);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync("/qa/ask", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            // The RAG contract documents /qa/ask as always 200 (even success:false) — a non-2xx here
            // means something more fundamental (network, 5xx, rate limit) rather than a normal "no answer".
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonConvert.DeserializeObject<RagAskResponse>(body, SnakeCaseSettings);
        return parsed is null
            ? null
            : new RagAnswer(parsed.Success, parsed.Message, parsed.Answer, parsed.Thinking, parsed.Sources, parsed.RetrievedContexts);
    }

    private static string EscapeHeaderValue(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", string.Empty).Replace("\n", string.Empty);
}
