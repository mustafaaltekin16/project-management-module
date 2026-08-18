using FluentAssertions;
using Ozdilek.PM.FeasibilityService.Domain;
using Xunit;

namespace Ozdilek.PM.FeasibilityService.Tests;

public class FeasibilityMainGroupTests
{
    [Fact]
    public void Create_WithWorkPackageLink_PersistsTimelineIdentity()
    {
        var workPackageId = Guid.NewGuid();

        var group = FeasibilityMainGroup.Create(Guid.NewGuid(), "BT Alımı", workPackageId, 1);

        group.WorkPackageId.Should().Be(workPackageId);
        group.TimelineSortOrder.Should().Be(1);
    }

    [Fact]
    public void TotalApprovedAmount_OnlyCountsApprovedItems()
    {
        var group = FeasibilityMainGroup.Create(Guid.NewGuid(), "BT Alımı (Ana Grup)");
        var approvedItem = group.AddItem("BT Müdürlüğü", "Bilgisayar Alımları", 100_000m, "TRY");
        group.AddItem("BT Müdürlüğü", "Yazıcı Alımları", 50_000m, "TRY");

        group.SubmitItemForApproval(approvedItem.Id, ["Ahmet Görür"]);
        group.DecideItem(approvedItem.Id, "Ahmet Görür", true, null);

        group.TotalRequestedAmount.Should().Be(150_000m);
        group.TotalApprovedAmount.Should().Be(100_000m);
    }
}
