using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Domain;
using Ozdilek.PM.BuildingBlocks.Persistence;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Persistence;

public sealed class AiSuggestionRequestRepository(AIGatewayDbContext context)
    : EfRepository<AiSuggestionRequest>(context), IAiSuggestionRequestRepository
{
    // ThenInclude(Activities) olmadan, öneri bu üretimin İLK yanıtından sonra tekrar yüklendiğinde
    // (ör. onay/red, ya da sayfa yeniden açıldığında) her item'ın activities'i sessizce boş dönerdi —
    // GenerateAsync'in döndürdüğü ilk yanıt hâlâ bellekteki (henüz DB'den okunmamış) nesne olduğu için
    // bu eksiklik fark edilmiyordu. Onay sırasında bu, WorkPackageApprovedEvent'e boş bir activities
    // listesi gitmesine (yani onaylanan görevin alt görevsiz oluşmasına) yol açıyordu.
    public override async Task<AiSuggestionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(r => r.Items).ThenInclude(i => i.Activities).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<AiSuggestionRequest>> ListByProjectAsync(Guid projectId, CancellationToken ct = default) =>
        await Set.Include(r => r.Items).ThenInclude(i => i.Activities)
            .Where(r => r.ProjectId == projectId).AsNoTracking().ToListAsync(ct);
}

public sealed class PromptTemplateRepository(AIGatewayDbContext context)
    : EfRepository<PromptTemplate>(context), IPromptTemplateRepository
{
    public async Task<PromptTemplate?> GetByProjectTypeAsync(string projectType, CancellationToken ct = default) =>
        await Set.AsNoTracking().FirstOrDefaultAsync(t => t.ProjectType == projectType, ct);
}
