using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.FeasibilityService.Domain;

public class ApprovalStep : BaseEntity
{
    public Guid FeasibilityItemId { get; private set; }
    public string ApproverName { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public ApprovalDecision Decision { get; private set; } = ApprovalDecision.Pending;
    public string? Comment { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }

    private ApprovalStep() { }

    public ApprovalStep(Guid feasibilityItemId, string approverName, int order)
    {
        FeasibilityItemId = feasibilityItemId;
        ApproverName = approverName;
        Order = order;
    }

    public void Decide(bool approve, string? comment)
    {
        if (Decision != ApprovalDecision.Pending)
        {
            throw new DomainException("Bu onay adımı zaten karara bağlanmış.");
        }

        Decision = approve ? ApprovalDecision.Approved : ApprovalDecision.Rejected;
        Comment = comment;
        DecidedAtUtc = DateTimeOffset.UtcNow;
    }
}
