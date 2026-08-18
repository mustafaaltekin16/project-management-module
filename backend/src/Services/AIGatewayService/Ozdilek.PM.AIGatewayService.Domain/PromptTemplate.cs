using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.AIGatewayService.Domain;

/// <summary>
/// A project-type-scoped prompt template. Keeps the LLM instructions standardized instead of every
/// caller free-texting a prompt — see PROJE_BRIEF: "yapay zekâya gönderilecek istemlerin proje
/// türlerine göre standartlaştırıldığı ... bir prompt yapısı tasarlanacaktır."
/// Placeholders resolved by <c>PromptBuilder</c>: {ProjectName}, {ProjectDescription}, {ProjectType}, {Unit}, {ExtraInstructions}.
/// </summary>
public class PromptTemplate : BaseEntity
{
    public string ProjectType { get; private set; } = string.Empty;
    public string TemplateText { get; private set; } = string.Empty;

    private PromptTemplate() { }

    public static PromptTemplate Create(string projectType, string templateText)
    {
        if (string.IsNullOrWhiteSpace(projectType))
        {
            throw new DomainException("Proje türü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(templateText))
        {
            throw new DomainException("Şablon metni zorunludur.");
        }

        return new PromptTemplate { ProjectType = projectType, TemplateText = templateText };
    }
}
