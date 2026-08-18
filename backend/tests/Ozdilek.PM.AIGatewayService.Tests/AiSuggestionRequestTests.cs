using FluentAssertions;
using Ozdilek.PM.AIGatewayService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

/// <summary>
/// Covers the "AI output is never used directly" guarantee: every suggestion item starts Pending and
/// can only be decided once.
/// </summary>
public class AiSuggestionRequestTests
{
    private static AiSuggestionRequest CreateRequestWithOneItem()
    {
        var request = AiSuggestionRequest.Create(Guid.NewGuid(), "FeasibilityBased", null, "redacted prompt", "Mock");
        request.AddItem("Teknik standart kontrolü", "Teknik Müdürlük", 12, "Teknik Standartlar.pdf");
        return request;
    }

    [Fact]
    public void NewItem_StartsAsPending()
    {
        var request = CreateRequestWithOneItem();

        request.Items.Single().Decision.Should().Be(SuggestionItemDecision.Pending);
    }

    [Fact]
    public void ApproveItem_MarksApproved()
    {
        var request = CreateRequestWithOneItem();
        var item = request.Items.Single();

        request.ApproveItem(item.Id);

        item.Decision.Should().Be(SuggestionItemDecision.Approved);
    }

    [Fact]
    public void ApproveItem_Twice_Throws()
    {
        var request = CreateRequestWithOneItem();
        var item = request.Items.Single();
        request.ApproveItem(item.Id);

        var act = () => request.ApproveItem(item.Id);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RejectItem_UnknownId_ThrowsNotFound()
    {
        var request = CreateRequestWithOneItem();

        var act = () => request.RejectItem(Guid.NewGuid());

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void AddItem_ReturnsCreatedItem()
    {
        var request = AiSuggestionRequest.Create(Guid.NewGuid(), "FeasibilityBased", null, "redacted prompt", "Mock");

        var item = request.AddItem("Teknik standart kontrolü", "Teknik Müdürlük", 12, "Teknik Standartlar.pdf");

        item.Should().BeSameAs(request.Items.Single());
    }

    [Fact]
    public void AddActivity_PopulatesItemActivities()
    {
        var request = AiSuggestionRequest.Create(Guid.NewGuid(), "FeasibilityBased", null, "redacted prompt", "Mock");
        var item = request.AddItem("Teknik standart kontrolü", "Teknik Müdürlük", 12, "Teknik Standartlar.pdf");

        item.AddActivity("Mevcut standartların taranması", 4);

        request.Items.Single().Activities.Single().Title.Should().Be("Mevcut standartların taranması");
    }

    [Theory]
    [InlineData(null, "Analiz")]
    [InlineData("", "Analiz")]
    [InlineData("Saha Etüdü", null)]
    [InlineData("Saha Etüdü", " ")]
    public void AddItem_MissingRequiredText_ThrowsDomainException(string? title, string? department)
    {
        var request = AiSuggestionRequest.Create(Guid.NewGuid(), "FeasibilityBased", null, "prompt", "Mock");

        var act = () => request.AddItem(title!, department!, 10, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddActivity_MissingTitle_ThrowsDomainException()
    {
        var request = CreateRequestWithOneItem();
        var item = request.Items.Single();

        var act = () => item.AddActivity(" ", 4);

        act.Should().Throw<DomainException>();
    }
}
