using FluentAssertions;
using Ozdilek.PM.FeasibilityService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Xunit;

namespace Ozdilek.PM.FeasibilityService.Tests;

/// <summary>Covers the multi-step approval state machine: Draft → PendingApproval → Approved/Rejected.</summary>
public class FeasibilityItemApprovalTests
{
    private static FeasibilityItem CreateItem() =>
        FeasibilityItem.Create(Guid.NewGuid(), "BT Müdürlüğü", "Bilgisayar Alımları (20 Adet)", 250_000m, "TRY");

    [Fact]
    public void Create_WithNonPositiveAmount_Throws()
    {
        var act = () => FeasibilityItem.Create(Guid.NewGuid(), "BT Müdürlüğü", "Açıklama", 0m, "TRY");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SubmitForApproval_WithNoApprovers_Throws()
    {
        var item = CreateItem();

        var act = () => item.SubmitForApproval([]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SubmitForApproval_TwiceInARow_Throws()
    {
        var item = CreateItem();
        item.SubmitForApproval(["Ahmet Görür"]);

        var act = () => item.SubmitForApproval(["Ahmet Görür"]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Decide_OutOfOrder_Throws()
    {
        var item = CreateItem();
        item.SubmitForApproval(["Ahmet Görür", "Defne Sattı"]);

        // Defne is second in line — she should not be able to decide before Ahmet.
        var act = () => item.Decide("Defne Sattı", true, null);

        act.Should().Throw<DomainException>().WithMessage("*Ahmet Görür*");
    }

    [Fact]
    public void Decide_AllApprove_MarksItemApproved()
    {
        var item = CreateItem();
        item.SubmitForApproval(["Ahmet Görür", "Defne Sattı"]);

        item.Decide("Ahmet Görür", true, null);
        item.Decide("Defne Sattı", true, "Uygundur");

        item.Status.Should().Be(FeasibilityItemStatus.Approved);
    }

    [Fact]
    public void Decide_OneRejection_MarksItemRejectedEvenIfEarlierStepsApproved()
    {
        var item = CreateItem();
        item.SubmitForApproval(["Ahmet Görür", "Defne Sattı"]);

        item.Decide("Ahmet Görür", true, null);
        item.Decide("Defne Sattı", false, "Bütçe yetersiz");

        item.Status.Should().Be(FeasibilityItemStatus.Rejected);
    }

    [Fact]
    public void Decide_WhenNotPendingApproval_Throws()
    {
        var item = CreateItem();

        var act = () => item.Decide("Ahmet Görür", true, null);

        act.Should().Throw<DomainException>();
    }
}
