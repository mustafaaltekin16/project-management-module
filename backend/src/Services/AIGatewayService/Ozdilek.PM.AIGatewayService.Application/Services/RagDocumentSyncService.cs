using Microsoft.Extensions.Logging;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Ensures a project's eligible documents are indexed in the external RAG service before either feature
/// (İş Paketi generation, Proje Rehberi chat) asks it a question. Stateless — every call re-reads RAG's
/// own GET /documents/list as the source of truth for "what's already indexed" rather than keeping a
/// local tracking table, so there is nothing to keep in sync if RAG's own state ever diverges.
/// </summary>
public sealed class RagDocumentSyncService(
    ITaskDocumentClient taskDocumentClient,
    IRagClient ragClient,
    RagOptions ragOptions,
    ProjectSyncLockRegistry syncLocks,
    ILogger<RagDocumentSyncService> logger) : IRagDocumentSyncService
{
    // RAG's own SUPPORTED_EXTENSIONS — checked by file EXTENSION, deliberately NOT by TaskService's
    // DocumentKind enum. ProjectDocument.KindFromFileName buckets .txt/.csv/.bmp/.tiff all into the
    // generic "File" kind (RAG supports all four) and legacy .doc into "Word" (RAG only supports .docx),
    // so filtering by Kind here would both wrongly exclude RAG-supported files and wrongly include one
    // RAG can't parse. Kind remains used elsewhere for UI display only.
    private static readonly HashSet<string> RagSupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".txt", ".xlsx", ".xls", ".pptx", ".ppt",
        ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tiff", ".csv"
    };

    /// <param name="restrictToDocumentIds">
    /// İş Paketi path: the user's selected document ids only. Chat path: null — sync every eligible
    /// project document, since chat has no per-message selection step.
    /// </param>
    public async Task<RagSyncResult> EnsureProjectDocumentsSyncedAsync(
        Guid projectId, IReadOnlyCollection<Guid>? restrictToDocumentIds, CancellationToken ct = default)
    {
        // Chat ve İş Paketi üretimi aynı proje için AYNI RAG session id'sini (projectId) paylaşıyor ve bu
        // metot stateless (her seferinde RAG'in kendi /documents/list'ini okuyup "eksik" olanı yüklüyor) —
        // iki eşzamanlı çağrı (ör. iki sekme/kullanıcı, ya da art arda gelen bir chat sorusu + üretim
        // isteği) aynı "eksik" dokümanı bağımsızca RAG'e yükleyebilirdi. Proje bazlı kilit bunu serileştirir.
        using var _ = await syncLocks.AcquireAsync(projectId, ct);

        var sessionId = projectId.ToString();

        IReadOnlyList<TaskDocumentSummary> available;
        try
        {
            available = await taskDocumentClient.ListDocumentsAsync(projectId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Proje {ProjectId} için doküman listesi TaskService'ten alınamadı, RAG senkronizasyonu atlanıyor.", projectId);
            return new RagSyncResult([], [], FullySynced: false);
        }

        var eligible = available.Where(d => RagSupportedExtensions.Contains(Path.GetExtension(d.Name)));
        if (restrictToDocumentIds is not null)
        {
            eligible = eligible.Where(d => restrictToDocumentIds.Contains(d.Id));
        }
        var eligibleList = eligible.ToList();

        IReadOnlyList<RagDocumentSummary> alreadyIndexed;
        try
        {
            alreadyIndexed = await ragClient.ListDocumentsAsync(sessionId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Proje {ProjectId} için RAG doküman listesi alınamadı, hepsi yeniden yüklenecek şekilde devam ediliyor.", projectId);
            alreadyIndexed = [];
        }
        var existingNames = alreadyIndexed.Select(d => d.FileName).ToHashSet(StringComparer.Ordinal);

        var confirmed = new List<string>(existingNames);
        var fullySynced = true;
        var pendingUploads = new List<(string FileName, string JobId)>();

        foreach (var doc in eligibleList.Where(d => !existingNames.Contains(d.Name)))
        {
            byte[]? content;
            try
            {
                content = await taskDocumentClient.GetDocumentContentAsync(projectId, doc.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Doküman {Name} indirilemedi, RAG'e yüklenmeden atlanıyor.", doc.Name);
                fullySynced = false;
                continue;
            }

            if (content is null)
            {
                logger.LogWarning("Doküman {Name} indirilemedi, RAG'e yüklenmeden atlanıyor.", doc.Name);
                fullySynced = false;
                continue;
            }

            try
            {
                var uploadResult = await ragClient.UploadDocumentAsync(sessionId, doc.Name, content, ct);
                if (!uploadResult.Success || string.IsNullOrWhiteSpace(uploadResult.JobId))
                {
                    logger.LogWarning("Doküman {Name} RAG'e kabul edilmedi: {Message}", doc.Name, uploadResult.Message);
                    fullySynced = false;
                    continue;
                }

                pendingUploads.Add((doc.Name, uploadResult.JobId));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Doküman {Name} RAG'e yüklenirken hata oluştu, atlanıyor.", doc.Name);
                fullySynced = false;
            }
        }

        foreach (var (fileName, jobId) in pendingUploads)
        {
            var done = await RagJobPoller.PollUntilDoneAsync(ragClient, ragOptions, jobId, $"Doküman {fileName}", logger, ct);
            if (done)
            {
                confirmed.Add(fileName);
            }
            else
            {
                fullySynced = false;
            }
        }

        return new RagSyncResult(confirmed, eligibleList.Select(d => d.Name).ToList(), fullySynced);
    }
}
