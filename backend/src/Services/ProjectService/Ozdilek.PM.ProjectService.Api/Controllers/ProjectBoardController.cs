using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Services;

namespace Ozdilek.PM.ProjectService.Api.Controllers;

[ApiController]
[Route("api/project-board")]
[Authorize]
public sealed class ProjectBoardController(ProjectBoardAppService appService) : ControllerBase
{
    [HttpGet("columns")]
    public async Task<ActionResult<ApiResponse<List<ProjectBoardColumnDto>>>> ListColumns(CancellationToken ct)
    {
        var result = await appService.ListColumnsAsync(ct);
        return Ok(ApiResponse<List<ProjectBoardColumnDto>>.Ok(result));
    }

    [HttpPost("columns")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<ProjectBoardColumnDto>>> CreateColumn(
        CreateProjectBoardColumnRequest request,
        CancellationToken ct)
    {
        var result = await appService.CreateColumnAsync(request, ct);
        return Ok(ApiResponse<ProjectBoardColumnDto>.Ok(result));
    }

    [HttpPut("columns/{id:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<ProjectBoardColumnDto>>> UpdateColumn(
        Guid id,
        UpdateProjectBoardColumnRequest request,
        CancellationToken ct)
    {
        var result = await appService.UpdateColumnAsync(id, request, ct);
        return Ok(ApiResponse<ProjectBoardColumnDto>.Ok(result));
    }

    [HttpPut("columns/reorder")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<object>>> ReorderColumns(
        ReorderProjectBoardColumnsRequest request,
        CancellationToken ct)
    {
        await appService.ReorderColumnsAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpDelete("columns/{id:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<object>>> ArchiveColumn(
        Guid id,
        [FromQuery] Guid? targetColumnId,
        CancellationToken ct)
    {
        await appService.ArchiveColumnAsync(id, targetColumnId, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPut("projects/{projectId:guid}/placement")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<object>>> MoveCard(
        Guid projectId,
        MoveProjectBoardCardRequest request,
        CancellationToken ct)
    {
        await appService.MoveCardAsync(projectId, request, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
