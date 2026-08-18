using System.Text;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Domain;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Fills a <see cref="PromptTemplate"/>'s placeholders with the concrete project/request data. Pure
/// string logic, no I/O — kept separate from <see cref="AiSuggestionAppService"/> so it is trivial to
/// unit test template substitution on its own.
/// </summary>
public static class PromptBuilder
{
    public static string Build(PromptTemplate template, ProjectInfoDto project, string? extraInstructions)
    {
        var text = template.TemplateText
            .Replace("{ProjectName}", project.Name)
            .Replace("{ProjectDescription}", project.Description)
            .Replace("{ProjectType}", project.Type)
            .Replace("{Unit}", project.Unit)
            .Replace("{ExtraInstructions}", string.IsNullOrWhiteSpace(extraInstructions) ? "(yok)" : extraInstructions);

        return text;
    }

    /// <summary>
    /// Appends the project's real department/work-package rows (entered at project creation) as a closed
    /// list the LLM must pick "department" from — without this, the model invents plausible-sounding
    /// department names that never match a real TaskService TaskGroup, so approved suggestions
    /// all fall through to the generic fallback group. Appended unconditionally (same reasoning as
    /// <see cref="AppendDocumentExcerpts"/>) so pre-existing custom <see cref="PromptTemplate"/> rows pick
    /// it up automatically. No-op for Simple-type projects with no department rows at all.
    /// </summary>
    public static string AppendDepartmentList(string prompt, IReadOnlyList<ProjectDepartmentInfoDto> departments)
    {
        if (departments.Count == 0)
        {
            return prompt;
        }

        // Çoklu Birimli projelerde her satırın kendi Title'ı var (TaskGroup.Title ile birebir eşleşir);
        // Basit projelerde birden çok Birim atanmış olsa bile satır Title'ı boş kalabilir — bu durumda
        // TEK güvenilir/temiz alan DepartmentName'dir (TaskGroup.Subtitle fallback eşleşmesi buna bakar).
        // Title boşken satırı hâlâ "(DepartmentName)" biçiminde yazıp "parantez öncesini kopyala"
        // demek talimatı anlamsızlaştırıp modelin ham "(DepartmentName)" metnini (parantezleriyle
        // birlikte) department alanına kopyalamasına yol açardı — bu yüzden Title boşsa satırda hiç
        // parantez yok, direkt DepartmentName gösterilir ve o birebir kopyalanır.
        var section = new StringBuilder(prompt).AppendLine().AppendLine(
            "--- Bu Projede Tanımlı Departman / İş Paketi Satırları ---");
        var labels = new List<string>();
        foreach (var department in departments)
        {
            if (string.IsNullOrWhiteSpace(department.Title))
            {
                section.AppendLine($"- {department.DepartmentName}");
                labels.Add(department.DepartmentName);
            }
            else
            {
                section.AppendLine($"- {department.Title} ({department.DepartmentName})");
                labels.Add(department.Title);
            }
        }
        labels = labels.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        section.AppendLine(
            "Her iş paketinin \"department\" alanına SADECE yukarıdaki listeden birebir bir değer yaz: " +
            "satırda parantez varsa parantez ÖNCESİNDEKİ başlığı (parantez içindeki departman adını " +
            "DEĞİL) kopyala; satırda parantez yoksa satırın TAMAMINI olduğu gibi kopyala. Listede olmayan " +
            "yeni bir departman/başlık ADI UYDURMA — en yakın anlamlı satırı seç.");
        section.AppendLine();
        section.AppendLine(
            $"ZORUNLU KAPSAMA KURALI: Yukarıdaki listede {labels.Count} departman var. Ürettiğin iş " +
            $"paketleri TOPLU olarak bu {labels.Count} departmanın HER BİRİNİ en az bir kez kapsamalı — " +
            "yani \"department\" alanlarına bakıldığında listedeki her etiketten en az bir öneri " +
            "bulunmalı. Yukarıda \"Sorumlu Birim\" olarak belirtilen birim sadece projenin resmi " +
            "yöneticisidir — iş paketlerinin KENDİSİNİ o birime yığma, projedeki TÜM departmanlara " +
            "yaymalısın. Bir departmanın bu kapsamda gerçekten hiçbir iş paketine ihtiyacı yoksa " +
            "atlayabilirsin, ama bu istisnai bir durumdur; varsayılan davranış HER departmana en az " +
            "bir öneri düşürmektir.");
        section.Append("--- Departman Listesi Sonu ---");

        return section.ToString();
    }

