using FluentAssertions;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Xunit;

namespace Ozdilek.PM.ProjectService.Tests;

public class ProjectTests
{
    private static Project CreateSimpleProject() => Project.Create(
        "Ocak Seti Kurulması", "açıklama", "Ahmet Görür", null, "Şafabat Lokantası",
        ProjectType.Simple, 100_000m, "TRY",
        new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1), null, []);

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => Project.Create(
            "", "açıklama", "Ahmet Görür", null, "Birim", ProjectType.Simple,
            10_000m, "TRY", new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), null, []);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_Throws()
    {
        var act = () => Project.Create(
            "Proje", "açıklama", "Ahmet Görür", null, "Birim", ProjectType.Simple,
            10_000m, "TRY", new DateOnly(2026, 3, 1), new DateOnly(2026, 1, 1), null, []);

        act.Should().Throw<DomainException>().WithMessage("*Bitiş tarihi*");
    }

    [Fact]
    public void AddDepartment_OnSimpleProject_Succeeds()
    {
        var project = CreateSimpleProject();

        project.AddDepartment("", "BT Departmanı", "Selin Akar", null, null);

        project.Departments.Should().ContainSingle();
        project.Departments.Single().DepartmentName.Should().Be("BT Departmanı");
    }

    [Fact]
    public void AddDepartment_OnMultiUnitProject_Succeeds()
    {
        var project = Project.Create(
            "Portföy Satın Alması", "açıklama", "Selin Akar", "Ahmet Görür", "Depo Müdürlüğü",
            ProjectType.MultiUnit, 1_400_000m, "TRY",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), null, []);

        project.AddDepartment("BT Alımı (Ana Grup)", "BT Müdürlüğü", "Selin G.", null, null);

        project.Departments.Should().ContainSingle();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void UpdateProgress_OutOfRange_Throws(int invalidProgress)
    {
        var project = CreateSimpleProject();

        var act = () => project.UpdateProgress(invalidProgress, 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateProgress_To100_MarksCompletedOnlyWhenActive()
    {
        var project = CreateSimpleProject();
        project.Activate();

        project.UpdateProgress(100, 0);

        project.Status.Should().Be(ProjectStatus.Completed);
        project.BoardColumnId.Should().Be(ProjectBoardDefaults.CompletedProjectsColumnId);
    }

    [Fact]
    public void Activate_MovesProjectToOngoingColumn()
    {
        var project = CreateSimpleProject();

        project.Activate();

        project.Status.Should().Be(ProjectStatus.Active);
        project.BoardColumnId.Should().Be(ProjectBoardDefaults.OngoingProjectsColumnId);
    }

    [Fact]
    public void Activate_WhenManuallyPlacedInCustomColumn_DoesNotMoveTheCard()
    {
        var project = CreateSimpleProject();
        var customColumnId = Guid.NewGuid();
        project.MoveOnBoard(customColumnId, 1024);

        project.Activate();

        project.Status.Should().Be(ProjectStatus.Active);
        project.BoardColumnId.Should().Be(customColumnId);
    }

    [Fact]
    public void UpdateProgress_To100_WhenManuallyPlacedInCustomColumn_DoesNotMoveTheCard()
    {
        var project = CreateSimpleProject();
        project.Activate();
        var customColumnId = Guid.NewGuid();
        project.MoveOnBoard(customColumnId, 1024);

        project.UpdateProgress(100, 0);

        project.Status.Should().Be(ProjectStatus.Completed);
        project.BoardColumnId.Should().Be(customColumnId);
    }

    [Fact]
    public void Activate_WhenNotDraft_Throws()
    {
        var project = CreateSimpleProject();
        project.Activate();

        var act = project.Activate;

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Cancelled)]
    public void UpdateProgress_WhenProjectIsTerminal_Throws(ProjectStatus terminalStatus)
    {
        var project = CreateSimpleProject();
        project.Activate();
        if (terminalStatus == ProjectStatus.Completed)
        {
            project.UpdateProgress(100, 0);
        }
        else
        {
            project.Cancel();
        }

        var act = () => project.UpdateProgress(50, 0);

        act.Should().Throw<DomainException>().WithMessage("*ilerlemesi güncellenemez*");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancel_WhenProjectIsOpen_MarksCancelled(bool activateFirst)
    {
        var project = CreateSimpleProject();
        if (activateFirst)
        {
            project.Activate();
        }

        project.Cancel();

        project.Status.Should().Be(ProjectStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var project = CreateSimpleProject();
        project.Cancel();

        var act = project.Cancel;

        act.Should().Throw<DomainException>();
    }
}
