using Newtonsoft.Json;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Clients;

/// <summary>Response envelope shape ProjectService returns — mirrors Ozdilek.PM.Contracts.Web.ApiResponse{T} without a hard project reference.</summary>
internal sealed class ProjectServiceEnvelope
{
    public bool Success { get; set; }
    public ProjectServiceProjectDto? Data { get; set; }
}

internal sealed class ProjectServiceProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<ProjectServiceDepartmentDto> Departments { get; set; } = [];
}

internal sealed class ProjectServiceDepartmentDto
{
    public string Title { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}

public sealed class ProjectServiceClient(HttpClient httpClient) : IProjectInfoClient
{
    public async Task<ProjectInfoDto?> GetProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/api/projects/{projectId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonConvert.DeserializeObject<ProjectServiceEnvelope>(body);

        if (envelope?.Data is not { } project)
        {
            return null;
        }

        return new ProjectInfoDto(
            project.Id, project.Name, project.Description, project.Type, project.Unit,
            project.Departments.Select(d => new ProjectDepartmentInfoDto(d.Title, d.DepartmentName)).ToList());
    }
}
