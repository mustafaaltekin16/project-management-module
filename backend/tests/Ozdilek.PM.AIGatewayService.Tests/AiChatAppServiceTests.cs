using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Ozdilek.PM.SharedKernel.Exceptions;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class AiChatAppServiceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();

    private static AiChatAppService CreateService(
        Mock<IRagDocumentSyncService> ragDocumentSyncService, Mock<IRagClient> ragClient, RagOptions? options = null) =>
        new(ragDocumentSyncService.Object, ragClient.Object, options ?? new RagOptions(),
            NullLogger<AiChatAppService>.Instance);

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task AskAsync_QuestionTooShort_ThrowsDomainException(string question)
    {
        var ragDocumentSyncService = new Mock<IRagDocumentSyncService>();
        var ragClient = new Mock<IRagClient>();
        var service = CreateService(ragDocumentSyncService, ragClient);

        var act = async () => await service.AskAsync(new AskProjectGuideRequestDto(ProjectId, question));

        await act.Should().ThrowAsync<DomainException>();
        ragClient.Verify(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AskAsync_QuestionTooLong_ThrowsDomainException()
    {
        var ragDocumentSyncService = new Mock<IRagDocumentSyncService>();
        var ragClient = new Mock<IRagClient>();
        var service = CreateService(ragDocumentSyncService, ragClient);
        var question = new string('a', 5001);

        var act = async () => await service.AskAsync(new AskProjectGuideRequestDto(ProjectId, question));

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task AskAsync_RagReturnsUnsuccessful_MapsToFallbackMessageInsteadOfThrowing()
    {
        var ragDocumentSyncService = new Mock<IRagDocumentSyncService>();
        ragDocumentSyncService
            .Setup(s => s.EnsureProjectDocumentsSyncedAsync(ProjectId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagSyncResult([], [], true));
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer(false, "Bağlam bulunamadı", null, null, null, null));

        var service = CreateService(ragDocumentSyncService, ragClient);
        var result = await service.AskAsync(new AskProjectGuideRequestDto(ProjectId, "Projenin kapsamı nedir?"));

        result.Answer.Should().Be("Bağlam bulunamadı");
        result.Sources.Should().BeEmpty();
        result.UsedRealDocumentContext.Should().BeFalse();
    }

    [Fact]
    public async Task AskAsync_RagSucceedsButRetrievedContextsEmpty_MarksUsedRealDocumentContextFalse()
    {
        // The exact "looks like a real answer but isn't grounded" case: RAG returns success:true with a
        // plausible answer, but retrieved_contexts is empty — this happens whenever RAG had no usable
        // document for the session (sync failed, or nothing eligible was ever indexed), and the LLM falls
        // back to only its own general knowledge instead of the project's real documents.
        var ragDocumentSyncService = new Mock<IRagDocumentSyncService>();
        ragDocumentSyncService
            .Setup(s => s.EnsureProjectDocumentsSyncedAsync(ProjectId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagSyncResult([], [], true));
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer(true, "ok", "Genel bir cevap", null, [], []));

        var service = CreateService(ragDocumentSyncService, ragClient);
        var result = await service.AskAsync(new AskProjectGuideRequestDto(ProjectId, "Projenin kapsamı nedir?"));

        result.Answer.Should().Be("Genel bir cevap");
        result.UsedRealDocumentContext.Should().BeFalse();
    }

    [Fact]
    public async Task AskAsync_RagClientThrows_ReturnsFriendlyGenericMessageInsteadOfPropagating()
    {
        var ragDocumentSyncService = new Mock<IRagDocumentSyncService>();
        ragDocumentSyncService
            .Setup(s => s.EnsureProjectDocumentsSyncedAsync(ProjectId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagSyncResult([], [], true));
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("RAG unreachable"));

        var service = CreateService(ragDocumentSyncService, ragClient);
        var act = async () => await service.AskAsync(new AskProjectGuideRequestDto(ProjectId, "Projenin kapsamı nedir?"));

        var result = await act.Should().NotThrowAsync();
        result.Subject.Answer.Should().NotBeNullOrWhiteSpace();
        result.Subject.Sources.Should().BeEmpty();
        result.Subject.UsedRealDocumentContext.Should().BeFalse();
    }

    [Fact]
    public async Task AskAsync_SyncThrows_StillAsksRagAndReturnsAnswer()
    {
        var ragDocumentSyncService = new Mock<IRagDocumentSyncService>();
        ragDocumentSyncService
            .Setup(s => s.EnsureProjectDocumentsSyncedAsync(ProjectId, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("TaskService unreachable"));
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer(true, "ok", "Cevap metni", null, ["dosya.pdf"], ["bağlam"]));

        var service = CreateService(ragDocumentSyncService, ragClient);
        var result = await service.AskAsync(new AskProjectGuideRequestDto(ProjectId, "Projenin kapsamı nedir?"));

        result.Answer.Should().Be("Cevap metni");
        result.Sources.Should().Contain("dosya.pdf");
        result.UsedRealDocumentContext.Should().BeTrue();
    }
}
