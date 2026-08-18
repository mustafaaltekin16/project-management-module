using Newtonsoft.Json;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Clients;

internal sealed class TaskServiceTaskGroupListEnvelope
{
    public bool Success { get; set; }
    public List<TaskServiceTaskGroupDto>? Data { get; set; }
}

internal sealed class TaskServiceTaskGroupDto
{
    public List<TaskServiceTaskItemDto>? Tasks { get; set; }
}

internal sealed class TaskServiceTaskItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMainTask { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? StartDateUtc { get; set; }
    public DateTimeOffset? DueDateUtc { get; set; }
}

public sealed class TaskInfoClient(HttpClient httpClient) : ITaskInfoClient
{
    public async Task<IReadOnlyList<ExistingTaskInfoDto>> ListExistingTasksAsync(Guid projectId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/api/projects/{projectId}/task-groups", ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonConvert.DeserializeObject<TaskServiceTaskGroupListEnvelope>(body);
        var tasks = envelope?.Data?.SelectMany(g => g.Tasks ?? []) ?? [];

        // Sadece ana görevler (iş paketi seviyesi) — alt görevler LLM için gereksiz detay/gürültü.
        // Gerçek başlangıç tarihine göre sıralanır (tarihi olmayanlar sona) — Görevler ekranındaki
        // "tek liste, uygulama sırasına göre" sıralamasıyla aynı mantık (bkz. project-detail-page.ts
        // sequencedTasks), böylece LLM'e gösterilen sıra kullanıcının gördüğü sırayla tutarlı olur.
        return tasks
            .Where(t => t.IsMainTask)
            .OrderBy(t => t.StartDateUtc.HasValue ? 0 : 1)
            .ThenBy(t => t.StartDateUtc)
            .Select(t => new ExistingTaskInfoDto(t.Title, t.Description, t.Status, t.StartDateUtc, t.DueDateUtc))
            .ToList();
    }
}
