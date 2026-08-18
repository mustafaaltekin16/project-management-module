using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Services;

namespace Ozdilek.PM.ProjectService.Api.Controllers;

[ApiController]
[Route("api/project-templates")]
[Authorize]
public sealed class ProjectTemplatesController(ProjectTemplateAppService appService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TemplateDto>>>> List(CancellationToken ct)
    {
        var result = await appService.ListAsync(ct);
        return Ok(ApiResponse<List<TemplateDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TemplateDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await appService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<TemplateDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<TemplateDto>>> Create(CreateTemplateRequest request, CancellationToken ct)
    {
        var result = await appService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<TemplateDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<TemplateDto>>> Update(Guid id, UpdateTemplateRequest request, CancellationToken ct)
    {
        var result = await appService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<TemplateDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await appService.DeleteAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpDelete("{templateId:guid}/fields/{fieldId:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<TemplateDto>>> RemoveField(Guid templateId, Guid fieldId, CancellationToken ct)
    {
        var result = await appService.RemoveFieldAsync(templateId, fieldId, ct);
        return Ok(ApiResponse<TemplateDto>.Ok(result));
    }
}
