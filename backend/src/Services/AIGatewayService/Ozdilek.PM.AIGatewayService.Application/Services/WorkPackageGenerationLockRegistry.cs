using System.Collections.Concurrent;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Serializes work-package generation calls per project. This lock is deliberately separate from
/// <see cref="ProjectSyncLockRegistry"/>: generation invokes document synchronization internally,
/// so sharing one non-reentrant semaphore would make the request wait on its own lock.
/// </summary>
public sealed class WorkPackageGenerationLockRegistry
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> locks = new();

    public async Task<IDisposable> AcquireAsync(Guid projectId, CancellationToken ct = default)
    {
        var gate = locks.GetOrAdd(projectId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        return new Releaser(gate);
    }

    private sealed class Releaser(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
}
