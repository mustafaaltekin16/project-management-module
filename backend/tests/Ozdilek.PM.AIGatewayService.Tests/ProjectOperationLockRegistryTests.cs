using FluentAssertions;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class ProjectOperationLockRegistryTests
{
    [Fact]
    public async Task GenerationAndDocumentSyncLocks_ForSameProject_CanBeHeldTogether()
    {
        var projectId = Guid.NewGuid();
        var generationLocks = new WorkPackageGenerationLockRegistry();
        var syncLocks = new ProjectSyncLockRegistry();

        using var generationLease = await generationLocks.AcquireAsync(projectId);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var syncLease = await syncLocks.AcquireAsync(projectId, timeout.Token);

        syncLease.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerationLock_SerializesConcurrentCalls_ButAllowsNextCallAfterRelease()
    {
        var projectId = Guid.NewGuid();
        var generationLocks = new WorkPackageGenerationLockRegistry();
        var firstLease = await generationLocks.AcquireAsync(projectId);

        var secondLeaseTask = generationLocks.AcquireAsync(projectId);
        await Task.Delay(50);
        secondLeaseTask.IsCompleted.Should().BeFalse();

        firstLease.Dispose();
        using var secondLease = await secondLeaseTask.WaitAsync(TimeSpan.FromSeconds(1));

        secondLease.Should().NotBeNull();
    }
}
