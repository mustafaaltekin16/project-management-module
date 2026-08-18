using FluentAssertions;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Application.Services;
using Ozdilek.PM.ProjectService.Domain;
using Xunit;

namespace Ozdilek.PM.ProjectService.Tests;

public class ProjectProgressCalculatorTests
{
    private static readonly DateOnly Start = new(2026, 1, 1);
    private static readonly DateOnly End = new(2026, 4, 11); // 100 days after Start
    private static readonly DateOnly HalfwayToday = new(2026, 2, 20); // 50 days after Start

    private static TimelineTaskGroupData TasksGroup(params string[] statuses) => new(
        Guid.NewGuid(), null, null, 0, "Grup", "",
        statuses.Select(status => new TimelineTaskItemData(
            Guid.NewGuid(), "Görev", "", null, null, status, null, null)).ToList());

    private static TimelineFeasibilityGroupData FeasibilityGroup(params string[] statuses) => new(
        Guid.NewGuid(), null, 0, "Grup",
        statuses.Select(status => new TimelineFeasibilityItemData(status, [])).ToList());

    [Fact]
    public void Calculate_NoTasksNoFeasibility_ZeroProgressAndBehindByElapsedTime()
    {
        var result = ProjectProgressCalculator.Calculate(
            ProjectType.Simple, Start, End, HalfwayToday, [], []);

        result.ProgressPercent.Should().Be(0);
        result.DeviationDays.Should().Be(-50); // 50% of the schedule elapsed, 0% done
    }

    [Fact]
    public void Calculate_AllTasksDone_AheadOfSchedule()
    {
        var result = ProjectProgressCalculator.Calculate(
            ProjectType.Simple, Start, End, HalfwayToday,
            [TasksGroup("Done", "Done")], []);

        result.ProgressPercent.Should().Be(100);
        result.DeviationDays.Should().Be(50); // fully done at the halfway point
    }

    [Fact]
    public void Calculate_HalfTasksDoneAtHalfwayPoint_OnSchedule()
    {
        var result = ProjectProgressCalculator.Calculate(
            ProjectType.Simple, Start, End, HalfwayToday,
            [TasksGroup("Done", "Todo")], []);

        result.ProgressPercent.Should().Be(50);
        result.DeviationDays.Should().Be(0);
    }

    [Fact]
    public void Calculate_FeasibilityBased_WeighsTasks70PercentAndFeasibility30Percent()
    {
        var result = ProjectProgressCalculator.Calculate(
            ProjectType.FeasibilityBased, Start, End, HalfwayToday,
            [TasksGroup("Done", "Done", "Done", "Done")], // taskRatio = 1.0
            [FeasibilityGroup("Approved", "Draft")]); // feasibilityRatio = 0.5

        // 1.0 * 0.7 + 0.5 * 0.3 = 0.85 -> 85%
        result.ProgressPercent.Should().Be(85);
    }

    [Fact]
    public void Calculate_NonFeasibilityBasedProject_IgnoresFeasibilityItemsEntirely()
    {
        var result = ProjectProgressCalculator.Calculate(
            ProjectType.MultiUnit, Start, End, HalfwayToday,
            [TasksGroup("Done", "Todo")], // taskRatio = 0.5
            [FeasibilityGroup("Draft", "Draft")]); // would drag progress to 0 if it were counted

        result.ProgressPercent.Should().Be(50);
    }

    [Fact]
    public void Calculate_FeasibilityBasedWithNoTasksYet_UsesFeasibilityRatioAlone()
    {
        var result = ProjectProgressCalculator.Calculate(
            ProjectType.FeasibilityBased, Start, End, HalfwayToday,
            [],
            [FeasibilityGroup("Approved", "Rejected", "Draft", "Draft")]); // (1+1+0+0)/4 = 0.5

        result.ProgressPercent.Should().Be(50);
    }

    [Fact]
    public void Calculate_ZeroDurationProject_DeviationIsAlwaysZero()
    {
        var result = ProjectProgressCalculator.Calculate(
            ProjectType.Simple, Start, Start, Start,
            [TasksGroup("Todo")], []);

        result.DeviationDays.Should().Be(0);
    }

    [Fact]
    public void Calculate_ProgressPercent_IsAlwaysClampedBetween0And100()
    {
        var result = ProjectProgressCalculator.Calculate(
            ProjectType.Simple, Start, End, HalfwayToday,
            [TasksGroup("Done")], []);

        result.ProgressPercent.Should().BeInRange(0, 100);
    }
}
