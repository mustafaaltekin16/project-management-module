using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.FeasibilityService.Domain;

/// <summary>
/// A single budget line under a <see cref="FeasibilityMainGroup"/>. Carries its own sequential
/// multi-step approval workflow: Draft → PendingApproval → Approved/Rejected. Approvers must decide
/// in the order they were submitted — this mirrors a real sign-off chain (e.g. unit manager, then
/// finance) rather than "first response wins".
/// </summary>
public class FeasibilityItem : BaseEntity
{
    private readonly List<ApprovalStep> _steps = [];

    public Guid MainGroupId { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public FeasibilityItemStatus Status { get; private set; } = FeasibilityItemStatus.Draft;

    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    private FeasibilityItem() { }

    public static FeasibilityItem Create(Guid mainGroupId, string unit, string description, decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new DomainException("Bütçe kalemi tutarı sıfırdan büyük olmalıdır.");
        }

        return new FeasibilityItem
        {
            MainGroupId = mainGroupId,
            Unit = unit,
            Description = description,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency
        };
    }

    public void SubmitForApproval(IReadOnlyList<string> approverNamesInOrder)
    {
        if (Status != FeasibilityItemStatus.Draft)
        {
            throw new DomainException("Sadece taslak durumundaki kalemler onaya gönderilebilir.");
        }

        if (approverNamesInOrder.Count == 0)
        {
            throw new DomainException("En az bir onaylayıcı belirtilmelidir.");
        }

        _steps.Clear();
        for (var i = 0; i < approverNamesInOrder.Count; i++)
        {
            _steps.Add(new ApprovalStep(Id, approverNamesInOrder[i], i));
        }

        Status = FeasibilityItemStatus.PendingApproval;
        MarkUpdated();
    }

    /// <summary>Applies one approver's decision. Must be the next pending step in the chain, in order.</summary>
    public void Decide(string approverName, bool approve, string? comment)
    {
        if (Status != FeasibilityItemStatus.PendingApproval)
        {
            throw new DomainException("Bu kalem şu anda onay bekleme durumunda değil.");
        }

        var nextStep = _steps
            .Where(s => s.Decision == ApprovalDecision.Pending)
            .OrderBy(s => s.Order)
            .FirstOrDefault() ?? throw new DomainException("Bekleyen bir onay adımı bulunamadı.");

        if (!string.Equals(nextStep.ApproverName, approverName, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException($"Sıradaki onaylayıcı '{nextStep.ApproverName}' olmalıdır.");
        }

        nextStep.Decide(approve, comment);

        if (!approve)
        {
            Status = FeasibilityItemStatus.Rejected;
        }
        else if (_steps.All(s => s.Decision == ApprovalDecision.Approved))
        {
            Status = FeasibilityItemStatus.Approved;
        }

        MarkUpdated();
    }
}
