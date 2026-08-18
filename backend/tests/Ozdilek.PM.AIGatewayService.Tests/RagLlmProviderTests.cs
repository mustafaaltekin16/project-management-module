using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Ozdilek.PM.AIGatewayService.Infrastructure.Providers;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class RagLlmProviderTests
{
    private const string Prompt = "İş paketi üretim talimatları...";

    private static RagLlmProvider CreateProvider(Mock<IRagClient> ragClient, RagOptions? ragOptions = null) =>
        new(ragClient.Object, ragOptions ?? new RagOptions { JobPollIntervalMs = 5, JobPollTimeoutMs = 200 },
            NullLogger<RagLlmProvider>.Instance);

    [Fact]
    public void Name_IsRag()
    {
        var provider = CreateProvider(new Mock<IRagClient>());

        provider.Name.Should().Be("RAG");
    }

    [Fact]
    public async Task GenerateWorkPackagesJsonAsync_HappyPath_UploadsPromptAsSyntheticDocumentAndReturnsAnswer()
    {
        string? uploadedSessionId = null;
        byte[]? uploadedBytes = null;
        string? askedSessionId = null;
        string? generationQuestion = null;
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, byte[], CancellationToken>((sessionId, _, bytes, _) =>
            {
                uploadedSessionId = sessionId;
                uploadedBytes = bytes;
            })
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", null, null));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", "irrelevant", "done", 1, 1, 0, 100));
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RagAskRequest, CancellationToken>((req, _) =>
            {
                askedSessionId = req.SessionId;
                generationQuestion = req.Question;
            })
            .ReturnsAsync(new RagAnswer(true, "ok", """[{"title":"X"}]""", null, null, null));

        var provider = CreateProvider(ragClient);
        var result = await provider.GenerateWorkPackagesJsonAsync(Prompt);

        result.Should().Be("""[{"title":"X"}]""");
        System.Text.Encoding.UTF8.GetString(uploadedBytes!).Should().Be(Prompt);
        askedSessionId.Should().Be(uploadedSessionId);
        generationQuestion.Should().Contain("title").And.Contain("department").And.Contain("activities");
    }

    [Fact]
    public async Task GenerateWorkPackagesJsonAsync_UploadRejected_Throws()
    {
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagDocumentUploadResult(false, "reddedildi", "", "", null, null));

        var provider = CreateProvider(ragClient);
        var act = async () => await provider.GenerateWorkPackagesJsonAsync(Prompt);

        await act.Should().ThrowAsync<InvalidOperationException>();
        ragClient.Verify(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateWorkPackagesJsonAsync_IndexingFails_Throws()
    {
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", null, null));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", "irrelevant", "failed", 1, 0, 1, 100));

        var provider = CreateProvider(ragClient);
        var act = async () => await provider.GenerateWorkPackagesJsonAsync(Prompt);

        await act.Should().ThrowAsync<InvalidOperationException>();
        ragClient.Verify(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateWorkPackagesJsonAsync_AskReturnsEmptyAnswer_Throws()
    {
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", null, null));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", "irrelevant", "done", 1, 1, 0, 100));
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer(false, "no documents", null, null, null, null));

        var provider = CreateProvider(ragClient);
        var act = async () => await provider.GenerateWorkPackagesJsonAsync(Prompt);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateWorkPackagesJsonAsync_UsesRagOptionsDefaultMode()
    {
        string? usedMode = null;
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", null, null));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", "irrelevant", "done", 1, 1, 0, 100));
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RagAskRequest, CancellationToken>((req, _) => usedMode = req.Mode)
            .ReturnsAsync(new RagAnswer(true, "ok", "[]", null, null, null));

        var provider = CreateProvider(ragClient, new RagOptions { JobPollIntervalMs = 5, JobPollTimeoutMs = 200, DefaultMode = "creative" });
        await provider.GenerateWorkPackagesJsonAsync(Prompt);

        usedMode.Should().Be("creative");
    }
}
