using System.Text;
using Ozdilek.PM.AIGatewayService.Application.Dtos;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Formats this app's own DB-backed lists (existing tasks, pending suggestion titles) into plain text
/// documents suitable for <see cref="IWorkPackageContextRetrievalService"/> to upload to RAG as ephemeral,
/// throwaway documents — pure data, no instructional/talimat text (that stays fixed in
/// <see cref="PromptBuilder"/>, independent of how the shown subset was retrieved).
/// </summary>
public static class SyntheticContextDocumentFormatter
{
    public static string FormatExistingTasks(IReadOnlyList<ExistingTaskInfoDto> tasks)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            var dateRange = task.StartDateUtc.HasValue || task.DueDateUtc.HasValue
                ? $"{task.StartDateUtc:dd.MM.yyyy} – {task.DueDateUtc:dd.MM.yyyy}"
                : "tarih planlanmamış";
            sb.AppendLine($"{i + 1}. \"{task.Title}\" — durum: {task.Status}, {dateRange}");
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                sb.AppendLine($"   Açıklama: {task.Description}");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Same per-task formatting as <see cref="FormatExistingTasks"/>, but returns one string PER task
    /// instead of joining them into a single RAG-upload document — used by AiSuggestionAppService to feed
    /// the FULL task list directly into <see cref="PromptBuilder.AppendExistingTasksList"/> for small
    /// projects, bypassing RAG's semantic-subset retrieval entirely (see
    /// WorkPackageContextRetrievalOptions.FullListThreshold).
    /// </summary>
    public static IReadOnlyList<string> FormatExistingTasksAsIndividualContexts(IReadOnlyList<ExistingTaskInfoDto> tasks)
    {
        var result = new List<string>(tasks.Count);
        for (var i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            var dateRange = task.StartDateUtc.HasValue || task.DueDateUtc.HasValue
                ? $"{task.StartDateUtc:dd.MM.yyyy} – {task.DueDateUtc:dd.MM.yyyy}"
                : "tarih planlanmamış";
            var line = $"{i + 1}. \"{task.Title}\" — durum: {task.Status}, {dateRange}";
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                line += $"\n   Açıklama: {task.Description}";
            }
            result.Add(line);
        }
        return result;
    }

    public static string FormatPendingSuggestionTitles(IReadOnlyList<string> titles)
    {
        var sb = new StringBuilder();
        foreach (var title in titles)
        {
            sb.AppendLine($"- \"{title}\"");
        }
        return sb.ToString();
    }
}
