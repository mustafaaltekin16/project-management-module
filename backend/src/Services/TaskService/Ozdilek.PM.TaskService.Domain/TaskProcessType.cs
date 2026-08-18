namespace Ozdilek.PM.TaskService.Domain;

/// <summary>
/// Stable workflow identity used by the project timeline. Display labels may change, but this value
/// keeps task groups linked to the correct process without relying on Turkish title matching.
/// </summary>
public enum TaskProcessType
{
    Feasibility,
    PriceComparison,
    Approval,
    Procurement
}