    /// <summary>
    /// Appends selected documents' extracted text after the built prompt (phase 1 — direct inclusion,
    /// not RAG). Appended unconditionally rather than via a template placeholder so pre-existing custom
    /// <see cref="PromptTemplate"/> rows (which predate this feature and have no such placeholder) don't
    /// silently drop document content.
    /// </summary>
    public static string AppendDocumentExcerpts(string prompt, IReadOnlyList<DocumentExcerpt> excerpts)
    {
        if (excerpts.Count == 0)
        {
            return prompt;
        }

        var section = new StringBuilder(prompt).AppendLine().AppendLine(
            "--- Seçilen Doküman İçerikleri (test amaçlı doğrudan eklendi; RAG değildir) ---");
        foreach (var excerpt in excerpts)
        {
            section.AppendLine($"[Doküman: {excerpt.FileName}]").AppendLine(excerpt.Text).AppendLine();
        }
        section.AppendLine(
            "ÖNEMLİ: Yukarıdaki \"[Doküman: ...]\" etiketleri (\"(section: ...)\" kısmı dahil) SADECE bu " +
            "metnin hangi dokümandan/bölümden alındığını gösteren bir KAYNAK ATIFIDIR — bunlar bir görev " +
            "başlığı DEĞİLDİR. \"insertAfterTaskTitle\" alanına bu doküman/bölüm etiketlerinden birini ASLA " +
            "yazma; o alan SADECE aşağıda (varsa) verilecek \"Mevcut Görevler\" listesindeki gerçek bir " +
            "görev başlığı olabilir.");
        section.Append("--- Doküman İçerikleri Sonu ---");

        return section.ToString();
    }

