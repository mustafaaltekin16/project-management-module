using FluentAssertions;
using Moq;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.TaskService.Application.Interfaces;
using Ozdilek.PM.TaskService.Application.Services;
using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Tests;

public class ProjectDocumentAppServiceTests
{
    [Fact]
    public async Task DeleteAsync_ProjectDocument_RemovesDocumentAndSaves()
    {
        var projectId = Guid.NewGuid();
        var document = ProjectDocument.Create(
            projectId, "faaliyet-raporu.docx", DocumentKind.Word, 4, "application/octet-stream", [1, 2, 3, 4]);
        var repository = new Mock<IProjectDocumentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        var service = new ProjectDocumentAppService(repository.Object, unitOfWork.Object);

        await service.DeleteAsync(projectId, document.Id);

        repository.Verify(x => x.Remove(document), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DocumentBelongsToAnotherProject_ThrowsWithoutRemoving()
    {
        var document = ProjectDocument.Create(
            Guid.NewGuid(), "faaliyet-raporu.docx", DocumentKind.Word, 1, "application/octet-stream", [1]);
        var repository = new Mock<IProjectDocumentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        var service = new ProjectDocumentAppService(repository.Object, unitOfWork.Object);

        var action = () => service.DeleteAsync(Guid.NewGuid(), document.Id);

        await action.Should().ThrowAsync<NotFoundException>();
        repository.Verify(x => x.Remove(It.IsAny<ProjectDocument>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
