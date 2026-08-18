using Ozdilek.PM.AIGatewayService.Domain;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

public interface IAiSuggestionRequestRepository : IRepository<AiSuggestionRequest>
{
    Task<List<AiSuggestionRequest>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
}

public interface IPromptTemplateRepository : IRepository<PromptTemplate>
{
    Task<PromptTemplate?> GetByProjectTypeAsync(string projectType, CancellationToken ct = default);
}
