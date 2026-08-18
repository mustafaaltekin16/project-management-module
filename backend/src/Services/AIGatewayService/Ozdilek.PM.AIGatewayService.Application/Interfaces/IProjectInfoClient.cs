using Ozdilek.PM.AIGatewayService.Application.Dtos;

namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

/// <summary>Reads project data from ProjectService (a synchronous cross-service call; see BearerTokenForwardingHandler for auth).</summary>
public interface IProjectInfoClient
{
    Task<ProjectInfoDto?> GetProjectAsync(Guid projectId, CancellationToken ct = default);
}
