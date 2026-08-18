using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.AIGatewayService.Domain;

public class AiSuggestionItem : BaseEntity
{
    private readonly List<AiSuggestionActivity> _activities = [];

    public Guid RequestId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public int EffortHours { get; private set; }
    public string? SourceDocument { get; private set; }
    public SuggestionItemDecision Decision { get; private set; } = SuggestionItemDecision.Pending;
    // Görevin ne olduğunu tek satırlık başlıktan daha ayrıntılı anlatan gövde metni — LLM'den istenir,
    // boş kalabilir (ör. eski/Mock üretimler için).
    public string? Description { get; private set; }
    // "Bu iş paketi neden/nereye oturuyor" gerekçesi — projenin gerçek mevcut görev sırasına bakılarak
    // LLM tarafından üretilir (ör. "İskele kurulumu bittikten sonra başlar"). Sıralamanın kendisi değil,
    // insan onayı için okunabilir bir açıklamadır.
    public string? SequenceNote { get; private set; }
    // SequenceNote'un insan tarafından okunabilir hâlinin aksine, bu alan MAKİNE tarafından kullanılır:
    // LLM'e verilen mevcut görev listesindeki (bkz. PromptBuilder.AppendExistingTasksList) TAM bir görev
    // başlığı — onay anında WorkPackageApprovedConsumer bunu gerçek bir göreve eşleştirip yeni ana
    // görevin başlangıç tarihini buna göre hesaplar, böylece onaylanan iş paketi listenin sonuna değil
    // modelin işaret ettiği gerçek sıraya oturur. Eşleşme yoksa/null ise görev tarihsiz kalır.
    public string? InsertAfterTaskTitle { get; private set; }
    // Aynı üretimdeki (request) diğer önerilerle KENDİ ARALARINDAKİ göreli sırayı ifade eder — 1'den
    // başlayan, LLM'in kendi öngördüğü gerçek uygulama sırası. InsertAfterTaskTitle'ın aksine gerçek bir
    // göreve bağlı değildir, bu yüzden bir kardeş öneri reddedilse bile kırılmaz: kalanlar kendi rank'ıyla
    // sıralanmaya devam eder (bkz. project-detail-page.ts unifiedSequenceRows). Aynı gerekçeyle
    // InsertAfterTaskTitle'ın YERİNE değil, ONUNLA BİRLİKTE kullanılır — biri "hangi gerçek görevden
    // sonra en erken başlayabilir" sorusuna, diğeri "bu grup içindeki diğer önerilere göre nerede durur"
    // sorusuna cevap verir.
    public int? SequenceRank { get; private set; }
    // InsertAfterTaskTitle == null'ın iki farklı anlamını ayırt eder: (a) bu iş GERÇEKTEN projenin en
    // başında yapılabilir, hiçbir mevcut göreve bağlı değil (bu alan true) — ya da (b) model bir yere
    // oturtamadı/belirsiz kaldı (bu alan false, varsayılan). Bu ayrım olmadan Görevler ekranındaki
    // unifiedSequenceRows ikisini de aynı "çözülemedi" sepetine atıp listenin SONUNA koyuyordu — hâlbuki
    // (a) durumunda önerinin listenin EN BAŞINA (1 numaradan önce) girmesi gerekir.
    public bool IsAtProjectStart { get; private set; }

    // The work package's own faaliyet/alt görev breakdown — see AiSuggestionActivity. Populated right
    // after construction (mirrors how AiSuggestionRequest.AddItem builds an item then the caller adds
    // its activities), not via the constructor, so a package with zero activities is still valid.
    public IReadOnlyCollection<AiSuggestionActivity> Activities => _activities.AsReadOnly();

    private AiSuggestionItem() { }

    public AiSuggestionItem(
        Guid requestId, string title, string department, int effortHours, string? sourceDocument,
        string? description = null, string? sequenceNote = null, string? insertAfterTaskTitle = null,
        int? sequenceRank = null, bool isAtProjectStart = false)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("İş paketi başlığı zorunludur.");
        }
        if (string.IsNullOrWhiteSpace(department))
        {
            throw new DomainException("İş paketi departmanı zorunludur.");
        }

        RequestId = requestId;
        Title = title;
        Department = department;
        EffortHours = effortHours;
        SourceDocument = sourceDocument;
        Description = description;
        SequenceNote = sequenceNote;
        InsertAfterTaskTitle = insertAfterTaskTitle;
        SequenceRank = sequenceRank;
        IsAtProjectStart = isAtProjectStart;
    }

    public void AddActivity(string title, int? effortHours)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Faaliyet başlığı zorunludur.");
        }

        _activities.Add(new AiSuggestionActivity(Id, title, effortHours));
        MarkUpdated();
    }

    public void Approve()
    {
        if (Decision != SuggestionItemDecision.Pending)
        {
            throw new DomainException("Bu öneri zaten karara bağlanmış.");
        }

        Decision = SuggestionItemDecision.Approved;
        MarkUpdated();
    }

    public void Reject()
    {
        if (Decision != SuggestionItemDecision.Pending)
        {
            throw new DomainException("Bu öneri zaten karara bağlanmış.");
        }

        Decision = SuggestionItemDecision.Rejected;
        MarkUpdated();
    }
}
