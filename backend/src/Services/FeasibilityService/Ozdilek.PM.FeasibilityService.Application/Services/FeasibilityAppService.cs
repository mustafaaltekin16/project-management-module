using Ozdilek.PM.Contracts.Events;
using Ozdilek.PM.FeasibilityService.Application.Dtos;
using Ozdilek.PM.FeasibilityService.Application.Interfaces;
using Ozdilek.PM.FeasibilityService.Domain;
using Ozdilek.PM.SharedKernel.Events;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.FeasibilityService.Application.Services;

public sealed class FeasibilityAppService(
    IFeasibilityMainGroupRepository mainGroups, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
{
    // Published after any mutation that changes a project's feasibility-completion ratio (new item,
    // submit for approval, decide — all change either the item count or its resolved/pending weight)
    // so ProjectService can re-derive ProgressPercent/DeviationDays for FeasibilityBased projects (see
    // ProjectProgressInputsChangedEvent). Not published from CreateMainGroupAsync/ConfigureTimelineAsync
    // — neither changes any item's status.
    private Task PublishProgressInputsChangedAsync(Guid projectId, CancellationToken ct) =>
        eventPublisher.PublishAsync(new ProjectProgressInputsChangedEvent { ProjectId = projectId }, ct);

    public async Task<FeasibilityMainGroupDto> CreateMainGroupAsync(CreateMainGroupRequest request, CancellationToken ct = default)
    {
        var group = FeasibilityMainGroup.Create(
            request.ProjectId,
            request.Name,
            request.WorkPackageId,
            request.TimelineSortOrder);
        await mainGroups.AddAsync(group, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(group);
    }

    public async Task<List<FeasibilityMainGroupDto>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var result = await mainGroups.ListByProjectAsync(projectId, ct);
        return result.Select(ToDto).ToList();
    }

    public async Task<FeasibilityMainGroupDto> AddItemAsync(Guid mainGroupId, AddFeasibilityItemRequest request, CancellationToken ct = default)
    {
        // No repository Update() call: `group` is already tracked (loaded via a tracking query), so EF
        // Core's change tracker picks up the newly added item on its own. Calling Update() again on a
        // graph that just gained a brand-new child (client-generated Guid key) makes EF treat that new
        // child as an existing row to UPDATE instead of INSERT — fails with "0 rows affected".
        var group = await mainGroups.GetByIdAsync(mainGroupId, ct) ?? throw new NotFoundException("Ana grup bulunamadı.");
        group.AddItem(request.Unit, request.Description, request.Amount, request.Currency);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishProgressInputsChangedAsync(group.ProjectId, ct);
        return ToDto(group);
    }

    public async Task<FeasibilityMainGroupDto> ConfigureTimelineAsync(
        Guid mainGroupId,
        ConfigureMainGroupTimelineRequest request,
        CancellationToken ct = default)
    {
        var group = await mainGroups.GetByIdAsync(mainGroupId, ct) ?? throw new NotFoundException("Ana grup bulunamadı.");
        group.ConfigureTimeline(request.WorkPackageId, request.TimelineSortOrder);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(group);
    }

    public async Task<FeasibilityMainGroupDto> SubmitForApprovalAsync(Guid mainGroupId, Guid itemId, SubmitForApprovalRequest request, CancellationToken ct = default)
    {
        // Same reasoning as AddItemAsync: SubmitForApproval creates brand-new ApprovalStep children.
        var group = await mainGroups.GetByIdAsync(mainGroupId, ct) ?? throw new NotFoundException("Ana grup bulunamadı.");
        group.SubmitItemForApproval(itemId, request.ApproverNamesInOrder);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishProgressInputsChangedAsync(group.ProjectId, ct);
        return ToDto(group);
    }

    public async Task<FeasibilityMainGroupDto> DecideAsync(Guid mainGroupId, Guid itemId, DecideApprovalRequest request, CancellationToken ct = default)
    {
        var group = await mainGroups.GetByIdAsync(mainGroupId, ct) ?? throw new NotFoundException("Ana grup bulunamadı.");
        group.DecideItem(itemId, request.ApproverName, request.Approve, request.Comment);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishProgressInputsChangedAsync(group.ProjectId, ct);
        return ToDto(group);
    }

    private static FeasibilityMainGroupDto ToDto(FeasibilityMainGroup group) => new(
        group.Id, group.ProjectId, group.WorkPackageId, group.TimelineSortOrder,
        group.Name, group.TotalRequestedAmount, group.TotalApprovedAmount,
        group.Items.Select(i => new FeasibilityItemDto(
            i.Id, i.Unit, i.Description, i.Amount, i.Currency, i.Status,
            i.Steps.OrderBy(s => s.Order)
                .Select(s => new ApprovalStepDto(s.Id, s.ApproverName, s.Order, s.Decision, s.Comment, s.DecidedAtUtc))
                .ToList()))
            .ToList());
}
