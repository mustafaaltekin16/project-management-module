using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Application.Services;

namespace Ozdilek.PM.UserDirectoryService.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController(EmployeeAppService appService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> Search(
        [FromQuery] string? role,
        [FromQuery] string? q,
        [FromQuery] Guid? departmentId,
        [FromQuery] bool includeInactive,
        CancellationToken ct)
    {
        var result = await appService.SearchAsync(new EmployeeListFilter(role, q, departmentId, includeInactive), ct);
        return Ok(ApiResponse<List<EmployeeDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await appService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<EmployeeDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Create(CreateEmployeeRequest request, CancellationToken ct)
    {
        var result = await appService.CreateAsync(request, User.IsInRole(Roles.Admin), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<EmployeeDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Update(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken ct)
    {
        var result = await appService.UpdateAsync(id, request, User.IsInRole(Roles.Admin), ct);
        return Ok(ApiResponse<EmployeeDto>.Ok(result));
    }

    [HttpPut("{id:guid}/department")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> AssignDepartment(Guid id, AssignEmployeeDepartmentRequest request, CancellationToken ct)
    {
        var result = await appService.AssignDepartmentAsync(id, request, User.IsInRole(Roles.Admin), ct);
        return Ok(ApiResponse<EmployeeDto>.Ok(result));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Policies.CanManageDirectory)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> SetStatus(
        Guid id,
        SetEmployeeStatusRequest request,
        CancellationToken ct)
    {
        var result = await appService.SetStatusAsync(id, request, ct);
        return Ok(ApiResponse<EmployeeDto>.Ok(result));
    }

    [HttpPut("{id:guid}/password")]
    [Authorize(Policy = Policies.CanManageDirectory)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        Guid id,
        ResetEmployeePasswordRequest request,
        CancellationToken ct)
    {
        await appService.ResetPasswordAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageDirectory)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await appService.DeleteAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
