using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class WorkPackageContextRetrievalServiceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly ExistingTaskInfoDto[] OneTask = [new("Zemin Etüdü", null, "Done", null, null)];
    private static readonly string[] OnePendingTitle = ["Saha Analizi"];

    private static WorkPackageContextRetrievalService CreateService(
        Mock<IRagClient> ragClient, RagOptions? ragOptions = null, WorkPackageContextRetrievalOptions? options = null) =>
        new(ragClient.Object, ragOptions ?? new RagOptions { JobPollIntervalMs = 5, JobPollTimeoutMs = 200 },
            options ?? new WorkPackageContextRetrievalOptions(), NullLogger<WorkPackageContextRetrievalService>.Instance);

    [Fact]
    public async Task RetrieveExistingTaskContextAsync_NoTasks_MakesNoRagCallsAndReturnsEmpty()
    {
        var ragClient = new Mock<IRagClient>();
        var service = CreateService(ragClient);

        var result = await service.RetrieveExistingTaskContextAsync(ProjectId, [], null);

        result.Should().BeEmpty();
        ragClient.Verify(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RetrievePendingSuggestionContextAsync_NoTitles_MakesNoRagCallsAndReturnsEmpty()
    {
        var ragClient = new Mock<IRagClient>();
        var service = CreateService(ragClient);

        var result = await service.RetrievePendingSuggestionContextAsync(ProjectId, [], null);

        result.Should().BeEmpty();
        ragClient.Verify(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RetrieveExistingTaskContextAsync_HappyPath_UploadsToASessionDifferentFromTheProjectSession()
    {
        string? uploadedSessionId = null;
        string? askedSessionId = null;
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, byte[], CancellationToken>((sessionId, _, _, _) => uploadedSessionId = sessionId)
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", null, "gorev-listesi.txt"));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", "irrelevant", "done", 1, 1, 0, 100));
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RagAskRequest, CancellationToken>((req, _) => askedSessionId = req.SessionId)
            .ReturnsAsync(new RagAnswer(true, "ok", "cevap", null, null, ["1. \"Zemin Etüdü\" — durum: Done, tarih planlanmamış"]));

        var service = CreateService(ragClient);
        var result = await service.RetrieveExistingTaskContextAsync(ProjectId, OneTask, null);

        result.Should().ContainSingle().Which.Should().Contain("Zemin Etüdü");
        uploadedSessionId.Should().NotBeNullOrEmpty().And.NotBe(ProjectId.ToString());
        askedSessionId.Should().Be(uploadedSessionId);
    }

    [Fact]
    public async Task RetrieveExistingTaskContextAsync_UploadFails_ReturnsEmptyAndNeverAsks()
    {
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagDocumentUploadResult(false, "reddedildi", "", "", null, null));

        var service = CreateService(ragClient);
        var result = await service.RetrieveExistingTaskContextAsync(ProjectId, OneTask, null);

        result.Should().BeEmpty();
        ragClient.Verify(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RetrieveExistingTaskContextAsync_JobNeverReachesDoneWithinTimeout_ReturnsEmptyWithoutThrowing()
    {
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", null, null));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", "irrelevant", "processing", 1, 0, 0, 10));

        var service = CreateService(ragClient, new RagOptions { JobPollIntervalMs = 5, JobPollTimeoutMs = 30 });
        var act = async () => await service.RetrieveExistingTaskContextAsync(ProjectId, OneTask, null);

        var result = await act.Should().NotThrowAsync();
        result.Subject.Should().BeEmpty();
        ragClient.Verify(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RetrieveExistingTaskContextAsync_AskReturnsEmptyRetrievedContexts_ReturnsEmpty()
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
            .ReturnsAsync(new RagAnswer(true, "ok", "cevap yok", null, null, RetrievedContexts: []));

        var service = CreateService(ragClient);
        var result = await service.RetrieveExistingTaskContextAsync(ProjectId, OneTask, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RetrieveExistingTaskContextAsync_RagClientThrows_ReturnsEmptyWithoutThrowing()
    {
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("RAG unreachable"));

        var service = CreateService(ragClient);
        var act = async () => await service.RetrieveExistingTaskContextAsync(ProjectId, OneTask, null);

        var result = await act.Should().NotThrowAsync();
        result.Subject.Should().BeEmpty();
    }

    [Fact]
    public async Task RetrieveExistingTaskContextAndRetrievePendingSuggestionContext_UseDifferentEphemeralSessions()
    {
        var uploadedSessionIds = new List<string>();
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, byte[], CancellationToken>((sessionId, _, _, _) => uploadedSessionIds.Add(sessionId))
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", null, null));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", "irrelevant", "done", 1, 1, 0, 100));
        ragClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer(true, "ok", "cevap", null, null, ["ilgili bağlam"]));

        var service = CreateService(ragClient);
        await service.RetrieveExistingTaskContextAsync(ProjectId, OneTask, null);
        await service.RetrievePendingSuggestionContextAsync(ProjectId, OnePendingTitle, null);

        uploadedSessionIds.Should().HaveCount(2);
        uploadedSessionIds[0].Should().NotBe(uploadedSessionIds[1]);
    }
}
