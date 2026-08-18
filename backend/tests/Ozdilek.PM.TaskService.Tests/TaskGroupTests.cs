using FluentAssertions;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.TaskService.Domain;
using Xunit;

namespace Ozdilek.PM.TaskService.Tests;

public class TaskGroupTests
{
    [Fact]
    public void Create_WithTimelineLink_PersistsStableProcessIdentity()
    {
        var workPackageId = Guid.NewGuid();

        var group = TaskGroup.Create(
            Guid.NewGuid(),
            "Kullanıcının değiştirebileceği başlık",
            "Alt başlık",
            workPackageId,
            TaskProcessType.PriceComparison,
            2);

        group.WorkPackageId.Should().Be(workPackageId);
        group.ProcessType.Should().Be(TaskProcessType.PriceComparison);
        group.TimelineSortOrder.Should().Be(2);
    }

    [Fact]
    public void AddTask_WithoutDependency_Succeeds()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Fizibilite Listesi", "BT Alımı (Ana Grup)");

        var task = group.AddTask("Bilgisayar Alımları", "Zeynel Mutlu", null, 16, isMainTask: true, dependsOnTaskId: null);

        group.Tasks.Should().ContainSingle();
        task.Depth.Should().Be(0);
    }

    [Fact]
    public void AddTask_DependingOnUnknownTask_Throws()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Fizibilite Listesi", "BT Alımı (Ana Grup)");

        var act = () => group.AddTask("Yazarkasa Alımları", "Zeynel Mutlu", null, 8, false, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddTask_DependingOnExistingTask_IncrementsDepth()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Fizibilite Listesi", "BT Alımı (Ana Grup)");
        var parent = group.AddTask("Bilgisayar Alımları", "Zeynel Mutlu", null, 16, true, null);

        var child = group.AddTask("Yazarkasa Alımları", "Zeynel Mutlu", null, 8, false, parent.Id);

        child.Depth.Should().Be(1);
        child.DependsOnTaskId.Should().Be(parent.Id);
    }

    [Fact]
    public void AddTask_DependingOnSubTask_Throws()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Fizibilite Listesi", "BT Alımı (Ana Grup)");
        var mainTask = group.AddTask("Bilgisayar Alımları", "Zeynel Mutlu", null, 16, true, null);
        var subTask = group.AddTask("Yazarkasa Alımları", "Zeynel Mutlu", null, 8, false, mainTask.Id);

        var act = () => group.AddTask("Kablo Alımları", "Zeynel Mutlu", null, 2, false, subTask.Id);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangeTaskStatus_UnknownTask_ThrowsNotFound()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Fizibilite Listesi", "BT Alımı (Ana Grup)");

        var act = () => group.ChangeTaskStatus(Guid.NewGuid(), KanbanStatus.Done, "Selin Güler");

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void ChangeTaskStatus_ExistingTask_UpdatesStatus()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Fiyat Karşılaştırma", "BT Alımı (Ana Grup)");
        var task = group.AddTask("Kamera Alımları", "Yasin Ters", null, null, true, null, assigneeEmployeeId: Guid.NewGuid());

        group.ChangeTaskStatus(task.Id, KanbanStatus.InProgress, "Yasin Ters");

        group.Tasks.Single().Status.Should().Be(KanbanStatus.InProgress);
        group.Tasks.Single().Comments.Should().ContainSingle(c =>
            c.Text == "Görev durumu Bekliyor durumundan Devam Ediyor durumuna getirildi.");
    }

    [Fact]
    public void ChangeTaskStatus_UnassignedTask_CannotStart()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Genel Görevler", "");
        var task = group.AddTask("Atama bekleyen görev", "Atanmamış", null, 4, true, null);

        var act = () => group.ChangeTaskStatus(task.Id, KanbanStatus.InProgress, "Selin Güler");

        act.Should().Throw<DomainException>().WithMessage("*sorumlu atayın*");
    }

    [Fact]
    public void ChangeTaskStatus_MainTaskWithOpenSubtask_CannotComplete()
    {
        var employeeId = Guid.NewGuid();
        var group = TaskGroup.Create(Guid.NewGuid(), "Genel Görevler", "");
        var main = group.AddTask("Ana görev", "Selin Güler", null, 8, true, null, assigneeEmployeeId: employeeId);
        group.AddTask("Açık alt görev", "Selin Güler", null, 2, false, main.Id, assigneeEmployeeId: employeeId);

        var act = () => group.ChangeTaskStatus(main.Id, KanbanStatus.Done, "Selin Güler");

        act.Should().Throw<DomainException>().WithMessage("*Açık alt görev*");
    }

    [Fact]
    public void ChangeTaskStatus_Done_RecordsCompletionActorAndTime()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Genel Görevler", "");
        var task = group.AddTask("Tek görev", "Selin Güler", null, 8, true, null, assigneeEmployeeId: Guid.NewGuid());

        group.ChangeTaskStatus(task.Id, KanbanStatus.Done, "Selin Güler");

        task.Status.Should().Be(KanbanStatus.Done);
        task.CompletedAtUtc.Should().NotBeNull();
        task.CompletedBy.Should().Be("Selin Güler");
    }

    [Fact]
    public void AddCommentToTask_AppendsComment()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Fizibilite Listesi", "BT Alımı (Ana Grup)");
        var task = group.AddTask("Kamera Alımları", "Yasin Ters", null, null, true, null);

        group.AddCommentToTask(task.Id, "Selin Güler", "Teklif toplama süreci başladı.");

        group.Tasks.Single().Comments.Should().ContainSingle(c => c.Text == "Teklif toplama süreci başladı.");
    }

    [Fact]
    public void UpdateTask_ChangesEditableFields_AndKeepsAiSource()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Genel Görevler", "");
        var sourceId = Guid.NewGuid();
        var task = group.AddTask("Eski başlık", "Atanmamış", null, 8, true, null, true, sourceId);

        group.UpdateTask(
            task.Id, "Yeni başlık", "Selin Güler", Guid.NewGuid(), "Teknik Ofis", 12,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), "İyileştirme", "Yeni açıklama");

        task.Title.Should().Be("Yeni başlık");
        task.EffortHours.Should().Be(12);
        task.IsAiGenerated.Should().BeTrue();
        task.SourceAiSuggestionItemId.Should().Be(sourceId);
    }

    [Fact]
    public void ArchiveTask_MainTask_ArchivesItsSubtasks()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Genel Görevler", "");
        var main = group.AddTask("Ana görev", "Selin Güler", null, 8, true, null);
        var child = group.AddTask("Alt görev", "Selin Güler", null, 2, false, main.Id);

        var archivedCount = group.ArchiveTask(main.Id);

        archivedCount.Should().Be(2);
        main.ArchivedAtUtc.Should().NotBeNull();
        child.ArchivedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ArchiveTask_WithExternalDependent_BlocksArchive()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Genel Görevler", "");
        var main = group.AddTask("Ön koşul", "Selin Güler", null, 8, true, null);
        group.AddTask("Bağlı ana görev", "Selin Güler", null, 2, true, main.Id);

        var act = () => group.ArchiveTask(main.Id);

        act.Should().Throw<DomainException>().WithMessage("*Bağlı ana görev*");
    }

    [Fact]
    public void RestoreTask_MainTask_RestoresItsSubtasks()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Genel Görevler", "");
        var main = group.AddTask("Ana görev", "Selin Güler", null, 8, true, null);
        var child = group.AddTask("Alt görev", "Selin Güler", null, 2, false, main.Id);
        group.ArchiveTask(main.Id);

        var restoredCount = group.RestoreTask(main.Id);

        restoredCount.Should().Be(2);
        main.ArchivedAtUtc.Should().BeNull();
        child.ArchivedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CopyTask_MainTask_CopiesItsSubtasks_AsUserTasks()
    {
        var group = TaskGroup.Create(Guid.NewGuid(), "Genel Görevler", "");
        var main = group.AddTask("AI ana görev", "Selin Güler", null, 8, true, null, true, Guid.NewGuid());
        group.AddTask("Alt görev", "Selin Güler", null, 2, false, main.Id, true, Guid.NewGuid());

        var copiedCount = group.CopyTask(main.Id);

        copiedCount.Should().Be(2);
        var copiedMain = group.Tasks.Single(t => t.Title == "AI ana görev (Kopya)");
        copiedMain.IsAiGenerated.Should().BeFalse();
        group.Tasks.Should().ContainSingle(t => t.DependsOnTaskId == copiedMain.Id && t.Title == "Alt görev");
    }
}
