using FluentAssertions;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class RagPromptQuestionsTests
{
    [Fact]
    public void BuildWorkPackageRetrievalQuestion_NullExtraInstructions_UsesPlaceholder()
    {
        var question = RagPromptQuestions.BuildWorkPackageRetrievalQuestion(null);

        question.Should().Contain("(yok)");
        question.Should().Contain("iş paketi");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildWorkPackageRetrievalQuestion_BlankExtraInstructions_UsesPlaceholder(string extraInstructions)
    {
        var question = RagPromptQuestions.BuildWorkPackageRetrievalQuestion(extraInstructions);

        question.Should().Contain("(yok)");
    }

    [Fact]
    public void BuildWorkPackageRetrievalQuestion_WithExtraInstructions_AppendsThemVerbatim()
    {
        var question = RagPromptQuestions.BuildWorkPackageRetrievalQuestion("Sadece güvenlik gereksinimlerine odaklan.");

        question.Should().Contain("Sadece güvenlik gereksinimlerine odaklan.");
        question.Should().NotContain("(yok)");
    }

    [Fact]
    public void BuildExistingTaskRetrievalQuestion_NullExtraInstructions_UsesPlaceholder()
    {
        var question = RagPromptQuestions.BuildExistingTaskRetrievalQuestion(null);

        question.Should().Contain("(yok)");
        question.Should().Contain("BİREBİR AYNI");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildExistingTaskRetrievalQuestion_BlankExtraInstructions_UsesPlaceholder(string extraInstructions)
    {
        var question = RagPromptQuestions.BuildExistingTaskRetrievalQuestion(extraInstructions);

        question.Should().Contain("(yok)");
    }

    [Fact]
    public void BuildExistingTaskRetrievalQuestion_WithExtraInstructions_AppendsThemVerbatim()
    {
        var question = RagPromptQuestions.BuildExistingTaskRetrievalQuestion("Sadece güvenlik gereksinimlerine odaklan.");

        question.Should().Contain("Sadece güvenlik gereksinimlerine odaklan.");
        question.Should().NotContain("(yok)");
    }

    [Fact]
    public void BuildPendingSuggestionRetrievalQuestion_NullExtraInstructions_UsesPlaceholder()
    {
        var question = RagPromptQuestions.BuildPendingSuggestionRetrievalQuestion(null);

        question.Should().Contain("(yok)");
        question.Should().Contain("BİREBİR AYNI");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildPendingSuggestionRetrievalQuestion_BlankExtraInstructions_UsesPlaceholder(string extraInstructions)
    {
        var question = RagPromptQuestions.BuildPendingSuggestionRetrievalQuestion(extraInstructions);

        question.Should().Contain("(yok)");
    }

    [Fact]
    public void BuildPendingSuggestionRetrievalQuestion_WithExtraInstructions_AppendsThemVerbatim()
    {
        var question = RagPromptQuestions.BuildPendingSuggestionRetrievalQuestion("Sadece güvenlik gereksinimlerine odaklan.");

        question.Should().Contain("Sadece güvenlik gereksinimlerine odaklan.");
        question.Should().NotContain("(yok)");
    }
}
