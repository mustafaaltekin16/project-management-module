using System.Collections.Concurrent;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Serializes RAG document synchronization per project so concurrent chat and work-package requests do
/// not upload the same missing document twice. It must remain separate from
/// <see cref="WorkPackageGenerationLockRegistry"/> because generation calls synchronization internally.
/// </summary>
public sealed class ProjectSyncLockRegistry
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
