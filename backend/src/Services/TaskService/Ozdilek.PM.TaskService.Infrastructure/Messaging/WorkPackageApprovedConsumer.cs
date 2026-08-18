using MassTransit;
using Microsoft.Extensions.Logging;
using Ozdilek.PM.Contracts.Events;
using Ozdilek.PM.SharedKernel.Events;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.TaskService.Application.Interfaces;
using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Infrastructure.Messaging;

/// <summary>
/// Reacts to an approved AI work-package suggestion (published by AIGatewayService) by creating a real
/// main task for the package plus one sub-task per activity (dependsOnTaskId = the main task), each
/// marked <see cref="ProjectTaskItem.IsAiGenerated"/> so the UI can show the "AI" badge. The package is
/// routed into the project's real task group matching its department — first by an exact match on the
/// group's own <see cref="TaskGroup.Title"/> (the specific department/work-package row title; the LLM is
/// prompted with the project's real row titles, see PromptBuilder.AppendDepartmentList, so this is now
/// precise even when multiple rows share the same department name), falling back to a looser
/// <see cref="TaskGroup.Subtitle"/> (department name) match for older suggestions/custom prompts that
/// only produced a department name. Only when NEITHER matches (e.g. Basit projects with no department
/// rows at all) does it fall back to a neutral "Genel Görevler" bucket group — the exact same group a
/// manually-added, group-less task defaults into (see project-detail-page.ts `openTaskDialog`) — so this
/// bucket never carries a misleading "AI-only" identity; provenance is always the per-task "AI" badge
/// (<see cref="ProjectTaskItem.IsAiGenerated"/>), never the container's name. This is the asynchronous,
/// event-driven half of the AI-suggestion approval flow — the approval decision itself happens
/// synchronously in AIGatewayService.
/// </summary>
public sealed class WorkPackageApprovedConsumer(
    ITaskGroupRepository groups,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<WorkPackageApprovedConsumer> logger) : IConsumer<WorkPackageApprovedEvent>
{
    private const string FallbackGroupTitle = "Genel Görevler";

    public async Task Consume(ConsumeContext<WorkPackageApprovedEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        // No-tracking read (see ProjectDepartmentsAssignedConsumer for the same rationale) — re-fetched
        // through the tracked GetByIdAsync below once the actual target group is known.
        var existingGroups = await groups.ListByProjectAsync(message.ProjectId, ct);

        foreach (var item in message.Items)
        {
            var matched = existingGroups.FirstOrDefault(g =>
                string.Equals(g.Title, item.Department, StringComparison.OrdinalIgnoreCase))
                ?? existingGroups.FirstOrDefault(g =>
                    !string.IsNullOrWhiteSpace(g.Subtitle) &&
                    string.Equals(g.Subtitle, item.Department, StringComparison.OrdinalIgnoreCase));

            TaskGroup targetGroup;
            bool isNewGroup;
            if (matched is not null)
            {
                targetGroup = await groups.GetByIdAsync(matched.Id, ct)
                    ?? throw new InvalidOperationException("Eşleşen görev grubu beklenmedik şekilde bulunamadı.");
                isNewGroup = false;
            }
            else
            {
                var fallback = existingGroups.FirstOrDefault(g => g.Title == FallbackGroupTitle);
                isNewGroup = fallback is null;
                targetGroup = isNewGroup
                    ? TaskGroup.Create(message.ProjectId, FallbackGroupTitle, "")
                    : await groups.GetByIdAsync(fallback!.Id, ct)
                        ?? throw new InvalidOperationException("Genel görevler grubu beklenmedik şekilde bulunamadı.");
            }

            var (startDateUtc, dueDateUtc) = ResolveSequencePlacement(existingGroups, item);

            // Ana görev = iş paketinin kendisi.
            var mainTask = targetGroup.AddTask(
                title: item.Title,
                // A department name is not a person — leave this genuinely unassigned (frontend shows
                // "Atanmamış") rather than making it look like "Department" was assigned the task.
                assigneeName: string.Empty,
                department: item.Department,
                effortHours: item.EffortHours,
                isMainTask: true,
                dependsOnTaskId: null,
                isAiGenerated: true,
                sourceAiSuggestionItemId: item.SuggestionItemId,
                startDateUtc: startDateUtc,
                dueDateUtc: dueDateUtc,
                description: item.Description);

            // Faaliyetler = ana görevin alt görevleri.
            foreach (var activity in item.Activities)
            {
                targetGroup.AddTask(
                    title: activity.Title,
                    assigneeName: string.Empty,
                    department: item.Department,
                    effortHours: activity.EffortHours,
                    isMainTask: false,
                    dependsOnTaskId: mainTask.Id,
                    isAiGenerated: true,
                    sourceAiSuggestionItemId: item.SuggestionItemId);
            }

            if (isNewGroup)
            {
                await groups.AddAsync(targetGroup, ct);
                // Keep the local snapshot in sync so a second item in the same event (not used today,
                // but the event shape allows it) can also match this newly-created fallback group.
                existingGroups.Add(targetGroup);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        // New main tasks change the project's task-completion ratio — see ProjectProgressInputsChangedEvent.
        await eventPublisher.PublishAsync(new ProjectProgressInputsChangedEvent { ProjectId = message.ProjectId }, ct);

        logger.LogInformation(
            "WorkPackageApprovedEvent processed for project {ProjectId}: {ItemCount} work package(s) approved from suggestion {SuggestionRequestId}.",
            message.ProjectId, message.Items.Count, message.SuggestionRequestId);
    }

    // LLM'in ürettiği item.InsertAfterTaskTitle'ı (bkz. PromptBuilder.AppendExistingTasksList) gerçek bir
    // ana görevle eşleştirip yeni görevin başlangıç/bitiş tarihini buna göre hesaplar — bu sayede
    // onaylanan iş paketi Görevler ekranının (tarihe göre sıralanan) listesinde en sona değil, modelin
    // işaret ettiği yere oturur. Eşleşme yoksa (başlık uydurulmuş/silinmiş bir göreve ait olabilir)
    // tarihsiz bırakılır — sahte bir tarih uydurmaktansa bu daha güvenli, ama nadiren de olsa eski
    // "listenin sonuna düşme" davranışına geri döner.
    private (DateTimeOffset? StartDateUtc, DateTimeOffset? DueDateUtc) ResolveSequencePlacement(
        List<TaskGroup> existingGroups, WorkPackageItem item)
    {
        if (string.IsNullOrWhiteSpace(item.InsertAfterTaskTitle))
        {
            return (null, null);
        }

        var anchor = existingGroups
            .SelectMany(g => g.Tasks)
            .Where(t => t.IsMainTask && !t.ArchivedAtUtc.HasValue)
            .FirstOrDefault(t => string.Equals(t.Title, item.InsertAfterTaskTitle, StringComparison.OrdinalIgnoreCase));

        if (anchor is null)
        {
            logger.LogWarning(
                "AI önerisi \"{ItemTitle}\", var olmayan bir göreve (\"{InsertAfterTaskTitle}\") bağlanmak istedi — tarihsiz eklendi.",
                item.Title, item.InsertAfterTaskTitle);
            return (null, null);
        }

        var startDateUtc = anchor.DueDateUtc ?? anchor.StartDateUtc ?? DateTimeOffset.UtcNow;
        var workDays = Math.Max(1, (int)Math.Ceiling(item.EffortHours / 8.0));
        return (startDateUtc, startDateUtc.AddDays(workDays));
    }
}
