namespace Ozdilek.PM.Contracts.Events;

/// <summary>
/// Published whenever something that feeds a project's progress/deviation calculation changes — a task's
/// status/count (TaskService: add, status change, archive, restore, copy, AI-approval) or a feasibility
/// item's status/count (FeasibilityService: add item, submit for approval, decide). Consumed by
/// ProjectService, which re-derives ProgressPercent/DeviationDays from current TaskService/FeasibilityService
/// data (see ProjectProgressCalculator) and persists it — so the numbers stay correct without depending on
/// a specific browser tab being open (the previous design: the Angular Detail Page computed these client-side
/// and PUT them back only when someone happened to visit that page).
/// </summary>
public sealed record ProjectProgressInputsChangedEvent
{
    public required Guid ProjectId { get; init; }
}
