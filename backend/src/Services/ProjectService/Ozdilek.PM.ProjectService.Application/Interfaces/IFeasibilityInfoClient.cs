namespace Ozdilek.PM.ProjectService.Application.Interfaces;

/// <summary>Reads feasibility approval status from FeasibilityService (a synchronous cross-service call; see BearerTokenForwardingHandler for auth).</summary>
public interface IFeasibilityInfoClient
{
    /// <summary>
    /// True only if the project has at least one feasibility main group, every group has at least one
    /// item, and every item's approval chain has fully resolved to Approved. Anything else — no
    /// feasibility recorded yet, still in draft, pending approval, or rejected — is "not approved".
    /// </summary>
    Task<bool> IsFullyApprovedAsync(Guid projectId, CancellationToken ct = default);
}
