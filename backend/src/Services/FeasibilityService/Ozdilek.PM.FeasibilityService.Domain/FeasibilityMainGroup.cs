using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.FeasibilityService.Domain;

/// <summary>
/// Aggregate root for a project's feasibility hierarchy: an "ana grup" (e.g. "BT Alımı (Ana Grup)")
/// containing per-unit budget line items. Owning the items here (rather than each item standing alone)
/// gives a natural boundary for the budget summary and keeps submit/decide transactional per group.
/// </summary>
public class FeasibilityMainGroup : BaseEntity
{
    private readonly List<FeasibilityItem> _items = [];

    public Guid ProjectId { get; private set; }
    public Guid? WorkPackageId { get; private set; }
    public int TimelineSortOrder { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<FeasibilityItem> Items => _items.AsReadOnly();

    public decimal TotalRequestedAmount => _items.Sum(i => i.Amount);
    public decimal TotalApprovedAmount => _items.Where(i => i.Status == FeasibilityItemStatus.Approved).Sum(i => i.Amount);

    private FeasibilityMainGroup() { }

    public static FeasibilityMainGroup Create(
        Guid projectId,
        string name,
        Guid? workPackageId = null,
        int timelineSortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Ana grup adı zorunludur.");
        }

        return new FeasibilityMainGroup
        {
            ProjectId = projectId,
            WorkPackageId = workPackageId,
            TimelineSortOrder = Math.Max(0, timelineSortOrder),
            Name = name.Trim()
        };
    }

    public void ConfigureTimeline(Guid? workPackageId, int sortOrder)
    {
        WorkPackageId = workPackageId;
        TimelineSortOrder = Math.Max(0, sortOrder);
        MarkUpdated();
    }

    public FeasibilityItem AddItem(string unit, string description, decimal amount, string currency)
    {
        var item = FeasibilityItem.Create(Id, unit, description, amount, currency);
        _items.Add(item);
        MarkUpdated();
        return item;
    }

    public void SubmitItemForApproval(Guid itemId, IReadOnlyList<string> approverNamesInOrder)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException("Fizibilite kalemi bulunamadı.");
        item.SubmitForApproval(approverNamesInOrder);
        MarkUpdated();
    }

    public void DecideItem(Guid itemId, string approverName, bool approve, string? comment)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException("Fizibilite kalemi bulunamadı.");
        item.Decide(approverName, approve, comment);
        MarkUpdated();
    }
}
