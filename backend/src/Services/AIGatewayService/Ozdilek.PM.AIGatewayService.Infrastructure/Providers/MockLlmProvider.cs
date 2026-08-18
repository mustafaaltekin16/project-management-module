using Ozdilek.PM.AIGatewayService.Application.Interfaces;

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Providers;

/// <summary>
/// Default provider — no external API key required. Returns a fixed but realistic work-package list so
/// the whole approve → task-creation flow can be exercised end-to-end without a live LLM subscription.
/// Swap "Ai:Provider" to OpenAI/Anthropic/Gemini once real credentials are available (see README).
/// </summary>
public sealed class MockLlmProvider : ILlmProvider
{
    public string Name => "Mock";

    public Task<string> GenerateWorkPackagesJsonAsync(string prompt, CancellationToken ct = default) =>
        Task.FromResult("""
        [
          {"title": "Teknik standart ve mevzuat uygunluk kontrolü", "department": "Teknik Müdürlük", "effortHours": 12, "sourceDocument": "Teknik Standartlar.pdf",
           "description": "Projenin teknik şartnamesi, yürürlükteki teknik standartlar ve mevzuatla karşılaştırılır; uyumsuz maddeler tespit edilip düzeltme önerileriyle birlikte raporlanır.",
           "sequenceNote": "Mevcut görev yok, projenin başında yapılabilir.", "insertAfterTaskTitle": null, "sequenceRank": 1,
           "isAtProjectStart": true,
           "activities": [
             {"title": "İlgili mevzuat ve standartların listelenmesi", "effortHours": 3},
             {"title": "Teknik şartnamenin standartlarla karşılaştırılması", "effortHours": 5},
             {"title": "Uygunluk raporunun hazırlanması", "effortHours": 4}
           ]},
          {"title": "Fizibilite bütçe kalemlerinin proje görevleriyle eşleştirilmesi", "department": "BT Grubu", "effortHours": 16, "sourceDocument": "FizibiliteRaporu.pdf",
           "description": "Fizibilite raporundaki bütçe kalemleri, projenin gerçek görev listesiyle bire bir eşleştirilir; sapma gösteren kalemler işaretlenip proje yöneticisine özet bir rapor sunulur.",
           "sequenceNote": "Teknik uygunluk kontrolüyle paralel yürütülebilir.", "insertAfterTaskTitle": null, "sequenceRank": 1,
           "isAtProjectStart": true,
           "activities": [
             {"title": "Bütçe kalemlerinin görev listesiyle eşleştirilmesi", "effortHours": 6},
             {"title": "Sapma noktalarının belirlenmesi", "effortHours": 4},
             {"title": "Eşleştirme raporunun proje yöneticisine sunulması", "effortHours": 6}
           ]},
          {"title": "Termin riski ve görev bağımlılık planının hazırlanması", "department": "Arge Proje Müdürlüğü", "effortHours": 8, "sourceDocument": "Proje Yönetim Prosedürü",
           "description": "Görevler arası bağımlılıklar çıkarılır, kritik yol analiz edilir ve termin riskini azaltacak önlemler proje yönetim prosedürüne uygun şekilde dokümante edilir.",
           "sequenceNote": "Diğer iki iş paketi tamamlandıktan sonra, genel plan netleştiğinde yapılmalı.", "insertAfterTaskTitle": null, "sequenceRank": 2,
           "isAtProjectStart": false,
           "activities": [
             {"title": "Görevler arası bağımlılıkların çıkarılması", "effortHours": 3},
             {"title": "Kritik yol analizinin yapılması", "effortHours": 3},
             {"title": "Risk azaltma önerilerinin dokümante edilmesi", "effortHours": 2}
           ]}
        ]
        """);
}
