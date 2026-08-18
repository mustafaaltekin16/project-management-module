using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;

namespace Ozdilek.PM.AIGatewayService.Api.Controllers;

[ApiController]
[Route("api/ai-suggestions")]
[Authorize]
public sealed class AiSuggestionsController(AiSuggestionAppService appService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AiSuggestionRequestDto>>> Generate(GenerateSuggestionsRequest request, CancellationToken ct)
    {
        var result = await appService.GenerateAsync(request, ct);
        return Ok(ApiResponse<AiSuggestionRequestDto>.Ok(result));
    }

    [HttpGet("projects/{projectId:guid}")]
    public async Task<ActionResult<ApiResponse<List<AiSuggestionRequestDto>>>> ListForProject(Guid projectId, CancellationToken ct)
    {
        var result = await appService.ListByProjectAsync(projectId, ct);
        return Ok(ApiResponse<List<AiSuggestionRequestDto>>.Ok(result));
    }

    [HttpPost("{requestId:guid}/items/{itemId:guid}/approve")]
    [Authorize(Policy = Policies.CanApprove)]
    public async Task<ActionResult<ApiResponse<AiSuggestionRequestDto>>> ApproveItem(Guid requestId, Guid itemId, CancellationToken ct)
    {
        var approvedBy = User.FindFirst("sub")?.Value ?? "unknown";
        var result = await appService.ApproveItemAsync(requestId, itemId, approvedBy, ct);
        return Ok(ApiResponse<AiSuggestionRequestDto>.Ok(result));
    }

    [HttpPost("{requestId:guid}/items/{itemId:guid}/reject")]
    [Authorize(Policy = Policies.CanApprove)]
    public async Task<ActionResult<ApiResponse<AiSuggestionRequestDto>>> RejectItem(Guid requestId, Guid itemId, CancellationToken ct)
    {
        var result = await appService.RejectItemAsync(requestId, itemId, ct);
        return Ok(ApiResponse<AiSuggestionRequestDto>.Ok(result));
    }
}
