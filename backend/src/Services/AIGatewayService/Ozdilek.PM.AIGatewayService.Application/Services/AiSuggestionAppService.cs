using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Domain;
using Ozdilek.PM.Contracts.Events;
using Ozdilek.PM.SharedKernel.Events;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.SharedKernel.Security;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

public sealed class AiSuggestionAppService(
    IAiSuggestionRequestRepository requests,
    IPromptTemplateRepository templates,
    IProjectInfoClient projectInfoClient,
    IRagDocumentSyncService ragDocumentSyncService,
    IRagClient ragClient,
    RagOptions ragOptions,
    ITaskInfoClient taskInfoClient,
    IWorkPackageContextRetrievalService workPackageContextRetrievalService,
    WorkPackageContextRetrievalOptions workPackageContextRetrievalOptions,
    DocumentExcerptOptions excerptOptions,
    ILlmProvider llmProvider,
    IPromptAuditLogger auditLogger,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    WorkPackageGenerationLockRegistry generationLocks,
    ILogger<AiSuggestionAppService> logger)
{
    public async Task<AiSuggestionRequestDto> GenerateAsync(GenerateSuggestionsRequest request, CancellationToken ct = default)
    {
        // Aynı proje için eşzamanlı iki "İş Paketi Çıkart" çağrısı (ör. iki sekme/kullanıcı) titlesToSkip'i
        // birbirinden habersiz hesaplayıp örtüşen/neredeyse-aynı önerileri paralel üretebilirdi — proje
        // bazlı kilit ikinciyi birincinin bitmesini bekletir (sessizce çakışan öneriler üretmek yerine).
        using var _ = await generationLocks.AcquireAsync(request.ProjectId, ct);

        var project = await projectInfoClient.GetProjectAsync(request.ProjectId, ct)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var template = await templates.GetByProjectTypeAsync(project.Type, ct)
            ?? PromptBuilder.DefaultTemplateFor(project.Type);

        var assembledPrompt = PromptBuilder.Build(template, project, request.ExtraInstructions);
        assembledPrompt = PromptBuilder.AppendDepartmentList(assembledPrompt, project.Departments);

        var (excerpts, usedDocumentNames) = await CollectRagDocumentExcerptsAsync(request, ct);
        assembledPrompt = PromptBuilder.AppendDocumentExcerpts(assembledPrompt, excerpts);

        // Non-fatal: TaskService kısa süreliğine erişilemezse üretim mevcut görev bağlamı olmadan devam
        // eder (tekrar önleme/sıralama gerekçesi zayıflar ama üretim tamamen başarısız olmaz).
        IReadOnlyList<ExistingTaskInfoDto> existingTasks;
        try
        {
            existingTasks = await taskInfoClient.ListExistingTasksAsync(request.ProjectId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Projenin mevcut görevleri alınamadı, üretim bağlamsız devam ediyor.");
            existingTasks = [];
        }

        // SADECE hâlâ karar bekleyen (Pending) önerilerin başlıkları — LLM'in "mevcut görevler"
        // listesinde göremediği TEK gerçek boşluğu kapatır: onay bekleyen bir öneri henüz gerçek görev
        // olmadığı için ListExistingTasksAsync'te görünmez, bu yüzden model aynı fikri fark etmeden
        // ikinci kez üretebilir. Reddedilmiş ya da onaylandıktan sonra arşivlenmiş öneriler BİLEREK
        // bu listeye dahil edilmez — kullanıcı bir öneriyi reddettiğinde ya da onayladığı görevi
        // arşivlediğinde bu açık bir "bu fikri istemiyorum / artık planda değil" kararıdır; bu kararı
        // sonsuza dek hatırlayıp aynı fikrin bir daha hiç önerilememesine izin vermek (ör. kullanıcı
        // eski önerileri temizleyip projeyi sıfırdan yeniden değerlendirmek istediğinde) modelin hiç
        // yeni öneri üretememesine yol açar.
        var priorRequests = await requests.ListByProjectAsync(request.ProjectId, ct);
        var pendingSuggestionTitles = priorRequests
            .SelectMany(r => r.Items)
            .Where(i => i.Decision == SuggestionItemDecision.Pending)
            .Select(i => i.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // RAG'e taşındı: prompt'a artık existingTasks/pendingSuggestionTitles'ın TAMAMI değil, RAG'in
        // semantik olarak ilgili bulduğu bir alt kümesi yazılıyor (workPackageContextRetrievalService asla
        // exception fırlatmaz, herhangi bir aşamada başarısızlıkta [] döner — o durumda ilgili bölüm no-op
        // olur). KRİTİK: existingTasks ve pendingSuggestionTitles'ın TAM halleri aşağıdaki titlesToSkip
        // deterministik tekrar filtresi için DEĞİŞMEDEN kullanılmaya devam ediyor — bu exact-match filtre
        // RAG'in alt kümesine ASLA bağımlı hale gelmemeli (aksi halde birebir aynı başlıklı bir öneri
        // filtreden kaçabilir).
        //
        // İSTİSNA: RAG'in "ilgili alt küme" retrieval'i az sayıda görev/öneri için fayda değil zarar
        // veriyor — küçük bir listede LLM'e hiç gösterilmeyen bir görev, o görevle yeni öneri arasına
        // (insertAfterTaskTitle) yerleştirme yapılamamasına, hatta aynı işin farklı bir başlıkla tekrar
        // önerilmesine yol açar (bkz. 2026-08-05 canlı test: 7 görevlik bir projede RAG'in alt kümesi
        // "UI/UX Tasarım" görevini göstermeyince model neredeyse birebir aynı işi "UI UX Tasarım" diye
        // tekrar önerdi). Liste FullListThreshold'un altındaysa RAG'e hiç gidilmez, TAM liste doğrudan
        // formatlanıp gösterilir.
        var usesFullExistingTaskList = existingTasks.Count <= workPackageContextRetrievalOptions.FullListThreshold;
        var existingTaskContextsTask = usesFullExistingTaskList
            ? Task.FromResult<IReadOnlyList<string>>(SyntheticContextDocumentFormatter.FormatExistingTasksAsIndividualContexts(existingTasks))
            : workPackageContextRetrievalService.RetrieveExistingTaskContextAsync(
                request.ProjectId, existingTasks, request.ExtraInstructions, ct);
        var pendingSuggestionContextsTask = workPackageContextRetrievalService.RetrievePendingSuggestionContextAsync(
            request.ProjectId, pendingSuggestionTitles, request.ExtraInstructions, ct);
        await Task.WhenAll(existingTaskContextsTask, pendingSuggestionContextsTask);

        assembledPrompt = PromptBuilder.AppendExistingTasksList(
            assembledPrompt, existingTaskContextsTask.Result, isCompleteList: usesFullExistingTaskList);
        assembledPrompt = PromptBuilder.AppendPendingSuggestionTitles(assembledPrompt, pendingSuggestionContextsTask.Result);

        // Defense in depth: redact KVKK-sensitive data from the FULL assembled prompt (which includes
        // server-fetched project data and any selected document text) before it ever reaches the provider.
        var detected = PiiRegexFilter.Detect(assembledPrompt);
        var redactedPrompt = PiiRegexFilter.Redact(assembledPrompt);

        await auditLogger.LogAsync(request.ProjectId, llmProvider.Name, redactedPrompt,
            detected.Select(m => m.Category).Distinct().ToList(), ct);

        var (suggestions, possiblyIncomplete) = await GenerateAndParseSuggestionsAsync(redactedPrompt, ct);

        // LLM talimata (yukarıdaki "TEKRAR önerme"/"AYNEN üretme") her zaman uymayabilir — özellikle
        // ucuz/küçük modellerde. Bu yüzden aynı başlıkla gelen önerileri modelin insafına bırakmadan,
        // deterministik olarak burada eleriz: hem gerçek aktif görevlerle hem de bu projenin hâlâ karar
        // bekleyen önerileriyle eşleşen bir öneri asla kullanıcıya gösterilmez. Reddedilmiş/arşivlenmiş
        // geçmiş BİLEREK bu filtrenin dışında (yukarıdaki pendingSuggestionTitles yorumuna bkz.) — aksi
        // halde kullanıcı bir fikri reddettikten sonra o fikir projenin geri kalan ömrü boyunca bir daha
        // hiç önerilemez.
        //
        // Eşleşme NORMALIZE edilmiş başlık üzerinden yapılır (bkz. NormalizeTitle) — birebir string eşleşme
        // yeterli değil: canlıda "UI/UX Tasarım" onaylandıktan sonra model "UI UX Tasarım" diye (sadece "/"
        // farkıyla) neredeyse aynı işi tekrar önerebiliyordu, salt OrdinalIgnoreCase bunu yakalayamıyordu.
        // titlesToSkip'e kabul edilen her öneri EKLENİR de (aşağıdaki foreach içinde) — böylece TEK bir
        // üretim yanıtı kendi içinde iki neredeyse-aynı öneri döndürürse ikincisi de elenir.
        var titlesToSkip = existingTasks.Select(t => t.Title)
            .Concat(pendingSuggestionTitles)
            .Select(NormalizeTitle)
            .ToHashSet(StringComparer.Ordinal);

        // Modelin "insertAfterTaskTitle" için SADECE existingTasks'teki gerçek başlıkları kullanması
        // gerekiyor (bkz. PromptBuilder.AppendExistingTasksList), ama canlıda model bunun yerine
        // "[Doküman: ... (section: X)]" kaynak etiketlerini (bkz. CollectRagDocumentExcerptsAsync'in
        // fileName'i) ya da JSON alan adının kendisini ("insertAfterTaskTitle" literal metni) bu alanlara
        // yazabiliyor — ikisi de görsel olarak bir başlığa benziyor ama hiçbiri gerçek bir görev değil.
        // Deterministik olarak temizlenir: insertAfterTaskTitle gerçek bir existingTasks başlığıyla
        // eşleşmiyorsa null'a çekilir; sequenceNote de literal alan adına ya da (artık geçersiz olan)
        // insertAfterTaskTitle değerine birebir eşitse null'a çekilir. Temizlenen bir öneri sadece
        // "nereye oturduğu belirsiz" hâle döner (bkz. AppendExistingTasksList'teki isAtProjectStart/null
        // ayrımı) — reddedilmez, sadece yanlış/kafa karıştırıcı sıralama bilgisi kullanıcıya gösterilmez.
        var existingTaskTitles = existingTasks.Select(t => t.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var suggestionRequest = AiSuggestionRequest.Create(
            request.ProjectId, project.Type, request.ExtraInstructions, redactedPrompt, llmProvider.Name,
            usedDocumentNames.Count > 0 ? string.Join(", ", usedDocumentNames) : null,
            usedRealDocumentContext: excerpts.Count > 0);

        foreach (var suggestion in suggestions)
        {
            // GenerateAndParseSuggestionsAsync only returns suggestions that passed required-field
            // validation. Keep an explicit guard at the persistence boundary as defense in depth.
            if (string.IsNullOrWhiteSpace(suggestion.Title) || string.IsNullOrWhiteSpace(suggestion.Department))
            {
                throw new DomainException("Yapay zekâ önerisinde zorunlu başlık veya departman bilgisi eksik.");
            }

            var normalizedTitle = NormalizeTitle(suggestion.Title);
            if (!titlesToSkip.Add(normalizedTitle))
            {
                logger.LogWarning(
                    "AI önerisi \"{Title}\" mevcut/daha önce üretilmiş (ya da bu yanıttaki başka bir öneriyle) " +
                    "neredeyse birebir eşleştiği için atlandı.", suggestion.Title);
                continue;
            }

            var insertAfterTaskTitle = suggestion.InsertAfterTaskTitle;
            if (insertAfterTaskTitle is not null && !existingTaskTitles.Contains(insertAfterTaskTitle))
            {
                logger.LogWarning(
                    "AI önerisi \"{Title}\" var olmayan bir göreve (\"{InsertAfterTaskTitle}\") bağlanmak " +
                    "istedi — muhtemelen bir doküman bölüm etiketiyle karıştırıldı, sıralama bağlantısı " +
                    "temizlendi.", suggestion.Title, insertAfterTaskTitle);
                insertAfterTaskTitle = null;
            }

            var sequenceNote = suggestion.SequenceNote;
            if (sequenceNote is not null &&
                (string.Equals(sequenceNote, "insertAfterTaskTitle", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(sequenceNote, suggestion.InsertAfterTaskTitle, StringComparison.Ordinal)))
            {
                logger.LogWarning(
                    "AI önerisi \"{Title}\" için sequenceNote alanı geçersiz bir değer içeriyordu (\"{SequenceNote}\"), temizlendi.",
                    suggestion.Title, sequenceNote);
                sequenceNote = null;
            }

            // EffortHours/IsAtProjectStart, ToObject<RawWorkPackageSuggestion>() sırasında tüm öneriyi
            // (sağlam title/department/description alanlarıyla birlikte) atmamak için nullable tutuluyor
            // (bkz. RawWorkPackageSuggestion) — model talimata rağmen null/geçersiz bir değer yazarsa
            // burada güvenli bir varsayılana düşülür: 0, kullanıcıya "AI bir süre tahmini vermedi" olarak
            // görünür (elle düzeltilebilir); false zaten IsAtProjectStart'ın "model belirsiz kaldı" anlamına
            // gelen mevcut varsayılanıyla birebir aynıdır (bkz. AiSuggestionItem.IsAtProjectStart yorumu).
            var item = suggestionRequest.AddItem(
                suggestion.Title, suggestion.Department, suggestion.EffortHours ?? 0, suggestion.SourceDocument,
                suggestion.Description, sequenceNote, insertAfterTaskTitle,
                suggestion.SequenceRank, suggestion.IsAtProjectStart ?? false);
            foreach (var activity in suggestion.Activities ?? [])
            {
                if (string.IsNullOrWhiteSpace(activity.Title))
                {
                    throw new DomainException("Yapay zekâ önerisindeki faaliyet başlığı eksik.");
                }

                item.AddActivity(activity.Title, activity.EffortHours);
            }
        }

        await requests.AddAsync(suggestionRequest, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDto(suggestionRequest, possiblyIncomplete);
    }

    // Never lets a document/RAG problem fail the whole generation — any failure here just means the
    // prompt proceeds without document context (same resilience philosophy as ListExistingTasksAsync's
    // catch below). Replaces the old direct PdfPig/DocumentFormat.OpenXml extraction (see
    // RagDocumentSyncService for why eligibility now checks file extension, not TaskService's Kind):
    // instead of blindly truncating a document's first N characters, this asks the RAG service a
    // targeted question and uses whatever it semantically retrieves as relevant.
    private async Task<(List<DocumentExcerpt> Excerpts, List<string> UsedNames)> CollectRagDocumentExcerptsAsync(
        GenerateSuggestionsRequest request, CancellationToken ct)
    {
        if (request.SelectedDocumentIds is not { Count: > 0 } selectedIds)
        {
            return ([], []);
        }

        try
        {
            var sync = await ragDocumentSyncService.EnsureProjectDocumentsSyncedAsync(request.ProjectId, selectedIds, ct);

            var question = RagPromptQuestions.BuildWorkPackageRetrievalQuestion(request.ExtraInstructions);
            // UseHistory: false is deliberate, not just the option default — chat and İş Paketi generation
            // share one RAG session per project, so folding conversation history into this synthetic
            // retrieval question would let unrelated prior chat turns silently steer work-package generation.
            var answer = await ragClient.AskAsync(
                new RagAskRequest(question, request.ProjectId.ToString(), ragOptions.DefaultMode,
                    Model: null, RetrievedContextsMode: "text", UseHistory: false),
                ct);

            if (answer is null || !answer.Success || answer.RetrievedContexts is not { Count: > 0 } contexts)
            {
                logger.LogWarning(
                    "RAG proje {ProjectId} için iş paketi bağlamı döndürmedi: {Message}", request.ProjectId, answer?.Message);
                return ([], sync.ConfirmedIndexedFileNames.ToList());
            }

            var fileName = answer.Sources is { Count: > 0 } sources
                ? string.Join(", ", sources)
                : "RAG Alınan Bağlam";
            var text = string.Join("\n\n---\n\n", contexts);
            var truncated = text.Length > excerptOptions.MaxCharsPerDocument
                ? text[..excerptOptions.MaxCharsPerDocument] + "\n[...kısaltıldı...]"
                : text;

            var usedNames = (answer.Sources is { Count: > 0 } ? answer.Sources : sync.ConfirmedIndexedFileNames).ToList();
            return ([new DocumentExcerpt(fileName, truncated)], usedNames);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Proje {ProjectId} için RAG doküman bağlamı alınamadı, üretim bağlamsız devam ediyor.", request.ProjectId);
            return ([], []);
        }
    }

    public async Task<List<AiSuggestionRequestDto>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var result = await requests.ListByProjectAsync(projectId, ct);
        return result.Select(r => ToDto(r)).ToList();
    }

    public async Task<AiSuggestionRequestDto> ApproveItemAsync(Guid requestId, Guid itemId, string approvedByUserId, CancellationToken ct = default)
    {
        var suggestionRequest = await requests.GetByIdAsync(requestId, ct) ?? throw new NotFoundException("Öneri isteği bulunamadı.");
        var item = suggestionRequest.ApproveItem(itemId);
        await unitOfWork.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(new WorkPackageApprovedEvent
        {
            SuggestionRequestId = suggestionRequest.Id,
            ProjectId = suggestionRequest.ProjectId,
            ApprovedByUserId = approvedByUserId,
            ApprovedAtUtc = DateTimeOffset.UtcNow,
            Items =
            [
                new WorkPackageItem
                {
                    SuggestionItemId = item.Id,
                    Title = item.Title,
                    Department = item.Department,
                    EffortHours = item.EffortHours,
                    SourceDocument = item.SourceDocument,
                    Description = item.Description,
                    InsertAfterTaskTitle = item.InsertAfterTaskTitle,
                    Activities = item.Activities
                        .Select(a => new WorkPackageActivity { Title = a.Title, EffortHours = a.EffortHours })
                        .ToList()
                }
            ]
        }, ct);

        return ToDto(suggestionRequest);
    }

    public async Task<AiSuggestionRequestDto> RejectItemAsync(Guid requestId, Guid itemId, CancellationToken ct = default)
    {
        var suggestionRequest = await requests.GetByIdAsync(requestId, ct) ?? throw new NotFoundException("Öneri isteği bulunamadı.");
        suggestionRequest.RejectItem(itemId);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(suggestionRequest);
    }

    // Kalıcı çözüm: yamalarla her bozulma türünü tek tek tolere etmek yerine, RAG sağlayıcısı geçici bir
    // hata verirse (ör. RunPod pod'u yeniden başladı, indeksleme zaman aşımına uğradı, guardrails reddetti
    // — bkz. aşağıdaki ilk catch) YA DA LLM'in yanıtı ayrıştırılamazsa/hiçbir kullanılabilir öneri
    // çıkaramazsa modele YENİDEN sorulur (bu tür arızalar genelde geçicidir, aynı isteğin ikinci denemesi
    // çoğunlukla düzgün gelir). Sadece son denemede hâlâ başarısızsa hata kullanıcıya yansıtılır. Gerçekten
    // boş bir yanıt (model haklı olarak "önerecek bir şey yok" dediğinde) tekrar denemeyi TETİKLEMEZ — bu
    // durum bir hata değildir.
    private const int MaxGenerationAttempts = 3;

    private sealed record SuggestionParseResult(
        List<RawWorkPackageSuggestion> Suggestions,
        int TotalElementsSeen,
        int SchemaOrRequiredFieldFailures,
        IReadOnlyCollection<string> MissingRequiredFields);

    private sealed record SuggestionConversionResult(
        List<RawWorkPackageSuggestion> Suggestions,
        int SchemaOrRequiredFieldFailures,
        IReadOnlyCollection<string> MissingRequiredFields);

    private async Task<(List<RawWorkPackageSuggestion> Suggestions, bool PossiblyIncomplete)> GenerateAndParseSuggestionsAsync(
        string prompt, CancellationToken ct)
    {
        var collectedSuggestions = new List<RawWorkPackageSuggestion>();
        var collectedTitles = new HashSet<string>(StringComparer.Ordinal);
        var attemptPrompt = prompt;

        for (var attempt = 1; attempt <= MaxGenerationAttempts; attempt++)
        {
            var isLastAttempt = attempt == MaxGenerationAttempts;

            string rawJson;
            try
            {
                rawJson = await llmProvider.GenerateWorkPackagesJsonAsync(attemptPrompt, ct);
            }
            catch (Exception ex) when (IsRetryableProviderFailure(ex, ct))
            {
                // RagLlmProvider, RAG'e yükleme/indeksleme/soru-sorma adımlarından biri başarısız olduğunda
                // InvalidOperationException fırlatır; gerçek bir ağ/timeout sorunu ise HttpRequestException/
                // TaskCanceledException sızabilir (RagClient'ın kendi HttpClient'ı ham fırlatır). Eskiden bu
                // çağrı retry döngüsünün DIŞINDAydı, yani tek seferlik bir RAG/altyapı aksaklığı (JSON
                // bozulmasından çok daha sık yaşanan bir arıza sınıfı) hiç tekrar denenmeden tüm üretimi
                // anında düşürüyordu.
                if (isLastAttempt)
                {
                    if (collectedSuggestions.Count > 0)
                    {
                        logger.LogWarning(
                            ex,
                            "Son üretim denemesinde RAG yanıt vermedi; önceki denemelerden doğrulanmış {Count} öneri korunarak eksik işaretleniyor.",
                            collectedSuggestions.Count);
                        return (collectedSuggestions, true);
                    }

                    // Kullanıcıya generic "Beklenmeyen bir hata oluştu" yerine ayırt edici bir mesaj
                    // göstermek için DomainException'a (400 + anlamlı mesaj) çeviriyoruz — ham istisna
                    // zaten server logunda (aşağıdaki LogWarning ve provider'ın kendi loglarında) duruyor.
                    throw new DomainException(
                        $"Yapay zekâ servisine (RAG) şu anda ulaşılamıyor, lütfen birazdan tekrar deneyin: {ex.Message}");
                }

                logger.LogWarning(
                    ex, "RAG sağlayıcısı {Attempt}/{Max}. denemede yanıt veremedi, tekrar deneniyor.",
                    attempt, MaxGenerationAttempts);
                continue;
            }

            SuggestionParseResult parsed;
            try
            {
                parsed = ParseSuggestions(rawJson);
            }
            catch (DomainException ex)
            {
                if (isLastAttempt)
                {
                    if (collectedSuggestions.Count > 0)
                    {
                        logger.LogWarning(
                            ex,
                            "Son yanıt ayrıştırılamadı; önceki denemelerden doğrulanmış {Count} öneri korunarak eksik işaretleniyor.",
                            collectedSuggestions.Count);
                        return (collectedSuggestions, true);
                    }

                    throw new DomainException(
                        "Yapay zekâ geçerli iş paketi biçimi üretemedi. Lütfen tekrar deneyin.");
                }

                logger.LogWarning(
                    "Yapay zekâ yanıtı {Attempt}/{Max}. denemede ayrıştırılamadı, tekrar deneniyor.",
                    attempt, MaxGenerationAttempts);
                attemptPrompt = AppendRetryValidationInstructions(
                    prompt, ["geçerli JSON şeması"], collectedSuggestions.Select(s => s.Title!).ToList());
                continue;
            }

            var suggestions = parsed.Suggestions;
            foreach (var suggestion in suggestions)
            {
                // Required-field validation guarantees Title is non-null/non-empty here. Valid suggestions
                // from an imperfect response are retained; repeated complete batches are deduplicated.
                var normalizedTitle = NormalizeTitle(suggestion.Title!);
                if (collectedTitles.Add(normalizedTitle))
                {
                    collectedSuggestions.Add(suggestion);
                }
            }
            // TotalElementsSeen 0 ise kaynak zaten boştu (ör. "[]") — bu meşru bir "önerecek bir şey yok"
            // cevabıdır. >0 iken suggestions bunun YARISINDAN AZI kadarsa, önerilerin çoğu ayrıştırma/şema
            // hatasıyla sessizce kayboldu demektir (canlıda görülen somut örnek: LLM "activities" alanını
            // aynı nesnede birden fazla kez, her seferinde süslü parantezsiz tekrarlayınca, kurtarma tek bir
            // tekrarı düzeltip geri kalanları düzeltemiyordu ve 5 öneriden 4'ü sessizce kayboluyordu) — bu,
            // kısmen çalışan ama eksik bir yanıtı sessizce kabul etmek yerine tekrar denenmesi gereken
            // şüpheli bir durumdur.
            var keptEnough = parsed.TotalElementsSeen == 0 || suggestions.Count * 2 >= parsed.TotalElementsSeen;
            var hasSchemaOrRequiredFieldFailure = parsed.SchemaOrRequiredFieldFailures > 0;

            if (parsed.TotalElementsSeen == 0 && collectedSuggestions.Count == 0)
            {
                return ([], false);
            }

            if (keptEnough && !hasSchemaOrRequiredFieldFailure)
            {
                if (attempt > 1)
                {
                    logger.LogInformation("Yapay zekâ {Attempt}. denemede kullanılabilir bir yanıt üretti.", attempt);
                }
                return (collectedSuggestions, false);
            }

            if (isLastAttempt)
            {
                if (collectedSuggestions.Count == 0)
                {
                    throw new DomainException(
                        "Yapay zekâ zorunlu başlık, departman ve faaliyet bilgilerini eksiksiz üretemedi. Lütfen tekrar deneyin.");
                }

                logger.LogWarning(
                    "Son denemeden sonra yalnızca {Count} doğrulanmış öneri korunabildi; sonuç eksik işaretleniyor.",
                    collectedSuggestions.Count);
                return (collectedSuggestions, true);
            }

            logger.LogWarning(
                "Yapay zekâ yanıtındaki {Total} öğeden {Kept} doğrulanmış öneri alındı; {Invalid} öğede şema/zorunlu alan sorunu var ({Attempt}/{Max}. deneme), geçerli öneriler korunarak tekrar deneniyor.",
                parsed.TotalElementsSeen, suggestions.Count, parsed.SchemaOrRequiredFieldFailures,
                attempt, MaxGenerationAttempts);
            attemptPrompt = AppendRetryValidationInstructions(
                prompt, parsed.MissingRequiredFields, collectedSuggestions.Select(s => s.Title!).ToList());
        }

        throw new InvalidOperationException("Beklenmeyen kod yolu: üretim denemeleri döngüsü sonuçsuz bitti.");
    }

    // Gerçek bir kullanıcı iptali (ör. istemci bağlantıyı kesti) ile RAG'in kendi geçici arızasını
    // (InvalidOperationException) ya da altyapı/ağ seviyesindeki bir zaman aşımını (HttpRequestException/
    // TaskCanceledException) birbirinden ayırır — ct zaten iptal edilmişse tekrar denemek anlamsızdır,
    // istisna olduğu gibi yukarı fırlar.
    private static bool IsRetryableProviderFailure(Exception ex, CancellationToken ct) =>
        ex is InvalidOperationException || (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested);

    private static string AppendRetryValidationInstructions(
        string originalPrompt,
        IReadOnlyCollection<string> missingFields,
        IReadOnlyCollection<string> retainedTitles)
    {
        var fields = missingFields.Count == 0
            ? "geçerli JSON şeması"
            : string.Join(", ", missingFields.Distinct(StringComparer.OrdinalIgnoreCase));

        var retainedNote = retainedTitles.Count == 0
            ? string.Empty
            : " Şu geçerli öneriler zaten korundu, bunları tekrar üretme: " +
              string.Join("; ", retainedTitles.Distinct(StringComparer.OrdinalIgnoreCase)) + ".";

        return originalPrompt + "\n\n--- Yeniden Üretim Doğrulama Uyarısı ---\n" +
               $"Önceki yanıt kabul edilmedi. Eksik veya hatalı alanlar: {fields}. " +
               "Her iş paketinde title ve department dolu olmalı; activities içindeki her öğe " +
               "{\"title\":\"...\",\"effortHours\":8} biçiminde ve title dolu olmalıdır. " +
               "Yanıt yalnızca geçerli bir JSON dizisi olmalıdır." + retainedNote + "\n" +
               "--- Yeniden Üretim Doğrulama Uyarısı Sonu ---";
    }

    // LLM'ler talimata rağmen bazen çıktıyı ```json ... ``` bloğuna sarar, tek bir iş paketi için dizi
    // zarfını ([ ]) atlar, dizinin içine şemaya uymayan başıboş bir öğe sıkıştırır, ya da dizinin
    // SÖZ DİZİMİNİ tamamen bozan (ör. süslü parantezsiz sıkışmış bir "anahtar": "değer" parçası) geçersiz
    // JSON üretir. İlk üçüne toleranslı davranılır (markdown çitleri soyulur, kök nesne tek elemanlı
    // listeye sarılır, dizi öğeleri TEK TEK dönüştürülüp uymayanlar atlanır). Metin hiç ayrıştırılamayacak
    // kadar bozuksa (dördüncü durum), tüm cevabı reddetmek yerine metindeki dengeli { } bloklarını arayıp
    // her birini BAĞIMSIZ birer nesne gibi ayrıştırmayı dener — aralarındaki geçersiz parçalar yok sayılır.
    // (Bu ayrıştırma tolerans katmanının üstüne, GenerateAndParseSuggestionsAsync ayrıca modeli tekrar
    // çağırarak kalıcı bir "yeniden dene" katmanı ekler.)
    private SuggestionParseResult ParseSuggestions(string rawJson)
    {
        var trimmed = StripMarkdownCodeFence(rawJson);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new SuggestionParseResult([], 0, 0, []);
        }

        JToken token;
        try
        {
            token = JToken.Parse(trimmed);
        }
        catch (JsonException ex)
        {
            var (conversion, candidateCount) = RecoverSuggestionsFromBrokenJson(trimmed);
            if (conversion.Suggestions.Count == 0)
            {
                throw new DomainException($"Yapay zekâ yanıtı beklenen JSON şemasına uymuyor: {ex.Message}");
            }

            logger.LogWarning(
                ex,
                "Yapay zekâ yanıtı geçerli JSON değildi, metindeki bağımsız nesneler taranarak {Count}/{Total} öneri kurtarıldı.",
                conversion.Suggestions.Count, candidateCount);
            return new SuggestionParseResult(
                conversion.Suggestions, candidateCount, conversion.SchemaOrRequiredFieldFailures,
                conversion.MissingRequiredFields);
        }

        var elements = token switch
        {
            JArray array => array,
            JObject single => new JArray(single),
            _ => throw new DomainException(
                $"Yapay zekâ yanıtı beklenen JSON şemasına uymuyor: beklenmeyen kök JSON türü {token.Type}")
        };

        var converted = ConvertElements(elements);
        return new SuggestionParseResult(
            converted.Suggestions, elements.Count, converted.SchemaOrRequiredFieldFailures,
            converted.MissingRequiredFields);
    }

    private SuggestionConversionResult ConvertElements(IEnumerable<JToken> elements)
    {
        var suggestions = new List<RawWorkPackageSuggestion>();
        var failures = 0;
        var missingFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements)
        {
            if (element is not JObject suggestionObject)
            {
                logger.LogWarning(
                    "Yapay zekâ yanıtındaki bir dizi öğesi nesne değil, atlanıyor: {Element}",
                    element.ToString(Formatting.None));
                continue;
            }

            try
            {
                var suggestion = suggestionObject.ToObject<RawWorkPackageSuggestion>();
                if (suggestion is not null)
                {
                    var validationErrors = ValidateRequiredFields(suggestion);
                    if (validationErrors.Count > 0)
                    {
                        failures++;
                        missingFields.UnionWith(validationErrors);
                        logger.LogWarning(
                            "Yapay zekâ önerisi zorunlu alanları eksik olduğu için atlanıyor ({Fields}): {Element}",
                            string.Join(", ", validationErrors), suggestionObject.ToString(Formatting.None));
                        continue;
                    }

                    suggestions.Add(suggestion);
                }
            }
            catch (JsonException ex)
            {
                failures++;
                missingFields.Add("JSON şeması / activities");
                logger.LogWarning(
                    ex, "Yapay zekâ yanıtındaki bir öneri şemaya uymadığı için atlanıyor: {Element}",
                    suggestionObject.ToString(Formatting.None));
            }
        }

        return new SuggestionConversionResult(suggestions, failures, missingFields);
    }

    private static IReadOnlyCollection<string> ValidateRequiredFields(RawWorkPackageSuggestion suggestion)
    {
        var errors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(suggestion.Title))
        {
            errors.Add("title");
        }
        if (string.IsNullOrWhiteSpace(suggestion.Department))
        {
            errors.Add("department");
        }
        if (suggestion.Activities is not null && suggestion.Activities.Any(a => string.IsNullOrWhiteSpace(a.Title)))
        {
            errors.Add("activities[].title");
        }

        return errors;
    }

    private (SuggestionConversionResult Conversion, int CandidateCount) RecoverSuggestionsFromBrokenJson(string text)
    {
        var candidates = ExtractBalancedObjects(text).ToList();
        var parsedCandidates = candidates.Select(TryParseCandidateObject).Where(parsed => parsed is not null);
        return (ConvertElements(parsedCandidates!), candidates.Count);
    }

    // RawWorkPackageSuggestion'daki dizi-tipli alanların JSON anahtar adları (ör. "activities") —
    // reflection ile hesaplanır ki yarın şemaya yeni bir dizi alanı eklenirse (ör. "tags") bu onarım
    // otomatik kapsasın; sabit bir "activities" string'ine bağlı KALICI OLMAYAN bir yama olarak kalmasın.
    private static readonly string[] ArrayTypedFieldNames = typeof(RawWorkPackageSuggestion)
        .GetProperties()
        .Where(p => p.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
        .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..])
        .ToArray();

    // Bir aday { ... } bloğunun kendisi de bozuk olabilir — canlıda görülen somut örnek: dış nesne
    // (başlık/departman/süre gibi sağlam alanlarla) tamamen geçerliyken, içindeki "activities" dizisine
    // yine süslü parantezsiz bir "anahtar": değer parçası sıkışmış oluyor. Düz ayrıştırma başarısız olursa
    // bilinen TÜM dizi-tipli alanlar (bugün için tek örnek: "activities") sırayla boş diziyle değiştirilip
    // tekrar denenir — geri kalan tüm alanlar (bu alanlar hâlâ isteğe bağlı/nullable olduğu için)
    // kurtarılmış olur.
    private JToken? TryParseCandidateObject(string candidate)
    {
        try
        {
            return JObject.Parse(candidate);
        }
        catch (JsonException ex)
        {
            var repaired = candidate;
            var repairedAny = false;
            foreach (var fieldName in ArrayTypedFieldNames)
            {
                var attempt = TryBlankOutMalformedArrayField(repaired, fieldName);
                if (attempt is null)
                {
                    continue;
                }

                repaired = attempt;
                repairedAny = true;
            }

            if (!repairedAny)
            {
                return null;
            }

            try
            {
                return JObject.Parse(repaired);
            }
            catch (JsonException)
            {
                logger.LogWarning(
                    ex, "Bilinen dizi alanları boşaltılmasına rağmen aday nesne hâlâ ayrıştırılamadı, atlanıyor: {Candidate}",
                    candidate);
                return null;
            }
        }
    }

    // "fieldName": [ ... ] alanının TÜM tekrarlarını (tırnaklı string'ler içindeki köşeli parantezleri
    // saymadan) "fieldName": [] ile değiştirir. Canlıda görülen somut örnek: LLM "activities" alanını tek
    // bir dizi içinde birden fazla öğeyle yazacağına, aynı nesnede alanı BİRDEN FAZLA KEZ tekrarlıyor
    // (her aktivite için ayrı bir "activities": [...], her biri de süslü parantezsiz) — sadece İLK
    // tekrarı boşaltmak geri kalanları bozuk bıraktığı için ayrıştırma yine başarısız oluyordu. Tüm
    // eşleşmeler SONDAN BAŞA doğru değiştirilir ki henüz işlenmemiş daha önceki eşleşmelerin konumu
    // kaymasın. (JObject.Parse yinelenen anahtar adlarını hataya düşürmez, sonuncusunu tutar — bu yüzden
    // aynı alanın birden çok kez "[]" olması sorun değil.) Alan hiç bulunamazsa null döner.
    private static string? TryBlankOutMalformedArrayField(string json, string fieldName)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            json, $"\"{fieldName}\"\\s*:\\s*\\[", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (matches.Count == 0)
        {
            return null;
        }

        var result = json;
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            var openBracketIndex = match.Index + match.Length - 1;
            var closeBracketIndex = FindMatchingBracket(result, openBracketIndex, '[', ']');
            if (closeBracketIndex < 0)
            {
                continue;
            }

            result = string.Concat(result.AsSpan(0, match.Index), $"\"{fieldName}\": []", result.AsSpan(closeBracketIndex + 1));
        }

        return result == json ? null : result;
    }

    // Tırnaklı string'lerin İÇİNDEKİ köşeli/süslü parantezleri saymadan openIndex'teki açılış karakterine
    // karşılık gelen kapanış karakterinin konumunu bulur (ExtractBalancedObjects'teki aynı mantık, tek bir
    // bilinen aralık için tekrar kullanılabilir hale getirilmiş hâli).
    private static int FindMatchingBracket(string text, int openIndex, char open, char close)
    {
        var depth = 0;
        var inString = false;
        var escaping = false;

        for (var i = openIndex; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (escaping) escaping = false;
                else if (c == '\\') escaping = true;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') inString = true;
            else if (c == open) depth++;
            else if (c == close)
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    // Metin içinde tırnaklı string'lerin İÇİNDEKİ süslü parantezleri saymadan (ör. bir açıklama alanının
    // içinde "{" geçse bile yanılmadan) en dıştaki { ... } bloklarını bulur. Aralarındaki her şey (dizi
    // parantezleri, virgüller, süslü parantezsiz başıboş parçalar) sessizce yok sayılır.
    private static IEnumerable<string> ExtractBalancedObjects(string text)
    {
        var candidates = new List<string>();
        var depth = 0;
        var start = -1;
        var inString = false;
        var escaping = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (escaping) escaping = false;
                else if (c == '\\') escaping = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    break;
                case '{':
                    if (depth == 0) start = i;
                    depth++;
                    break;
                case '}':
                    if (depth > 0)
                    {
                        depth--;
                        if (depth == 0 && start >= 0)
                        {
                            candidates.Add(text[start..(i + 1)]);
                            start = -1;
                        }
                    }
                    break;
            }
        }

        return candidates;
    }

    // Sadece harf/rakamları tutup büyük harfe çevirir — boşluk, "/", "-", noktalama gibi farklar iki
    // başlığın "aynı iş" olduğu gerçeğini değiştirmez (ör. "UI/UX Tasarım" ile "UI UX Tasarım"). Kasıtlı
    // olarak KELİME SIRASI/EK KELİME farklarını YAKALAMAZ (ör. "Ruhsat İzin Süreçleri" ile "Ruhsat ve İzin
    // Süreçleri Entegrasyonu" farklı normalize olur) — bunları yakalamak bulanık/semantik bir karşılaştırma
    // gerektirir, bu basit normalizasyonun kapsamı dışında.
    private static string NormalizeTitle(string title) =>
        new string(title.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string StripMarkdownCodeFence(string rawJson)
    {
        var trimmed = rawJson.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstNewline = trimmed.IndexOf('\n');
        trimmed = firstNewline >= 0 ? trimmed[(firstNewline + 1)..] : trimmed;

        var fenceEnd = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return fenceEnd >= 0 ? trimmed[..fenceEnd].Trim() : trimmed.Trim();
    }

    private static AiSuggestionRequestDto ToDto(AiSuggestionRequest request, bool possiblyIncomplete = false) => new(
        request.Id, request.ProjectId, request.ProjectType, request.ExtraInstructions, request.ProviderUsed,
        request.CreatedAtUtc,
        string.IsNullOrWhiteSpace(request.SelectedDocumentNames)
            ? []
            : request.SelectedDocumentNames.Split(", ", StringSplitOptions.RemoveEmptyEntries),
        request.Items.Select(i => new WorkPackageSuggestionItemDto(
            i.Id, i.Title, i.Department, i.EffortHours, i.SourceDocument, i.Decision,
            i.Description, i.SequenceNote, i.InsertAfterTaskTitle, i.SequenceRank, i.IsAtProjectStart,
            i.Activities.Select(a => new AiSuggestionActivityDto(a.Id, a.Title, a.EffortHours)).ToList())).ToList(),
        request.UsedRealDocumentContext, possiblyIncomplete);
}