    /// <summary>
    /// Appends a RAG-retrieved subset of the project's existing MAIN tasks (bkz. ITaskInfoClient ve
    /// IWorkPackageContextRetrievalService) — iki amaçla: (1) LLM bu listede zaten karşılığı olan bir işi
    /// TEKRAR önermesin, (2) yeni önerisinin bu görevlere göre nereye oturduğunu (sequenceNote) açıklayabilsin.
    /// <paramref name="retrievedExistingTaskContexts"/> RAG'in semantik olarak ilgili bulduğu bir ALT
    /// KÜMEDİR — projenin TÜM görevlerinin tam listesi DEĞİLDİR ve gerçek uygulama sırasını (kronolojik
    /// sırayı) birebir yansıtmayabilir; bu yüzden "insertAfterTaskTitle"/boşluk-bulma önerileri sadece
    /// burada gösterilenlerle sınırlı kalır. Appended unconditionally, aynı <see cref="AppendDocumentExcerpts"/>
    /// gerekçesiyle. RAG hiçbir ilgili görev bulamazsa (ya da retrieval başarısız olursa) no-op.
    /// </summary>
    /// <param name="isCompleteList">
    /// True when <paramref name="retrievedExistingTaskContexts"/> is the project's FULL existing-task list
    /// (AiSuggestionAppService's small-project bypass, see WorkPackageContextRetrievalOptions.FullListThreshold)
    /// rather than a RAG-retrieved semantic subset — changes the framing note so the LLM treats "not in this
    /// list" as a confident signal (a real gap) instead of hedging that the task might just not have been
    /// retrieved.
    /// </param>
    public static string AppendExistingTasksList(
        string prompt, IReadOnlyList<string> retrievedExistingTaskContexts, bool isCompleteList = false)
    {
        if (retrievedExistingTaskContexts.Count == 0)
        {
            return prompt;
        }

        var scopeNote = isCompleteList
            ? "projenin TÜM mevcut ana görevlerinin TAM listesidir — hiçbir görev eksik bırakılmadı, " +
              "burada olmayan bir görev gerçekten mevcut değildir"
            : "RAG ile getirilen, TÜM görevlerin anlamsal olarak ilgili bir alt kümesi olabilir — projenin " +
              "TÜM görevlerinin tam listesi DEĞİLDİR ve sıralaması gerçek uygulama sırasını birebir " +
              "yansıtmayabilir";
        var section = new StringBuilder(prompt).AppendLine().AppendLine(
            $"--- Bu Öneriyle İlgili Olabilecek Mevcut Görevler ({scopeNote}) ---");
        foreach (var context in retrievedExistingTaskContexts)
        {
            section.AppendLine(context).AppendLine();
        }
        section.AppendLine(
            "Yukarıdaki görevler ZATEN mevcut/planlanmış — bunların kapsadığı işi TEKRAR önerme. " +
            "Yalnızca bu listede karşılığı olmayan, projenin hâlâ ihtiyaç duyduğu YENİ iş paketlerini " +
            "öner. Sadece listenin EN SONUNA eklenecek işleri değil, listedeki ART ARDA gelen iki görev " +
            "arasında ATLANMIŞ/EKSİK bir adım olup olmadığını da değerlendir — ör. \"Tasarım\" ile " +
            "\"İnşaat\" arasında \"Ruhsat/İzin Süreçleri\" gibi bir görev listede yoksa ve gerekiyorsa, " +
            "bunu da öner ve \"insertAfterTaskTitle\" alanına ARADAN GEÇTİĞİ (önceki) görevin başlığını yaz " +
            "ki öneri gerçek sırada iki görev arasındaki doğru konuma otursun; sadece en son göreve bağlı " +
            "önerilerle sınırlı kalma. Her önerinin \"sequenceNote\" alanına, bu listeye bakarak önerinin " +
            "gerçek sırada nereye oturduğunu kısaca açıkla (ör. \"İskele kurulumu bittikten sonra, İnşaat " +
            "başlamadan önce yapılır\" ya da \"Mevcut görev yok, projenin başında yapılabilir\"). Ayrıca " +
            "\"insertAfterTaskTitle\" alanına, önerinin hangi mevcut görevden SONRA geldiğini yukarıdaki " +
            "listeden TAM ve TIRNAK İÇİNDEKİYLE BİREBİR AYNI başlığı kopyalayarak yaz (uydurma/kısaltma " +
            "yapma) — bu alan, öneri onaylandığında görevin gerçek sıraya doğru yerleşmesi için kullanılır. " +
            "ÖNEMLİ: \"insertAfterTaskTitle\" SADECE yukarıdaki listedeki bir başlık olabilir. Şu anda " +
            "ÜRETTİĞİN diğer iş paketlerinden (bu yanıttaki kardeş öneriler) birinin başlığını ASLA " +
            "buraya yazma — onlar henüz gerçek görev değil, onay bekleyen öneriler; kullanıcı reddederse " +
            "ya da farklı sırada onaylarsa referans geçersiz kalır. Bu üretimdeki başka bir öneriye bağlı " +
            "olsa bile, \"insertAfterTaskTitle\" alanına yalnızca gerçek bir mevcut görev yazabilirsin " +
            "ya da hiçbiri uygun değilse null bırakırsın. Öneri hiçbir mevcut göreve bağlı değilse " +
            "(projenin en başında yapılabilir gibi), bu alanı null bırak — ama bu durumda \"isAtProjectStart\" " +
            "alanına DOĞRUDAN true yaz. \"isAtProjectStart\" ile \"insertAfterTaskTitle\": null'ın İKİ FARKLI " +
            "ANLAMINI ayırt eder: \"insertAfterTaskTitle\" null VE \"isAtProjectStart\" true ise bu, önerinin " +
            "GERÇEKTEN projenin en başında (1 numaralı görevden bile önce) yapılması gerektiği anlamına gelir; " +
            "\"insertAfterTaskTitle\" null VE \"isAtProjectStart\" false ise bu, önerinin nereye oturduğunun " +
            "belirsiz kaldığı anlamına gelir. Bu ikisini KARIŞTIRMA — önerin gerçekten projenin en başında " +
            "yapılabilecek bir işse \"isAtProjectStart\": true yazmayı UNUTMA, aksi halde öneri kullanıcıya " +
            "listenin EN SONUNDAYMIŞ gibi gösterilir. Bu üretimdeki DİĞER önerilerle kendi aralarındaki " +
            "göreli sırayı ise \"sequenceRank\" alanına yaz — 1'den başlayan, bu yanıtta ürettiğin TÜM " +
            "önerilerin gerçek uygulama sırasına göre kendi aralarındaki tam sırası (1 en önce yapılır, en " +
            "büyük sayı en son). \"sequenceRank\" bir gerçek göreve değil, SADECE bu yanıttaki diğer " +
            "önerilere göre sırayı ifade eder — kullanıcı aralarından birini reddetse bile kalanların " +
            "birbirine göre sırası bu sayılarla korunur, bu yüzden kardeş bir önerinin başlığına değil bu " +
            "sayıya güvenilir. Her öneri için mutlaka bir sequenceRank ver, null bırakma.");
        section.Append("--- Mevcut Görevler Listesi Sonu ---");

        return section.ToString();
    }

