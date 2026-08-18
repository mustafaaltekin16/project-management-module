using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.TaskService.Application.Dtos;
using Ozdilek.PM.TaskService.Application.Interfaces;
using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Application.Services;

public sealed class ProjectDocumentAppService(IProjectDocumentRepository documents, IUnitOfWork unitOfWork)
{
    public async Task<ProjectDocumentDto> UploadAsync(Guid projectId, UploadDocumentCommand request, CancellationToken ct = default)
    {
        var kind = ProjectDocument.KindFromFileName(request.FileName);
        var document = ProjectDocument.Create(
            projectId, request.FileName, kind, request.Content.LongLength, request.ContentType, request.Content,
            request.NoteId, request.UploadedBy);
        await documents.AddAsync(document, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(document);
    }

    public async Task<List<ProjectDocumentDto>> ListForProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var result = await documents.ListByProjectAsync(projectId, ct);
        return result.Select(ToDto).ToList();
    }

    public async Task<ProjectDocument> GetContentAsync(Guid projectId, Guid documentId, CancellationToken ct = default)
    {
        var document = await documents.GetByIdAsync(documentId, ct);
        if (document is null || document.ProjectId != projectId)
        {
            throw new NotFoundException("Doküman bulunamadı.");
        }

        return document;
    }

    public async Task DeleteAsync(Guid projectId, Guid documentId, CancellationToken ct = default)
    {
        var document = await documents.GetByIdAsync(documentId, ct);
        if (document is null || document.ProjectId != projectId)
        {
            // Do not reveal whether a document with the supplied id belongs to another project.
            throw new NotFoundException("Doküman bulunamadı.");
        }

        documents.Remove(document);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static ProjectDocumentDto ToDto(ProjectDocument document) =>
        new(document.Id, document.ProjectId, document.NoteId, document.UploadedBy, document.Name, document.Kind,
            document.SizeBytes, document.ContentType, document.CreatedAtUtc);
}
