using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Domain;

/// <summary>A single entry in a project's description/notes feed (the "Proje Açıklaması" tab composer).</summary>
public class ProjectNote : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;

    private ProjectNote() { }

    public ProjectNote(Guid projectId, string author, string text)
    {
        ProjectId = projectId;
        Author = author;
        Text = text;
    }

    public void UpdateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("Not metni zorunludur.");
        }

        Text = text.Trim();
    }
}
