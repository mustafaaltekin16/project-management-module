using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.FeasibilityService.Application.Dtos;
using Ozdilek.PM.FeasibilityService.Application.Services;

namespace Ozdilek.PM.FeasibilityService.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class FeasibilityController(FeasibilityAppService appService) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/feasibility-groups")]
    public async Task<ActionResult<ApiResponse<List<FeasibilityMainGroupDto>>>> ListForProject(Guid projectId, CancellationToken ct)
    {
        var result = await appService.ListByProjectAsync(projectId, ct);
        return Ok(ApiResponse<List<FeasibilityMainGroupDto>>.Ok(result));
    }

    [HttpPost("feasibility-groups")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<FeasibilityMainGroupDto>>> CreateMainGroup(CreateMainGroupRequest request, CancellationToken ct)
    {
        var result = await appService.CreateMainGroupAsync(request, ct);
        return Ok(ApiResponse<FeasibilityMainGroupDto>.Ok(result));
    }

    [HttpPut("feasibility-groups/{mainGroupId:guid}/timeline")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<FeasibilityMainGroupDto>>> ConfigureTimeline(
        Guid mainGroupId,
        ConfigureMainGroupTimelineRequest request,
        CancellationToken ct)
    {
        var result = await appService.ConfigureTimelineAsync(mainGroupId, request, ct);
        return Ok(ApiResponse<FeasibilityMainGroupDto>.Ok(result));
    }

    [HttpPost("feasibility-groups/{mainGroupId:guid}/items")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<FeasibilityMainGroupDto>>> AddItem(Guid mainGroupId, AddFeasibilityItemRequest request, CancellationToken ct)
    {
        var result = await appService.AddItemAsync(mainGroupId, request, ct);
        return Ok(ApiResponse<FeasibilityMainGroupDto>.Ok(result));
    }

    [HttpPost("feasibility-groups/{mainGroupId:guid}/items/{itemId:guid}/submit")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<FeasibilityMainGroupDto>>> SubmitForApproval(
        Guid mainGroupId, Guid itemId, SubmitForApprovalRequest request, CancellationToken ct)
    {
        var result = await appService.SubmitForApprovalAsync(mainGroupId, itemId, request, ct);
        return Ok(ApiResponse<FeasibilityMainGroupDto>.Ok(result));
    }

    [HttpPost("feasibility-groups/{mainGroupId:guid}/items/{itemId:guid}/decide")]
    [Authorize(Policy = Policies.CanApprove)]
    public async Task<ActionResult<ApiResponse<FeasibilityMainGroupDto>>> Decide(
        Guid mainGroupId, Guid itemId, DecideApprovalRequest request, CancellationToken ct)
    {
        var result = await appService.DecideAsync(mainGroupId, itemId, request, ct);
        return Ok(ApiResponse<FeasibilityMainGroupDto>.Ok(result));
    }
}
