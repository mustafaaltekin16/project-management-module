using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Application.Services;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Xunit;

namespace Ozdilek.PM.ProjectService.Tests;

public class ProjectBoardAppServiceTests
{
    [Fact]
    public async Task CreateColumnAsync_DuplicateActiveName_Throws()
    {
        var existing = ProjectBoardColumn.Create("Oteller", "#4B7DD8", 0);
        var columns = new Mock<IProjectBoardColumnRepository>();
        columns
            .Setup(repository => repository.ListAsync(
                It.IsAny<Expression<Func<ProjectBoardColumn, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);
        var sut = CreateSut(columns, new Mock<IProjectRepository>());

        var act = () => sut.CreateColumnAsync(new CreateProjectBoardColumnRequest("oteller", "#B66A3C"));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*aynı ada*");
    }

    [Fact]
    public async Task MoveCardAsync_ToAnotherColumn_PersistsSharedPlacement()
    {
        var targetColumn = ProjectBoardColumn.Create("Oteller", "#B66A3C", 3);
        var movingProject = CreateProject("Taşınacak Proje");
        var existingTargetProject = CreateProject("Mevcut Otel Projesi");
        existingTargetProject.MoveOnBoard(targetColumn.Id, 1024);
        var expectedVersion = movingProject.UpdatedAtUtc ?? movingProject.CreatedAtUtc;

        var columns = new Mock<IProjectBoardColumnRepository>();
        columns
            .Setup(repository => repository.GetByIdAsync(targetColumn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetColumn);
        var projects = new Mock<IProjectRepository>();
        projects
            .Setup(repository => repository.GetByIdAsync(movingProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movingProject);
        projects
            .Setup(repository => repository.ListByBoardColumnAsync(
                targetColumn.Id,
                movingProject.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingTargetProject]);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new ProjectBoardAppService(
            columns.Object,
            projects.Object,
            new Mock<IFeasibilityInfoClient>().Object,
            unitOfWork.Object);

        await sut.MoveCardAsync(
            movingProject.Id,
            new MoveProjectBoardCardRequest(targetColumn.Id, existingTargetProject.Id, null, expectedVersion));

        movingProject.BoardColumnId.Should().Be(targetColumn.Id);
        movingProject.BoardPosition.Should().Be(1024);
        existingTargetProject.BoardPosition.Should().Be(2048);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveCardAsync_WithStaleVersion_Throws()
    {
        var project = CreateProject("Güncel Proje");
        project.MoveOnBoard(ProjectBoardDefaults.OngoingProjectsColumnId, 1024);
        var projects = new Mock<IProjectRepository>();
        projects
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        var sut = CreateSut(new Mock<IProjectBoardColumnRepository>(), projects);

        var act = () => sut.MoveCardAsync(
            project.Id,
            new MoveProjectBoardCardRequest(null, null, null, DateTimeOffset.UnixEpoch));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*başka bir kullanıcı*");
    }

    [Fact]
    public async Task MoveCardAsync_DraftProjectToOngoingColumn_ActivatesProject()
    {
        var ongoingColumn = CreateDefaultColumn(
            ProjectBoardDefaults.OngoingProjectsColumnId,
            "Devam Edenler",
            "#2F9E68",
            1);
        var project = CreateProject("Yeni Proje");
        var expectedVersion = project.UpdatedAtUtc ?? project.CreatedAtUtc;
        var columns = new Mock<IProjectBoardColumnRepository>();
        columns
            .Setup(repository => repository.GetByIdAsync(ongoingColumn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ongoingColumn);
        var projects = new Mock<IProjectRepository>();
        projects
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        projects
            .Setup(repository => repository.ListByBoardColumnAsync(
                ongoingColumn.Id,
                project.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var feasibility = new Mock<IFeasibilityInfoClient>();
        var sut = CreateSut(columns, projects, feasibility);

        await sut.MoveCardAsync(
            project.Id,
            new MoveProjectBoardCardRequest(ongoingColumn.Id, null, null, expectedVersion));

        project.Status.Should().Be(ProjectStatus.Active);
        project.BoardColumnId.Should().Be(ProjectBoardDefaults.OngoingProjectsColumnId);
        feasibility.Verify(
            client => client.IsFullyApprovedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MoveCardAsync_UnapprovedFeasibilityProjectToOngoingColumn_Throws()
    {
        var ongoingColumn = CreateDefaultColumn(
            ProjectBoardDefaults.OngoingProjectsColumnId,
            "Devam Edenler",
            "#2F9E68",
            1);
        var project = CreateProject("Fizibilite Projesi", ProjectType.FeasibilityBased);
        var expectedVersion = project.UpdatedAtUtc ?? project.CreatedAtUtc;
        var columns = new Mock<IProjectBoardColumnRepository>();
        columns
            .Setup(repository => repository.GetByIdAsync(ongoingColumn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ongoingColumn);
        var projects = new Mock<IProjectRepository>();
        projects
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        var feasibility = new Mock<IFeasibilityInfoClient>();
        feasibility
            .Setup(client => client.IsFullyApprovedAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = CreateSut(columns, projects, feasibility);

        var act = () => sut.MoveCardAsync(
            project.Id,
            new MoveProjectBoardCardRequest(ongoingColumn.Id, null, null, expectedVersion));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Fizibilitesi onaylanmamış*");
        project.Status.Should().Be(ProjectStatus.Draft);
        project.BoardColumnId.Should().Be(ProjectBoardDefaults.NewProjectsColumnId);
    }

    [Fact]
    public async Task MoveCardAsync_ActiveProjectBackToNewColumn_Throws()
    {
        var newColumn = CreateDefaultColumn(
            ProjectBoardDefaults.NewProjectsColumnId,
            "Yeni Projeler",
            "#4B7DD8",
            0);
        var project = CreateProject("Aktif Proje");
        project.Activate();
        var expectedVersion = project.UpdatedAtUtc ?? project.CreatedAtUtc;
        var columns = new Mock<IProjectBoardColumnRepository>();
        columns
            .Setup(repository => repository.GetByIdAsync(newColumn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newColumn);
        var projects = new Mock<IProjectRepository>();
        projects
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        var sut = CreateSut(columns, projects);

        var act = () => sut.MoveCardAsync(
            project.Id,
            new MoveProjectBoardCardRequest(newColumn.Id, null, null, expectedVersion));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Yeni Projeler*");
        project.Status.Should().Be(ProjectStatus.Active);
        project.BoardColumnId.Should().Be(ProjectBoardDefaults.OngoingProjectsColumnId);
    }

    [Fact]
    public async Task MoveCardAsync_IncompleteProjectToCompletedColumn_Throws()
    {
        var completedColumn = CreateDefaultColumn(
            ProjectBoardDefaults.CompletedProjectsColumnId,
            "Tamamlananlar",
            "#697386",
            2);
        var project = CreateProject("Devam Eden Proje");
        project.Activate();
        var expectedVersion = project.UpdatedAtUtc ?? project.CreatedAtUtc;
        var columns = new Mock<IProjectBoardColumnRepository>();
        columns
            .Setup(repository => repository.GetByIdAsync(completedColumn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedColumn);
        var projects = new Mock<IProjectRepository>();
        projects
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        var sut = CreateSut(columns, projects);

        var act = () => sut.MoveCardAsync(
            project.Id,
            new MoveProjectBoardCardRequest(completedColumn.Id, null, null, expectedVersion));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*%100*");
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task ArchiveColumnAsync_WithProjectsAndNoTarget_Throws()
    {
        var source = ProjectBoardColumn.Create("Kaldırılacak", "#697386", 4);
        var columns = new Mock<IProjectBoardColumnRepository>();
        columns
            .Setup(repository => repository.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        var projects = new Mock<IProjectRepository>();
        projects
            .Setup(repository => repository.ListByBoardColumnAsync(source.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateProject("Bağlı Proje")]);
        var sut = CreateSut(columns, projects);

        var act = () => sut.ArchiveColumnAsync(source.Id, null);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*hedef sütun*");
        source.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveColumnAsync_DefaultNewProjectsColumn_Throws()
    {
        var source = ProjectBoardColumn.Create("Yeni Projeler", "#4B7DD8", 0);
        typeof(ProjectBoardColumn)
            .GetProperty(nameof(ProjectBoardColumn.Id))!
            .SetValue(source, ProjectBoardDefaults.NewProjectsColumnId);
        var columns = new Mock<IProjectBoardColumnRepository>();
        columns
            .Setup(repository => repository.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        var sut = CreateSut(columns, new Mock<IProjectRepository>());

        var act = () => sut.ArchiveColumnAsync(source.Id, null);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*varsayılan sütun*");
    }

    private static ProjectBoardAppService CreateSut(
        Mock<IProjectBoardColumnRepository> columns,
        Mock<IProjectRepository> projects,
        Mock<IFeasibilityInfoClient>? feasibility = null) =>
        new(
            columns.Object,
            projects.Object,
            (feasibility ?? new Mock<IFeasibilityInfoClient>()).Object,
            new Mock<IUnitOfWork>().Object);

    private static ProjectBoardColumn CreateDefaultColumn(Guid id, string name, string color, int sortOrder)
    {
        var column = ProjectBoardColumn.Create(name, color, sortOrder);
        typeof(ProjectBoardColumn)
            .GetProperty(nameof(ProjectBoardColumn.Id))!
            .SetValue(column, id);
        return column;
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
