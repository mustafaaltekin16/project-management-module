using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Ozdilek.PM.Contracts.Web;

namespace Ozdilek.PM.AIGatewayService.Api.Controllers;

[ApiController]
[Route("api/ai-chat")]
[Authorize]
public sealed class AiChatController(AiChatAppService appService) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<ActionResult<ApiResponse<AskProjectGuideResponseDto>>> Ask(AskProjectGuideRequestDto request, CancellationToken ct)
    {
        var result = await appService.AskAsync(request, ct);
        return Ok(ApiResponse<AskProjectGuideResponseDto>.Ok(result));
    }
}
