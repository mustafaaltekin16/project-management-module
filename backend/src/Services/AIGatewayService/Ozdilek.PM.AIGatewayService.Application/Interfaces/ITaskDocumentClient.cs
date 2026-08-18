using Ozdilek.PM.AIGatewayService.Application.Dtos;

namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

/// <summary>
/// Reads project documents from TaskService (a synchronous cross-service call, same pattern as
/// <see cref="IProjectInfoClient"/>; see BearerTokenForwardingHandler for auth). Listing first (which
/// returns Name/Kind as JSON) lets callers skip unsupported document kinds before ever downloading their
/// bytes, and avoids depending on the raw-content endpoint's Content-Disposition header for the file name.
/// </summary>
public interface ITaskDocumentClient
{
    Task<IReadOnlyList<TaskDocumentSummary>> ListDocumentsAsync(Guid projectId, CancellationToken ct = default);

    Task<byte[]?> GetDocumentContentAsync(Guid projectId, Guid documentId, CancellationToken ct = default);
}
