using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Domain;

namespace Ozdilek.PM.ProjectService.Application.Services;

/// <summary>
/// The SINGLE formula for a project's ProgressPercent/DeviationDays — the canonical replacement for what
/// used to be computed ad hoc in the Angular Detail Page (project-detail-page.ts computeProgressAndDeviation)
/// and PUT back to the server as if it were authoritative data. Ported here verbatim (same weights, same
/// rounding) so behavior doesn't change for users, only WHERE and WHEN it runs — now triggered by
/// ProjectProgressInputsChangedEvent (see ProjectProgressInputsChangedConsumer) and a nightly safety-net
/// job (see ProjectProgressRecomputeJob), not by a specific browser tab happening to be open.
/// </summary>
public static class ProjectProgressCalculator
{
    // Approved and Rejected both count as "resolved" (no more pending work on that item) — progress here
    // measures how much of the process is behind us, not how much of it succeeded. PendingApproval gets
    // partial credit since it's already been submitted; Draft hasn't started the approval process yet.
    private static readonly Dictionary<FeasibilityItemStatusText, double> FeasibilityItemWeight = new()
    {
        [FeasibilityItemStatusText.Approved] = 1,
        [FeasibilityItemStatusText.Rejected] = 1,
        [FeasibilityItemStatusText.PendingApproval] = 0.5,
        [FeasibilityItemStatusText.Draft] = 0
    };

    public enum FeasibilityItemStatusText
    {
        Draft,
        PendingApproval,
        Approved,
        Rejected
    }

    public readonly record struct Result(int ProgressPercent, int DeviationDays);

    public static Result Calculate(
        ProjectType projectType,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly today,
        IReadOnlyList<TimelineTaskGroupData> taskGroups,
        IReadOnlyList<TimelineFeasibilityGroupData> feasibilityGroups)
    {
        var tasks = taskGroups.SelectMany(group => group.Tasks).ToList();
        double? taskRatio = tasks.Count > 0
            ? tasks.Count(task => task.Status.Equals("Done", StringComparison.OrdinalIgnoreCase)) / (double)tasks.Count
            : null;

        double? feasibilityRatio = null;
        if (projectType == ProjectType.FeasibilityBased)
        {
            var items = feasibilityGroups.SelectMany(group => group.Items).ToList();
            if (items.Count > 0)
            {
                var totalWeight = items.Sum(item =>
                    Enum.TryParse<FeasibilityItemStatusText>(item.Status, true, out var status)
                        ? FeasibilityItemWeight[status]
                        : 0);
                feasibilityRatio = totalWeight / items.Count;
            }
        }

        // Tasks carry most of the weight (0.7) with feasibility approvals as a secondary signal (0.3) when
        // both exist; falls back to whichever single signal is actually present, or 0 if neither exists yet.
        var overallRatio = (taskRatio, feasibilityRatio) switch
        {
            ({ } t, { } f) => t * 0.7 + f * 0.3,
            ({ } t, null) => t,
            (null, { } f) => f,
            _ => 0
        };

        var progressPercent = Math.Clamp((int)Math.Round(overallRatio * 100), 0, 100);

        // Sapma: bugüne kadar geçen süreye göre "olması gereken" ilerleme ile gerçek ilerleme arasındaki
        // farkın, toplam proje süresine oranlanarak gün karşılığına çevrilmiş hali. Negatifse proje geride.
        var deviationDays = 0;
        var totalDays = endDate.DayNumber - startDate.DayNumber;
        if (totalDays > 0)
        {
            var elapsedDays = Math.Clamp(today.DayNumber - startDate.DayNumber, 0, totalDays);
            var expectedPercent = elapsedDays / (double)totalDays * 100;
            deviationDays = (int)Math.Round((progressPercent - expectedPercent) / 100 * totalDays);
        }

        return new Result(progressPercent, deviationDays);
    }
}
