namespace Ozdilek.PM.Contracts.Events;

/// <summary>
/// Published by AIGatewayService when a user approves an AI-generated work package suggestion.
/// Consumed by TaskService, which converts each item into a real Task (marked as AI-originated).
/// </summary>
public sealed record WorkPackageApprovedEvent
{
    public required Guid SuggestionRequestId { get; init; }
    public required Guid ProjectId { get; init; }
    public required string ApprovedByUserId { get; init; }
    public required IReadOnlyList<WorkPackageItem> Items { get; init; }
    public required DateTimeOffset ApprovedAtUtc { get; init; }
}

public sealed record WorkPackageItem
{
    public required Guid SuggestionItemId { get; init; }
    public required string Title { get; init; }
    public required string Department { get; init; }
    public required int EffortHours { get; init; }
    public string? SourceDocument { get; init; }
    // LLM'in ürettiği, görevin ne olduğunu anlatan detaylı açıklama — TaskService'te ana görevin
    // Description alanına aynen aktarılır (bkz. ProjectTaskItem.Description).
    public string? Description { get; init; }
    // LLM'e verilen mevcut görev listesindeki (bkz. PromptBuilder.AppendExistingTasksList) TAM bir görev
    // başlığı, ya da bağımlılık yoksa null — TaskService bunu gerçek bir görevle eşleştirip yeni ana
    // görevin başlangıç/bitiş tarihini buna göre hesaplar (bkz. WorkPackageApprovedConsumer), böylece
    // onaylanan iş paketi listenin en sonuna değil, modelin işaret ettiği gerçek sıraya oturur.
    public string? InsertAfterTaskTitle { get; init; }
    // The work package's own activities — TaskService creates one main task for the package and one
    // sub-task per activity (dependsOnTaskId = the main task), instead of a single flat task.
    public required IReadOnlyList<WorkPackageActivity> Activities { get; init; }
}

public sealed record WorkPackageActivity
{
    public required string Title { get; init; }
    public int? EffortHours { get; init; }
}
