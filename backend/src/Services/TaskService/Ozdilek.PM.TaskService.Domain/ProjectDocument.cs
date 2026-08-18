using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.TaskService.Domain;

public class ProjectDocument : BaseEntity
{
    public Guid ProjectId { get; private set; }
    // Loose reference to a ProjectService ProjectNote — no FK across service boundaries (same pattern as
    // TaskGroup/ProjectDocument's bare ProjectId), just enough to let the Overview tab show which
    // attachments belong to which comment.
    public Guid? NoteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DocumentKind Kind { get; private set; }
    public long SizeBytes { get; private set; }
    public string ContentType { get; private set; } = "application/octet-stream";
    public byte[] Content { get; private set; } = [];
    public string? UploadedBy { get; private set; }

    private ProjectDocument() { }

    public static ProjectDocument Create(
        Guid projectId, string name, DocumentKind kind, long sizeBytes, string contentType, byte[] content,
        Guid? noteId = null, string? uploadedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Doküman adı zorunludur.");
        }

        return new ProjectDocument
        {
            ProjectId = projectId,
            NoteId = noteId,
            Name = name.Trim(),
            Kind = kind,
            SizeBytes = sizeBytes,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Content = content,
            UploadedBy = string.IsNullOrWhiteSpace(uploadedBy) ? null : uploadedBy.Trim()
        };
    }

    public static DocumentKind KindFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "doc" or "docx" => DocumentKind.Word,
            "ppt" or "pptx" => DocumentKind.PowerPoint,
            "xls" or "xlsx" => DocumentKind.Excel,
            "pdf" => DocumentKind.Pdf,
            "jpg" or "jpeg" or "png" or "webp" => DocumentKind.Image,
            "mov" or "mp4" or "webm" => DocumentKind.Video,
            _ => DocumentKind.File
        };
    }
}
