using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Application.Services;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Persistence;
using Xunit;

namespace Ozdilek.PM.ProjectService.Tests;

public class ProjectProgressAppServiceTests
{
    [Fact]
    public async Task RecomputeProgressAsync_DraftProjectWithInProgressTask_ActivatesProject()
    {
        var project = CreateProject("Yeni Proje");
        var taskGroups = new List<TimelineTaskGroupData>
        {
            new(Guid.NewGuid(), null, null, 0, "Grup", "", [
                new TimelineTaskItemData(Guid.NewGuid(), "Görev 1", "Ahmet Görür", null, null, "InProgress", null, null)
            ])
        };
        var sut = CreateSut(project, taskGroups, out _);

        await sut.RecomputeProgressAsync(project.Id);

        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task RecomputeProgressAsync_DraftProjectWithOnlyTodoTasks_StaysDraft()
    {
        var project = CreateProject("Yeni Proje");
        var taskGroups = new List<TimelineTaskGroupData>
        {
            new(Guid.NewGuid(), null, null, 0, "Grup", "", [
                new TimelineTaskItemData(Guid.NewGuid(), "Görev 1", "Ahmet Görür", null, null, "Todo", null, null)
            ])
        };
        var sut = CreateSut(project, taskGroups, out _);

        await sut.RecomputeProgressAsync(project.Id);

        project.Status.Should().Be(ProjectStatus.Draft);
    }

    [Fact]
    public async Task RecomputeProgressAsync_DraftProjectWithNoTasks_StaysDraft()
    {
        var project = CreateProject("Yeni Proje");
        var sut = CreateSut(project, [], out _);

        await sut.RecomputeProgressAsync(project.Id);

        project.Status.Should().Be(ProjectStatus.Draft);
    }

    [Fact]
    public async Task RecomputeProgressAsync_DraftProjectWithAllTasksDone_ActivatesAndCompletesInSameCall()
    {
        var project = CreateProject("Yeni Proje");
        var taskGroups = new List<TimelineTaskGroupData>
        {
            new(Guid.NewGuid(), null, null, 0, "Grup", "", [
                new TimelineTaskItemData(Guid.NewGuid(), "Görev 1", "Ahmet Görür", null, null, "Done", null, null)
            ])
        };
        var sut = CreateSut(project, taskGroups, out _);

        await sut.RecomputeProgressAsync(project.Id);

        project.Status.Should().Be(ProjectStatus.Completed);
        project.ProgressPercent.Should().Be(100);
    }

    private static ProjectProgressAppService CreateSut(
        Project project,
        IReadOnlyList<TimelineTaskGroupData> taskGroups,
        out Mock<IUnitOfWork> unitOfWork)
    {
        var projects = new Mock<IProjectRepository>();
        projects.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var taskTimeline = new Mock<IProjectTaskTimelineClient>();
        taskTimeline
            .Setup(client => client.ListByProjectAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taskGroups);

        unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new ProjectProgressAppService(
            projects.Object,
            taskTimeline.Object,
            new Mock<IProjectFeasibilityTimelineClient>().Object,
            unitOfWork.Object,
            new Mock<ILogger<ProjectProgressAppService>>().Object);
    }

    private static Project CreateProject(string name, ProjectType type = ProjectType.Simple) => Project.Create(
        name,
        "Açıklama",
        "Ahmet Görür",
        null,
        "Arge",
        type,
        100_000,
        "TRY",
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 3, 1),
        null,
        []);
}