    /// <summary>
    /// RAG'in semantik olarak ilgili bulduğu, hâlâ karar bekleyen (Pending) öneri başlıklarının bir ALT
    /// KÜMESİNİ listeler (bkz. IWorkPackageContextRetrievalService). <see cref="AppendExistingTasksList"/>'in
    /// aksine bu veri TaskService'teki gerçek görevlerden değil, AIGatewayService'in kendi bekleyen
    /// kayıtlarından gelir — henüz onaylanmamış bir öneri gerçek görev olmadığı için mevcut görev
    /// listesinde görünmez, model aynı fikri fark etmeden tekrar üretebilir. Reddedilmiş ya da
    /// onaylandıktan sonra arşivlenmiş öneriler BİLEREK bu listeye dahil edilmez: kullanıcı bir öneriyi
    /// reddetmek ya da arşivlemek suretiyle "bu fikri istemiyorum" dediğinde, bu kararı sonsuza dek
    /// hatırlayıp aynı fikrin bir daha hiç önerilememesine izin vermek (ör. kullanıcı eski önerileri
    /// temizleyip projeyi yeniden değerlendirmek istediğinde) modelin kalıcı olarak yeni öneri
    /// üretememesine yol açar — bkz. AiSuggestionAppService.GenerateAsync'teki pendingSuggestionTitles
    /// yorumu. <paramref name="retrievedPendingSuggestionContexts"/> TÜM bekleyen önerilerin tam listesi
    /// DEĞİLDİR — sadece üretilecek işle konu/kapsam olarak örtüşebilecek olanlardır; deterministik
    /// exact-match tekrar filtresi (titlesToSkip) bu alt kümeden BAĞIMSIZ olarak TAM listeyle çalışmaya
    /// devam eder. Appended unconditionally; boş listede no-op.
    /// </summary>
    public static string AppendPendingSuggestionTitles(string prompt, IReadOnlyList<string> retrievedPendingSuggestionContexts)
    {
        if (retrievedPendingSuggestionContexts.Count == 0)
        {
            return prompt;
        }

        var section = new StringBuilder(prompt).AppendLine().AppendLine(
            "--- Bu Öneriyle İlgili Olabilecek, Hâlâ Karar Bekleyen Öneri Başlıkları (RAG ile getirilen " +
            "bir alt küme; TÜM bekleyen önerilerin tam listesi DEĞİLDİR) ---");
        foreach (var context in retrievedPendingSuggestionContexts)
        {
            section.AppendLine(context);
        }
        section.AppendLine(
            "Yukarıdaki başlıklar bu proje için ÜRETİLDİ ve kullanıcının onay/red kararı hâlâ bekleniyor — " +
            "AYNI başlığı ya da neredeyse aynı kapsamı BİREBİR TEKRAR üretme, kullanıcı zaten bu öneriler " +
            "üzerinde karar verecek.");
        section.Append("--- Bekleyen Öneriler Sonu ---");

        return section.ToString();
    }

