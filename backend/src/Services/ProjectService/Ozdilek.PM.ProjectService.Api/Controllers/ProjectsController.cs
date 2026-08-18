using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Services;
using Ozdilek.PM.ProjectService.Domain;

namespace Ozdilek.PM.ProjectService.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController(
    ProjectAppService appService,
    ProjectTimelineAppService timelineAppService,
    ProjectProgressAppService progressAppService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProjectListItemDto>>>> Search(
        [FromQuery] ProjectType? type, [FromQuery] string? q, CancellationToken ct)
    {
        var result = await appService.SearchAsync(new ProjectListFilter(type, q), ct);
        return Ok(ApiResponse<List<ProjectListItemDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await appService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<ApiResponse<ProjectTimelineDto>>> GetTimeline(Guid id, CancellationToken ct)
    {
        var result = await timelineAppService.GetAsync(id, ct);
        return Ok(ApiResponse<ProjectTimelineDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var result = await appService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ProjectDetailDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanDeleteProjects)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await appService.DeleteAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("{id:guid}/departments")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> AddDepartment(Guid id, AddDepartmentRequest request, CancellationToken ct)
    {
        var result = await appService.AddDepartmentAsync(id, request, ct);
        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    [HttpPut("{id:guid}/template-values")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> UpdateTemplateValues(Guid id, UpdateTemplateValuesRequest request, CancellationToken ct)
    {
        var result = await appService.UpdateTemplateValuesAsync(id, request, ct);
        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> AddNote(Guid id, AddNoteRequest request, CancellationToken ct)
    {
        var result = await appService.AddNoteAsync(id, request, ct);
        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    [HttpPut("{id:guid}/notes/{noteId:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> UpdateNote(Guid id, Guid noteId, UpdateNoteRequest request, CancellationToken ct)
    {
        var result = await appService.UpdateNoteAsync(id, noteId, request, ct);
        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    // Deliberately just [Authorize] (any authenticated user), not CanManageProjects — this is driven
    // automatically off task/feasibility completion (see ProjectProgressInputsChangedConsumer), which any
    // project member can do (TaskGroupsController.ChangeStatus), not a manager typing in a number. Gating
    // it to PM/Admin would silently break progress syncing for every Member-role user completing their
    // own tasks. No request body: the client can no longer supply arbitrary progressPercent/deviationDays
    // — this is now a "recompute from current TaskService/FeasibilityService data" trigger, called by the
    // frontend right after a user action for instant feedback instead of waiting for the async event
    // (see ProjectProgressAppService.RecomputeProgressAsync).
    [HttpPut("{id:guid}/progress")]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> RecomputeProgress(Guid id, CancellationToken ct)
    {
        var result = await progressAppService.RecomputeProgressAsync(id, ct);
        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<object>>> Activate(Guid id, CancellationToken ct)
    {
        await appService.ActivateAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.CanManageProjects)]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id, CancellationToken ct)
    {
        await appService.CancelAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
