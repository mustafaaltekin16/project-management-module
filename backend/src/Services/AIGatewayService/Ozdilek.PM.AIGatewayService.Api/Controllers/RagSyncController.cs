using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.Contracts.Web;

namespace Ozdilek.PM.AIGatewayService.Api.Controllers;

/// <summary>
/// Lets the Documents tab push a just-uploaded document into RAG immediately, instead of waiting for
/// the next chat/İş Paketi call to lazily discover it — see RagDocumentSyncService for the actual
/// upload→poll logic, this controller only triggers it for a single document on demand.
/// </summary>
[ApiController]
[Route("api/rag-sync")]
[Authorize]
public sealed class RagSyncController(IRagDocumentSyncService ragDocumentSyncService) : ControllerBase
{
    [HttpPost("projects/{projectId:guid}/documents/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<RagSyncResult>>> SyncDocument(Guid projectId, Guid documentId, CancellationToken ct)
    {
        var result = await ragDocumentSyncService.EnsureProjectDocumentsSyncedAsync(projectId, [documentId], ct);
        return Ok(ApiResponse<RagSyncResult>.Ok(result));
    }
}
