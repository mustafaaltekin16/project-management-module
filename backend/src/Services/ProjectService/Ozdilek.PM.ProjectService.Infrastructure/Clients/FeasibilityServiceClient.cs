using Newtonsoft.Json;
using Ozdilek.PM.ProjectService.Application.Interfaces;

namespace Ozdilek.PM.ProjectService.Infrastructure.Clients;

/// <summary>Response shapes FeasibilityService returns — mirrors Ozdilek.PM.Contracts.Web.ApiResponse{T} without a hard project reference.</summary>
internal sealed class FeasibilityServiceEnvelope
{
    public bool Success { get; set; }
    public List<FeasibilityMainGroupResponse>? Data { get; set; }
}

internal sealed class FeasibilityMainGroupResponse
{
    public List<FeasibilityItemResponse> Items { get; set; } = [];
}

internal sealed class FeasibilityItemResponse
{
    public string Status { get; set; } = string.Empty;
}

public sealed class FeasibilityServiceClient(HttpClient httpClient) : IFeasibilityInfoClient
{
    public async Task<bool> IsFullyApprovedAsync(Guid projectId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/api/projects/{projectId}/feasibility-groups", ct);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonConvert.DeserializeObject<FeasibilityServiceEnvelope>(body);
        var groups = envelope?.Data;

        if (groups is not { Count: > 0 })
        {
            return false;
        }

        return groups.All(g => g.Items.Count > 0 && g.Items.All(i => i.Status == "Approved"));
    }
}
