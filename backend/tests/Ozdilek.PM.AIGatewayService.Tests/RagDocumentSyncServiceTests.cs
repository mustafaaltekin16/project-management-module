using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class RagDocumentSyncServiceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();

    private static RagDocumentSyncService CreateService(
        Mock<ITaskDocumentClient> taskDocumentClient, Mock<IRagClient> ragClient, RagOptions? options = null) =>
        new(taskDocumentClient.Object, ragClient.Object, options ?? new RagOptions(), new ProjectSyncLockRegistry(),
            NullLogger<RagDocumentSyncService>.Instance);

    [Fact]
    public async Task EnsureProjectDocumentsSyncedAsync_AlreadyIndexedFileName_IsSkippedFromUpload()
    {
        var documentId = Guid.NewGuid();
        var taskDocumentClient = new Mock<ITaskDocumentClient>();
        taskDocumentClient
            .Setup(c => c.ListDocumentsAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskDocumentSummary(documentId, "rapor.pdf", "Pdf")]);
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.ListDocumentsAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RagDocumentSummary("rapor.pdf", "pdf", 1.2, 10)]);

        var service = CreateService(taskDocumentClient, ragClient);
        var result = await service.EnsureProjectDocumentsSyncedAsync(ProjectId, restrictToDocumentIds: null);

        ragClient.Verify(c => c.UploadDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        result.ConfirmedIndexedFileNames.Should().Contain("rapor.pdf");
        result.FullySynced.Should().BeTrue();
    }

    [Theory]
    [InlineData("notlar.txt", true)]
    [InlineData("tablo.csv", true)]
    [InlineData("taslak.bmp", true)]
    [InlineData("gorsel.tiff", true)]
    [InlineData("eski.doc", false)]
    [InlineData("video.mov", false)]
    public async Task EnsureProjectDocumentsSyncedAsync_EligibilityIsByExtension_NotByTaskServiceKind(string fileName, bool expectedEligible)
    {
        // TaskService's Kind field is deliberately misleading here (e.g. "File" for .txt/.csv/.bmp/.tiff,
        // "Word" for .doc) — eligibility must be decided by RAG's own supported extensions, not Kind.
        var documentId = Guid.NewGuid();
        var taskDocumentClient = new Mock<ITaskDocumentClient>();
        taskDocumentClient
            .Setup(c => c.ListDocumentsAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskDocumentSummary(documentId, fileName, "File")]);
        taskDocumentClient
            .Setup(c => c.GetDocumentContentAsync(ProjectId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3]);
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.ListDocumentsAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ragClient
            .Setup(c => c.UploadDocumentAsync(ProjectId.ToString(), fileName, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", fileName, fileName));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", ProjectId.ToString(), "done", 1, 1, 0, 100));

        var service = CreateService(taskDocumentClient, ragClient, new RagOptions { JobPollIntervalMs = 5, JobPollTimeoutMs = 200 });
        var result = await service.EnsureProjectDocumentsSyncedAsync(ProjectId, restrictToDocumentIds: null);

        result.AttemptedFileNames.Contains(fileName).Should().Be(expectedEligible);
        if (expectedEligible)
        {
            result.ConfirmedIndexedFileNames.Should().Contain(fileName);
        }
        else
        {
            ragClient.Verify(c => c.UploadDocumentAsync(It.IsAny<string>(), fileName, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task EnsureProjectDocumentsSyncedAsync_JobNeverReachesDoneWithinTimeout_ExcludedWithoutThrowing()
    {
        var documentId = Guid.NewGuid();
        var taskDocumentClient = new Mock<ITaskDocumentClient>();
        taskDocumentClient
            .Setup(c => c.ListDocumentsAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskDocumentSummary(documentId, "yavas.pdf", "Pdf")]);
        taskDocumentClient
            .Setup(c => c.GetDocumentContentAsync(ProjectId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3]);
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.ListDocumentsAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ragClient
            .Setup(c => c.UploadDocumentAsync(ProjectId.ToString(), "yavas.pdf", It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagDocumentUploadResult(true, "queued", "job-1", "queued", "yavas.pdf", "yavas.pdf"));
        ragClient
            .Setup(c => c.GetJobStatusAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagJobStatus("job-1", ProjectId.ToString(), "processing", 1, 0, 0, 10));

        var service = CreateService(taskDocumentClient, ragClient, new RagOptions { JobPollIntervalMs = 5, JobPollTimeoutMs = 30 });
        var act = async () => await service.EnsureProjectDocumentsSyncedAsync(ProjectId, restrictToDocumentIds: null);

        var result = await act.Should().NotThrowAsync();
        result.Subject.ConfirmedIndexedFileNames.Should().NotContain("yavas.pdf");
        result.Subject.FullySynced.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureProjectDocumentsSyncedAsync_ConcurrentCallsForSameProject_SecondWaitsForFirst()
    {
        // Chat ve İş Paketi üretimi aynı proje için AYNI RAG session id'sini (projectId) paylaşıyor ve bu
        // metot stateless (her seferinde "eksik" olanı yeniden hesaplayıp yüklüyor) — kilit olmadan iki
        // eşzamanlı çağrı aynı "eksik" dokümanı bağımsızca RAG'e yükleyebilirdi. Bu test, ikinci çağrının
        // proje kilidini almak için birincinin bitmesini gerçekten beklediğini doğrular.
        var documentId = Guid.NewGuid();
        var firstCallEntered = new TaskCompletionSource();
        var releaseFirstCall = new TaskCompletionSource();
        var callCount = 0;

        var taskDocumentClient = new Mock<ITaskDocumentClient>();
        taskDocumentClient
            .Setup(c => c.ListDocumentsAsync(ProjectId, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                var isFirst = Interlocked.Increment(ref callCount) == 1;
                if (isFirst)
                {
                    firstCallEntered.SetResult();
                    await releaseFirstCall.Task;
                }
                return (IReadOnlyList<TaskDocumentSummary>)[new TaskDocumentSummary(documentId, "rapor.pdf", "Pdf")];
            });
        var ragClient = new Mock<IRagClient>();
        ragClient
            .Setup(c => c.ListDocumentsAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RagDocumentSummary("rapor.pdf", "pdf", 1.2, 10)]);

        var service = CreateService(taskDocumentClient, ragClient);

        var firstCall = service.EnsureProjectDocumentsSyncedAsync(ProjectId, restrictToDocumentIds: null);
        await firstCallEntered.Task;

        var secondCall = service.EnsureProjectDocumentsSyncedAsync(ProjectId, restrictToDocumentIds: null);
        await Task.Delay(50);
        callCount.Should().Be(1, "ikinci çağrı proje kilidini almak için birincinin bitmesini beklemeli");

        releaseFirstCall.SetResult();
        await Task.WhenAll(firstCall, secondCall);

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task EnsureProjectDocumentsSyncedAsync_TaskServiceThrows_ReturnsEmptyResultWithoutThrowing()
    {
        var taskDocumentClient = new Mock<ITaskDocumentClient>();
        taskDocumentClient
            .Setup(c => c.ListDocumentsAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("TaskService unreachable"));
        var ragClient = new Mock<IRagClient>();

        var service = CreateService(taskDocumentClient, ragClient);
        var act = async () => await service.EnsureProjectDocumentsSyncedAsync(ProjectId, restrictToDocumentIds: null);

        var result = await act.Should().NotThrowAsync();
        result.Subject.ConfirmedIndexedFileNames.Should().BeEmpty();
        result.Subject.FullySynced.Should().BeFalse();
        ragClient.Verify(c => c.ListDocumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
