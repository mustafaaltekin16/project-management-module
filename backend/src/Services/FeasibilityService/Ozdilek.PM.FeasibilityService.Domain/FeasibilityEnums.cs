namespace Ozdilek.PM.FeasibilityService.Domain;

public enum FeasibilityItemStatus
{
    Draft,
    PendingApproval,
    Approved,
    Rejected
}

public enum ApprovalDecision
{
    Pending,
    Approved,
    Rejected
}