    public static PromptTemplate DefaultTemplateFor(string projectType) => PromptTemplate.Create(
        projectType,
        """
        Kurumsal proje yönetimi asistanısın. Aşağıdaki proje için iş paketi (work package) önerileri üret.

        Proje Adı: {ProjectName}
        Proje Türü: {ProjectType}
        Sorumlu Birim: {Unit}
        Proje Açıklaması: {ProjectDescription}
        Ek Talimatlar: {ExtraInstructions}

        Kapsamı gerektiği kadar iş paketine böl — tek bir büyük pakette toplama. İş paketi SAYISINI
        projenin GERÇEK büyüklüğüne göre belirle, sabit bir sayıya (ör. her zaman 3-4) ANKRAJLANMA:
        - Aşağıda bir "Bu Projede Tanımlı Departman / İş Paketi Satırları" listesi verilmişse, HER
          departmandan/birimden en az bir iş paketi öner — kapsam gerçekten bir departmanı hiç
          ilgilendirmiyorsa bu departmanı atlayabilirsin, ama varsayılan davranış her departmana en az
          bir öneri düşürmektir; tüm önerileri tek bir departmana yığma.
        - Aşağıda doküman içeriği/RAG bağlamı verilmişse, dokümanın kapsadığı HER ana konu/bölüm alanı
          için (ör. teknik mimari, veri/entegrasyon, kullanıcı arayüzü, test/kalite, eğitim/yaygınlaştırma,
          operasyon/bakım gibi dokümanda geçen farklı alanlar) ayrı bir iş paketi değerlendir — dokümanın
          zengin ve çok bölümlü olması, projenin de o kadar çok iş paketine ayrılması gerektiğinin
          işaretidir.
        - Küçük/dar kapsamlı bir proje için 3-5, orta ölçekli bir proje için 6-10, çok departmanlı veya
          çok bölümlü bir dokümana sahip büyük bir proje için 10'un üzerinde (gerektiğinde 15-20) iş
          paketi tamamen normaldir — üst sınır YOKTUR, kapsamı gerçekten karşılayan kadar öneri üret.

        Her iş paketi, altında somut faaliyetlerden/alt görevlerden oluşur — "activities" dizisine o iş
        paketinin gerçekleştirilmesi için yapılması gereken 2-6 arası somut, uygulanabilir faaliyeti yaz.

        "description" alanına iş paketinin ne olduğunu, kapsamını ve nasıl tamamlanacağını 2-4 cümleyle
        anlatan bir açıklama yaz — sadece başlığın tekrarı olmasın, gerçek bir iş tanımı olsun.

        "sequenceNote" alanına, aşağıda (varsa) verilen mevcut görev listesine bakarak bu iş paketinin
        projenin gerçek uygulama sırasında nereye oturduğunu kısaca açıkla. "insertAfterTaskTitle"
        alanına ise aynı gerekçeyi MAKİNE tarafından okunabilir hâlde ver — SADECE aşağıdaki mevcut görev
        listesinden TAM eşleşen bir başlık olabilir; bu yanıtta ürettiğin BAŞKA bir iş paketinin başlığını
        ASLA yazma (onlar henüz gerçek görev değil). Mevcut görev listesi yoksa ya da hiçbir göreve bağlı
        değilse null bırak. Sadece son göreve eklenecek işleri değil, mevcut görev listesindeki ART ARDA
        gelen iki görev arasında atlanmış bir adım olup olmadığını da düşün.

        "insertAfterTaskTitle" null olduğunda bunun NEDENİNİ "isAtProjectStart" alanıyla açıkça belirt:
        öneri GERÇEKTEN projenin en başında (ilk görevden bile önce) yapılabiliyorsa true yaz; sadece
        nereye oturduğu belirsiz kaldıysa false yaz. Bu ikisi aynı şey değildir — true yazmayı unutursan
        öneri kullanıcıya listenin en sonundaymış gibi gösterilir.

        "sequenceRank" alanına, bu yanıtta ürettiğin TÜM iş paketlerinin (bu üretimdeki kardeş öneriler)
        kendi aralarındaki gerçek uygulama sırasını 1'den başlayan bir tam sayı olarak ver — her öneriye
        mutlaka bir rank ver, null bırakma. Bu, gerçek bir göreve değil SADECE bu yanıttaki diğer
        önerilere göre göreli sırayı ifade eder.

        effortHours alanına, ilgili iş paketinin veya faaliyetin kapsamına ve karmaşıklığına göre
        gerçekçi bir kişi-saat tahmini yaz (örnek: basit bir faaliyet için 4-16 saat, kapsamlı bir iş
        paketi için 40-160 saat arası olabilir). effortHours değeri asla 0 olamaz — her zaman pozitif,
        gerçekçi bir tam sayı tahmini ver.

        Yanıtı SADECE şu şemaya uyan bir JSON dizisi olarak ver, başka hiçbir metin ekleme. Aşağıdaki
        örnek yalnızca ŞEKLİ göstermek içindir — yanıtında kapsamın gerektirdiği kadar öğe olmalı, iki
        tane değil:
        [{"title": "...", "department": "...", "effortHours": 24, "sourceDocument": null,
          "description": "...", "sequenceNote": "...", "insertAfterTaskTitle": null, "sequenceRank": 1,
          "isAtProjectStart": true, "activities": [{"title": "...", "effortHours": 8}]},
         {"title": "...", "department": "...", "effortHours": 16, "sourceDocument": null,
          "description": "...", "sequenceNote": "...", "insertAfterTaskTitle": "...", "sequenceRank": 2,
          "isAtProjectStart": false, "activities": [{"title": "...", "effortHours": 6}]}]
        """);
}
