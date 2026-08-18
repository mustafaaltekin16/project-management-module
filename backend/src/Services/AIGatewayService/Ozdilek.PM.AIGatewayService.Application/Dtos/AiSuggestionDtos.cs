using Ozdilek.PM.AIGatewayService.Domain;

namespace Ozdilek.PM.AIGatewayService.Application.Dtos;

/// <summary>Minimal project data fetched from ProjectService to build the prompt — this service owns no project data itself.</summary>
public sealed record ProjectInfoDto(
    Guid Id, string Name, string Description, string Type, string Unit,
    IReadOnlyList<ProjectDepartmentInfoDto> Departments);

/// <summary>
/// A real department/work-package row entered at project creation. Given to the LLM as a closed list
/// (see PromptBuilder.AppendDepartmentList) so it routes suggestions to a title that actually exists
/// instead of inventing its own — only Title/DepartmentName are needed for that, not dates/managers.
/// </summary>
public sealed record ProjectDepartmentInfoDto(string Title, string DepartmentName);

public sealed record GenerateSuggestionsRequest(Guid ProjectId, string? ExtraInstructions, IReadOnlyList<Guid>? SelectedDocumentIds);

/// <summary>Minimal document data fetched from TaskService — Kind lets callers skip unsupported kinds before downloading content.</summary>
public sealed record TaskDocumentSummary(Guid Id, string Name, string Kind);

/// <summary>A document's extracted text, ready to be appended to a prompt (see PromptBuilder.AppendDocumentExcerpts).</summary>
public sealed record DocumentExcerpt(string FileName, string Text);

/// <summary>
/// An existing MAIN task (iş paketi seviyesi — alt görevler/activities hariç), TaskService'ten çekilir
/// (bkz. ITaskInfoClient) ve LLM'e gerçek uygulama sırasıyla verilir (bkz.
/// PromptBuilder.AppendExistingTasksList) — hem tekrar öneri üretmemesi hem de yeni önerinin sıraya
/// nereye oturduğunu açıklayabilmesi için.
/// </summary>
public sealed record ExistingTaskInfoDto(string Title, string? Description, string Status, DateTimeOffset? StartDateUtc, DateTimeOffset? DueDateUtc);

public sealed record AiSuggestionActivityDto(Guid Id, string Title, int? EffortHours);

public sealed record WorkPackageSuggestionItemDto(
    Guid Id, string Title, string Department, int EffortHours, string? SourceDocument, SuggestionItemDecision Decision,
    string? Description, string? SequenceNote, string? InsertAfterTaskTitle, int? SequenceRank, bool IsAtProjectStart,
    IReadOnlyList<AiSuggestionActivityDto> Activities);

public sealed record AiSuggestionRequestDto(
    Guid Id, Guid ProjectId, string ProjectType, string? ExtraInstructions, string ProviderUsed,
    DateTimeOffset CreatedAtUtc, IReadOnlyList<string> SelectedDocumentNames,
    IReadOnlyList<WorkPackageSuggestionItemDto> Items, bool UsedRealDocumentContext,
    // Sadece bu üretim çağrısının ANLIK sonucunu yansıtır (kalıcı değil, DB'ye yazılmaz) — modelin
    // yanıtındaki önerilerin yarısından azı ayrıştırılabildiğinde (bkz. AiSuggestionAppService.
    // GenerateAndParseSuggestionsAsync) true olur, ki kullanıcı "hiç hata almadım ama beklenenden çok az
    // öneri geldi" durumunu ayırt edebilsin. Geçmiş isteklerin listelenmesinde/onay-red akışında her zaman
    // false'tur — bu bilgi o anda tekrar hesaplanamaz ve tarihsel olarak saklanmasına gerek yoktur.
    bool PossiblyIncomplete = false);

/// <summary>
/// Raw shape a single work package's activity/sub-task is instructed to be returned as. Title is
/// nullable here because C# nullable annotations do not make a missing JSON property fail Newtonsoft
/// deserialization; the application layer validates it before the value reaches the domain/database.
/// </summary>
public sealed record RawActivity(string? Title, int? EffortHours);

/// <summary>
/// Raw shape the LLM is instructed to return as JSON — parsed with Newtonsoft.Json. EffortHours/
/// IsAtProjectStart are deliberately nullable even though the prompt always asks for a value: canlıda
/// model bazen belirsiz kaldığında null/geçersiz bir değer yazıyor, ve bu alanlar non-nullable olsaydı
/// ToObject&lt;T&gt;() tüm öneriyi (başlık/departman/açıklama gibi sağlam alanlarıyla birlikte) atardı —
/// bkz. AiSuggestionAppService.GenerateAsync'teki null-coalescing varsayılanları.
/// </summary>
public sealed record RawWorkPackageSuggestion(
    string? Title, string? Department, int? EffortHours, string? SourceDocument,
    string? Description, string? SequenceNote, string? InsertAfterTaskTitle, int? SequenceRank, bool? IsAtProjectStart,
    IReadOnlyList<RawActivity>? Activities);
