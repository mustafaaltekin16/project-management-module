namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Binds the "AiWorkPackageContextRetrieval" configuration section — controls how much of each RAG-retrieved
/// context chunk (existing tasks / pending suggestion titles, see IWorkPackageContextRetrievalService) gets
/// injected into the work-package generation prompt.
/// </summary>
public sealed class WorkPackageContextRetrievalOptions
{
    public const string SectionName = "AiWorkPackageContextRetrieval";

    public int MaxCharsPerRetrievedContext { get; set; } = 4000;

    // RAG'in "ilgili bir alt küme getir" mekanizması az sayıda görev/öneri için fayda değil zarar veriyor
    // — küçük bir listede semantik filtreleme, LLM'e ASLA gösterilmeyen (ve dolayısıyla arasına yeni bir
    // iş paketi yerleştirilemeyen) görevler yaratma riski taşıyor. Görev/öneri sayısı bu eşiğin altındaysa
    // AiSuggestionAppService RAG'e hiç gitmez, TAM listeyi doğrudan prompt'a yazar — bkz.
    // AiSuggestionAppService.GenerateAsync.
    public int FullListThreshold { get; set; } = 20;
}
