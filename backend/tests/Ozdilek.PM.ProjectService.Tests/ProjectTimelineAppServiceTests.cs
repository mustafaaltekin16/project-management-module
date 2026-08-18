using FluentAssertions;
using Moq;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Application.Services;
using Ozdilek.PM.ProjectService.Domain;
using Xunit;

namespace Ozdilek.PM.ProjectService.Tests;

public class ProjectTimelineAppServiceTests
{
    [Fact]
    public async Task GetAsync_UsesExplicitWorkPackageAndProcessLinks()
    {
        var project = CreateProject(ProjectType.MultiUnit);
        project.AddDepartment(
            "Teknik Alım",
            "Teknik Müdürlük",
            "Ahmet Görür",
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 8, 1));
        var workPackageId = project.Departments.Single().Id;
        var taskClient = new Mock<IProjectTaskTimelineClient>();
        taskClient
            .Setup(client => client.ListByProjectAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TimelineTaskGroupData(
                    Guid.NewGuid(),
                    workPackageId,
                    "PriceComparison",
                    1,
                    "Başlığı değiştirilmiş grup",
                    "Eşleşmeyen alt başlık",
                    [
                        new TimelineTaskItemData(
                            Guid.NewGuid(),
                            "Teklifleri değerlendir",
                            "Zeynel Mutlu",
                            Guid.NewGuid(),
                            "Satın Alma",
                            "Done",
                            null,
                            null)
                    ])
            ]);
        var feasibilityClient = new Mock<IProjectFeasibilityTimelineClient>();
        feasibilityClient
            .Setup(client => client.ListByProjectAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var service = CreateService(project, taskClient.Object, feasibilityClient.Object);

        var result = await service.GetAsync(project.Id);

        result.WorkPackages.Should().ContainSingle();
        result.WorkPackages[0].Processes.Should().ContainSingle();
        var priceComparison = result.WorkPackages[0].Processes
            .Single(process => process.Type == ProjectTimelineProcessType.PriceComparison);
        priceComparison.State.Should().Be(ProjectTimelineState.Completed);
        priceComparison.OwnerName.Should().Be("Zeynel Mutlu");
    }

    [Fact]
    public async Task GetAsync_SimpleProjectUsesDepartmentRowsAsTimelineGroups()
    {
        var project = CreateProject(ProjectType.Simple);
        project.AddDepartment("", "BT Departmanı", "Ahmet Görür", null, null);
        project.AddDepartment("", "E Ticaret Departmanı", "Merve Tezciler", null, null);
        var taskClient = new Mock<IProjectTaskTimelineClient>();
        taskClient
            .Setup(client => client.ListByProjectAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimelineTaskGroupData>());
        var feasibilityClient = new Mock<IProjectFeasibilityTimelineClient>();
        feasibilityClient
            .Setup(client => client.ListByProjectAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TimelineFeasibilityGroupData>());
        var service = CreateService(
            project,
            taskClient.Object,
            feasibilityClient.Object);

        var result = await service.GetAsync(project.Id);

        result.WorkPackages.Should().HaveCount(2);
        result.WorkPackages[0].Title.Should().Be("BT Departmanı");
        result.WorkPackages[0].ManagerName.Should().Be("Ahmet Görür");
        result.WorkPackages[0].StartDate.Should().Be(project.StartDate);
        result.WorkPackages[0].EndDate.Should().Be(project.EndDate);
        result.WorkPackages[0].Processes.Should().BeEmpty();
        result.WorkPackages[1].Title.Should().Be("E Ticaret Departmanı");
    }

    private static ProjectTimelineAppService CreateService(
        Project project,
        IProjectTaskTimelineClient taskClient,
        IProjectFeasibilityTimelineClient feasibilityClient)
    {
        var repository = new Mock<IProjectRepository>();
        repository
            .Setup(item => item.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        return new ProjectTimelineAppService(repository.Object, taskClient, feasibilityClient);
    }

    private static Project CreateProject(ProjectType type) => Project.Create(
        "Satın Alma Projesi",
        "Açıklama",
        "Ahmet Görür",
        null,
        "BT Departmanı",
        type,
        500_000,
        "TRY",
        new DateOnly(2026, 7, 1),
        new DateOnly(2026, 9, 1),
        null,
        []);
}
