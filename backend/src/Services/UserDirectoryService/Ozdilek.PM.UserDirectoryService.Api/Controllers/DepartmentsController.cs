using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Application.Services;

namespace Ozdilek.PM.UserDirectoryService.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public sealed class DepartmentsController(DepartmentAppService appService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> List(
        [FromQuery] bool includeInactive,
        CancellationToken ct)
    {
        var result = await appService.ListAsync(includeInactive, ct);
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await appService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<DepartmentDetailDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create(CreateDepartmentRequest request, CancellationToken ct)
    {
        var result = await appService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<DepartmentDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken ct)
    {
        var result = await appService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<DepartmentDto>.Ok(result));
    }

    [HttpPut("{id:guid}/head")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> AssignHead(Guid id, AssignDepartmentHeadRequest request, CancellationToken ct)
    {
        var result = await appService.AssignHeadAsync(id, request, ct);
        return Ok(ApiResponse<DepartmentDto>.Ok(result));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Policies.CanManageDirectory)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> SetStatus(
        Guid id,
        SetDepartmentStatusRequest request,
        CancellationToken ct)
    {
        var result = await appService.SetStatusAsync(id, request, ct);
        return Ok(ApiResponse<DepartmentDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageDirectory)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await appService.DeleteAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
