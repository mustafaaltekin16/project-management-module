using Newtonsoft.Json;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Clients;

/// <summary>Response envelope shape TaskService returns — mirrors Ozdilek.PM.Contracts.Web.ApiResponse{T} without a hard project reference.</summary>
internal sealed class TaskServiceDocumentListEnvelope
{
    public bool Success { get; set; }
    public List<TaskServiceDocumentDto>? Data { get; set; }
}

internal sealed class TaskServiceDocumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

public sealed class TaskDocumentClient(HttpClient httpClient) : ITaskDocumentClient
{
    public async Task<IReadOnlyList<TaskDocumentSummary>> ListDocumentsAsync(Guid projectId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/api/projects/{projectId}/documents", ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonConvert.DeserializeObject<TaskServiceDocumentListEnvelope>(body);
        return envelope?.Data?.Select(d => new TaskDocumentSummary(d.Id, d.Name, d.Kind)).ToList() ?? [];
    }

    public async Task<byte[]?> GetDocumentContentAsync(Guid projectId, Guid documentId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/api/projects/{projectId}/documents/{documentId}/content", ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(ct) : null;
    }
}
