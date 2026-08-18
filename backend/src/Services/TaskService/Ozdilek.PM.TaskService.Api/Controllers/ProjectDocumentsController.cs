using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.TaskService.Application.Dtos;
using Ozdilek.PM.TaskService.Application.Services;

namespace Ozdilek.PM.TaskService.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/documents")]
[Authorize]
public sealed class ProjectDocumentsController(ProjectDocumentAppService appService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProjectDocumentDto>>>> List(Guid projectId, CancellationToken ct)
    {
        var result = await appService.ListForProjectAsync(projectId, ct);
        return Ok(ApiResponse<List<ProjectDocumentDto>>.Ok(result));
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ProjectDocumentDto>>> Upload(
        Guid projectId, IFormFile file, [FromForm] Guid? noteId, [FromForm] string? uploadedBy, CancellationToken ct)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        var command = new UploadDocumentCommand(file.FileName, stream.ToArray(), file.ContentType, noteId, uploadedBy);
        var result = await appService.UploadAsync(projectId, command, ct);
        return Ok(ApiResponse<ProjectDocumentDto>.Ok(result));
    }

    [HttpGet("{documentId:guid}/content")]
    public async Task<IActionResult> Download(Guid projectId, Guid documentId, CancellationToken ct)
    {
        var document = await appService.GetContentAsync(projectId, documentId, ct);
        return File(document.Content, document.ContentType, document.Name);
    }

    [HttpDelete("{documentId:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<IActionResult> Delete(Guid projectId, Guid documentId, CancellationToken ct)
    {
        await appService.DeleteAsync(projectId, documentId, ct);
        return NoContent();
    }
}
