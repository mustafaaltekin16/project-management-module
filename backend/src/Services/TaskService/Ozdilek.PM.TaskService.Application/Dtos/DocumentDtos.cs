using Ozdilek.PM.TaskService.Domain;

namespace Ozdilek.PM.TaskService.Application.Dtos;

public sealed record ProjectDocumentDto(
    Guid Id,
    Guid ProjectId,
    Guid? NoteId,
    string? UploadedBy,
    string Name,
    DocumentKind Kind,
    long SizeBytes,
    string ContentType,
    DateTimeOffset CreatedAtUtc);

/// <summary>Upload payload assembled by the controller from the incoming multipart form — not bound directly from JSON.</summary>
public sealed record UploadDocumentCommand(string FileName, byte[] Content, string ContentType, Guid? NoteId, string? UploadedBy);
