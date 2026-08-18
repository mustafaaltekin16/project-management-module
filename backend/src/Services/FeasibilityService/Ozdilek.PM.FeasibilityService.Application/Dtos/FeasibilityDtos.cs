using Ozdilek.PM.FeasibilityService.Domain;

namespace Ozdilek.PM.FeasibilityService.Application.Dtos;

public sealed record ApprovalStepDto(Guid Id, string ApproverName, int Order, ApprovalDecision Decision, string? Comment, DateTimeOffset? DecidedAtUtc);

public sealed record FeasibilityItemDto(
    Guid Id, string Unit, string Description, decimal Amount, string Currency,
    FeasibilityItemStatus Status, IReadOnlyList<ApprovalStepDto> Steps);

public sealed record FeasibilityMainGroupDto(
    Guid Id, Guid ProjectId, Guid? WorkPackageId, int TimelineSortOrder,
    string Name, decimal TotalRequestedAmount, decimal TotalApprovedAmount,
    IReadOnlyList<FeasibilityItemDto> Items);

public sealed record CreateMainGroupRequest(
    Guid ProjectId,
    string Name,
    Guid? WorkPackageId = null,
    int TimelineSortOrder = 0);

public sealed record ConfigureMainGroupTimelineRequest(Guid? WorkPackageId, int TimelineSortOrder);

public sealed record AddFeasibilityItemRequest(string Unit, string Description, decimal Amount, string Currency);

public sealed record SubmitForApprovalRequest(List<string> ApproverNamesInOrder);

public sealed record DecideApprovalRequest(string ApproverName, bool Approve, string? Comment);
