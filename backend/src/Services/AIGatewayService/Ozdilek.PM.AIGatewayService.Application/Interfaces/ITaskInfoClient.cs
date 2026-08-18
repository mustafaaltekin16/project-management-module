using Ozdilek.PM.AIGatewayService.Application.Dtos;

namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

/// <summary>
/// Reads a project's existing MAIN tasks from TaskService (same cross-service HTTP pattern as
/// <see cref="ITaskDocumentClient"/>/<see cref="IProjectInfoClient"/>) so a new generation can be told
/// what already exists — both to avoid suggesting already-covered work and to explain where a new
/// suggestion fits in the real, dated sequence (see PromptBuilder.AppendExistingTasksList).
/// </summary>
public interface ITaskInfoClient
{
    Task<IReadOnlyList<ExistingTaskInfoDto>> ListExistingTasksAsync(Guid projectId, CancellationToken ct = default);
}
