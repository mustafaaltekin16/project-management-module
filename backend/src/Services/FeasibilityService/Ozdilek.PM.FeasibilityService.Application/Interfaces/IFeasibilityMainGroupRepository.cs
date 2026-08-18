using Ozdilek.PM.FeasibilityService.Domain;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.FeasibilityService.Application.Interfaces;

public interface IFeasibilityMainGroupRepository : IRepository<FeasibilityMainGroup>
{
    Task<List<FeasibilityMainGroup>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
}
