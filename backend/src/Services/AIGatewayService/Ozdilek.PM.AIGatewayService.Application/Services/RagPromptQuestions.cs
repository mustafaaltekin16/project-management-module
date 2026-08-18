namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Pure, I/O-free builders for the synthetic questions AiSuggestionAppService asks the RAG service to
/// retrieve document context for work-package generation — kept separate from AiSuggestionAppService so
/// the exact wording is trivial to unit test on its own (see PromptBuilder's own separation rationale).
/// </summary>
public static class RagPromptQuestions
{
    public static string BuildWorkPackageRetrievalQuestion(string? extraInstructions) =>
        "Bu projede iş paketi (work package) önerileri hazırlamak için gereken tüm kapsam, gereksinim, " +
        "teknik detay, kısıt ve teslim koşullarını yüklenen dokümanlardan özetle. " +
        $"Ek talimatlar: {(string.IsNullOrWhiteSpace(extraInstructions) ? "(yok)" : extraInstructions)}";

    public static string BuildExistingTaskRetrievalQuestion(string? extraInstructions) =>
        "Aşağıdaki doküman bu projenin TÜM mevcut ana görevlerinin listesidir. Bu projeye yeni iş paketi " +
        "önerileri hazırlarken dikkate alınması GEREKEN — yeni önerilerle konu/kapsam olarak örtüşebilecek, " +
        "TEKRAR üretilmemesi gereken ya da ardışık sırada aralarında bir boşluk/eksik adım olabilecek " +
        "görevleri listele. Her ilgili görevin başlığını, durumunu ve tarih aralığını (varsa açıklamasını) " +
        "TIRNAK İÇİNDEKİYLE BİREBİR AYNI, hiç değiştirmeden/kısaltmadan alıntıla — bu başlıklar daha sonra " +
        "makine tarafından birebir karşılaştırılacak. " +
        $"Ek talimatlar: {(string.IsNullOrWhiteSpace(extraInstructions) ? "(yok)" : extraInstructions)}";

    public static string BuildPendingSuggestionRetrievalQuestion(string? extraInstructions) =>
        "Aşağıdaki doküman bu proje için hâlâ karar bekleyen (onaylanmamış/reddedilmemiş) öneri " +
        "başlıklarının listesidir. Yeni üretilecek iş paketi önerileriyle konu/kapsam olarak örtüşebilecek " +
        "ya da neredeyse aynı olabilecek başlıkları TIRNAK İÇİNDEKİYLE BİREBİR AYNI, hiç değiştirmeden " +
        "alıntıla — bu başlıklar daha sonra makine tarafından birebir karşılaştırılacak. " +
        $"Ek talimatlar: {(string.IsNullOrWhiteSpace(extraInstructions) ? "(yok)" : extraInstructions)}";
}
