# Görevler Ekranı & Yeni Görev Oluştur — Tasarım Brief'i

**Amaç:** Bu doküman bir tasarım aracına/AI'a (Figma AI, v0, Lovable, Claude vb.) verilmek üzere hazırlanmıştır. Hedef, mevcut "Görevler" ve "Yeni Görev Oluştur" ekranlarını yeniden tasarlamak; bunları hem birbiriyle hem de mevcut **"AI İş Paketi Oluştur v1"** ekranıyla görsel ve yapısal olarak tam uyumlu hale getirmektir.

Ürün: Özdilek PM — kurumsal proje/görev yönetimi uygulaması (Angular, masaüstü ağırlıklı, yoğun bilgi içeren tablo/kart arayüzleri).

---

## 1. Referans Ekran: "AI İş Paketi Oluştur v1" (değiştirilmeyecek, uyum kaynağı)

Bu ekran zaten üretimde ve **tasarım dili referansı** olarak kullanılacak. Yeni ekranlar bunun kurduğu kalıpları tekrar etmeli:

1. **Hero header** — solda ikon rozeti (yuvarlak, soft arka plan), üstte küçük "eyebrow" etiket, altında H2 başlık ve açıklama paragrafı, sağda/altta ince bir "not" çubuğu (yardım ikonu + kısa bilgi cümlesi).
2. **Adım göstergesi (stepper)** — yatay 3 adımlı `<ol>`, her adımda numara rozeti + kalın başlık + küçük alt açıklama; aktif adım vurgulanır (`is-active`).
3. **Numaralı bölümler ("01", "02"...)** — her ana bölümün başında büyük, soluk renkli iki haneli numara (`01`, `02`) + başlık + açıklama satırı. Bölümler dikey olarak art arda gelir, aralarında net boşluk var.
4. **Form bölümü** — bağlam özeti (proje adı/tipi/birimi mini etiketler halinde), ardından serbest metin alanı (karakter sayacı ile) ve seçilebilir doküman/kart listesi (checkbox + ikon + ad + boyut, seçili durumda vurgulu kenarlık).
5. **Sonuç kartları listesi** — her öneri bir `article` kart: sol üstte 2 haneli sıra numarası, üstte küçük "kicker" etiket (tür + departman), kalın başlık, sağda süre/efor rozeti (ikon + sayı + birim). Gövdede etiketli alt bölümler ("Görev açıklaması", "Nasıl yapılacak?" — numaralı adım listesi). Altbilgide kaynak bilgisi + iki aksiyon butonu (Reddet / Onayla, ikonlu, sağ hizalı).
6. **Boş durum (empty state)** — ortalanmış ikon rozeti + kalın mesaj + açıklama + küçük özellik rozetleri satırı.
7. **Geçmiş kayıtlar** — açılır/kapanır (collapsible) bir liste; her kayıt tarih + kaynak + durum etiketiyle.

**Bileşen/stil sözlüğü (bu ekrandan miras alınacak sınıf adı kalıpları):** `*-hero`, `*-hero-icon`, `*-eyebrow`, `*-steps`, `*-section-heading`, `*-section-number`, `*-suggestion-card`, `*-suggestion-index`, `*-suggestion-kicker`, `*-detail-label`, `*-empty-state`, `*-history-toggle`. Yeni ekranlarda aynı isimlendirme mantığı (`pd-tasks-*`, `pd-task-create-*` gibi) izlenmeli.

---

## 2. Tasarım Sistemi Token'ları (sabit, değiştirilmeyecek)

```
Renkler:
  --pm-ink:    #303246   (birincil metin)
  --pm-muted:  #73778d   (ikincil metin)
  --pm-line:   #e9ebf2   (ince ayraç)
  --pm-panel:  #ffffff   (kart/panel zemini)
  --pm-soft:   #f7f8fb   (alt zemin / input arka planı ~ #f8f9fb)
  --pm-frame:  #e9ebf5   (dış çerçeve/tuval)
  --pm-navy:   #2f3145   (birincil aksiyon / vurgulu buton, koyu)
  --pm-blue:   #4b7dd8   (bağlantı, aktif durum, odak halkası)
  --pm-green:  #2eb86a   (başarı / onay / iyi sapma)
  --pm-orange: #ff8a35   (uyarı / orta öncelik)
  --pm-red:    #ff405a   (hata / kötü sapma / reddet)

Tipografi: Inter, 400/500/600/700. Taban gövde metni 13px. Akıcı (fluid) ölçek
  clamp() ile: --pm-fluid-font-xs .. -lg, --pm-fluid-space-xs .. -lg
  (küçük ekranda küçülür, büyük ekranda büyür — sabit px yerine bu ölçeği kullanın).

Köşe yuvarlama: kartlar ~ clamp(0.45rem, 1vmin, 0.8rem) (≈7–10px);
  pill/etiket/buton: 999rem (tam yuvarlak/hap biçimi); popover/dialog: 10px.

Gölge: kartlarda çok hafif — 0 3px 12px rgba(48,50,70,0.025) / 0 2px 8px rgba(42,46,68,0.045).

Kart çerçevesi: 1px solid #eef0f4 / #f0f1f5 (çok açık gri, neredeyse görünmez).

Buton dili:
  Birincil (pd-dialog-primary): dolgu #2f3145, beyaz metin, ikon+etiket.
  İkincil/Vazgeç: beyaz zemin, ince kenarlık, koyu metin.
  Onayla: yeşil vurgulu kontur/dolgu + check ikonu.
  Reddet: kırmızı vurgulu kontur + x ikonu.

Dialog kalıbı: yarı saydam/blur overlay (pd-dialog-layer) üstünde beyaz kart
  (header: başlık + kapat X / body: pd-dialog-field'lar / footer: Vazgeç + Kaydet).
  Input zemin: #f8f9fb, odakta hafif mavi halka rgba(75,125,216,0.11).
```

Yeni tasarımlar **bu paletin ve ölçeğin dışına çıkmamalı**; yeni renk eklenmeyecek, sadece mevcut semantik renkler (navy/blue/green/orange/red) farklı bileşenlerde tekrar kullanılacak.

---

## 3. Ekran A: "Görevler" (yeniden tasarım)

### 3.1 Amaç ve mevcut durumdan fark

Bugün "Görevler" sekmesi düz bir liste: görev grupları (`pd-task-group`) altında satır satır görevler (`pd-task-row`), her satırda checkbox + başlık + efor/bağımlılık + atanan kişi + yorum sayısı. Ana görev/alt görev ayrımı sadece girinti (`--task-depth`) ile gösteriliyor; AI'dan gelen görevler listeye karışmış durumda ve ayrı bir "AI iş paketi" gruplaması yok. **Uygulama sırası** (proje hangi sırayla yürüyecek) görsel olarak hiçbir yerde net değil.

Yeni tasarım şunu çözmeli: **tek bakışta** projenin hangi sırayla ilerleyeceğini (ana görev → alt görev → hangi AI iş paketinin ne zaman devreye gireceği) gösteren, iki net bölüme ayrılmış ama görsel dili tek bir sistemde birleşen bir ekran.

### 3.2 Üst yapı

- Sayfa üstünde hero header (referans ekrandaki `pd-ai-hero` kalıbıyla aynı): ikon rozeti + eyebrow ("Proje uygulama planı") + H2 ("Görevler ve uygulama sırası") + kısa açıklama + sağda özet rozetleri (toplam ana görev / toplam alt görev / bekleyen AI iş paketi sayısı — referans ekrandaki `pd-ai-pending-summary` kalıbı).
- Hemen altında bir **segment/filtre çubuğu**: "Tümü / Ana Görevler / AI İş Paketleri / Tamamlananlar" gibi pill-tab'lar (mevcut `pm-view-tabs` stiliyle aynı: beyaz zemin, aktifte navy dolgu, hap biçim).

### 3.3 Bölüm 1 — "Ana Görevler & Alt Görevler"

- Bölüm başlığı, referans ekrandaki numaralı bölüm kalıbıyla: `01` + "Ana Görevler & Alt Görevler" + açıklama satırı.
- Her **ana görev** bir kart (mevcut `pd-task-group` header'ının kart haline getirilmiş hali): sırasında **uygulama sıra numarası** (büyük, soluk, 2 haneli — `01`, `02`... referans ekrandaki `pd-ai-suggestion-index` ile birebir aynı görsel dil), başlık, durum rozeti (Todo/Devam Ediyor/Tamamlandı — renk: gri/mavi/yeşil), tarih aralığı, sorumlu departman.
- Kart açıldığında/altında **alt görevler** girintili satırlar halinde listelenir (mevcut `pd-task-row` düzeni korunur: checkbox + başlık + efor/bağımlılık + atanan kişi + AI rozeti + yorum sayısı + "+" menü). Alt görevlerin de kendi içinde küçük bir sıra numarası (1, 2, 3…) olmalı — ana göreve göre nispi sıra.
- Bağımlılık göstergesi güçlendirilmeli: bir görev başka bir göreve bağımlıysa, ince bir bağlantı çizgisi veya "→ Bağımlı: {görev adı}" etiketi ana görev kartları arasında da görünür olmalı (proje akışını "sırayla okunur" kılmak için).

### 3.4 Bölüm 2 — "AI İş Paketleri"

- Bölüm başlığı aynı kalıpla: `02` + "AI İş Paketleri" + açıklama ("AI tarafından oluşturulup onaylanan iş paketleri, uygulama sırasına göre listelenir").
- Kartlar **birebir referans ekrandaki `pd-ai-suggestion-card` görselini** kullanır (index rozeti, kicker/departman, başlık, efor/süre rozeti, açıklama, "Nasıl yapılacak?" adım listesi) — ama artık "Reddet/Onayla" yerine, onaylanmış paketler için durum rozeti (Onaylandı/Devam Ediyor/Tamamlandı) ve "Görevlere git" bağlantısı gösterir. Henüz onaylanmamış/bekleyen paketler varsa (AI tab'ından gelen), burada da "Bekliyor" rozetiyle görünür ama onay işlemi yine AI İş Paketi Oluştur ekranında yapılır (tekrar etmeyin — sadece durum yansısın).
- Her AI iş paketi kartında da aynı **uygulama sıra numarası** kullanılmalı; bu numara Bölüm 1'deki ana görev sırasıyla **aynı numaralandırma diziliminden** gelmelidir (bkz. 3.5).

### 3.5 Birleşik uygulama sırası (en kritik gereksinim)

Ana görevler ve AI iş paketleri **ayrı iki bölümde gösterilse de**, ikisi de projenin uygulanacağı **tek bir kronolojik sıraya** göre numaralandırılmalı. Yani: proje 7 adımda yürüyorsa ve 3. adım bir AI iş paketiyse, o iş paketi kartında "03" yazmalı; Bölüm 1'deki ana görevler de kendi gerçek sıra numaralarını taşımalı (03 orada görünmeyebilir çünkü o adım Bölüm 2'de, ama 01-02-04-05... boşluksuz akmalı ya da her bölüm kendi alt-sırasını gösterip üstte "genel plan" adında birleşik bir zaman çizelgesi (mini dikey stepper/timeline) bu ikisini tek satırda birleştirmeli).

**Önerilen çözüm:** Sayfanın en üstünde, iki bölümden önce, yatay/dikey bir **"Uygulama Planı" mini zaman çizelgesi** ekleyin — ana görev ve AI iş paketi düğümlerini tek bir sırada (renk kodlu: ana görev = navy nokta, AI iş paketi = mor/mavi + sparkle ikonu nokta) gösteren, tıklanabilir bir şerit. Buna tıklayınca ilgili karta scroll eder. Bu, iki ayrı bölümün "aynı sırayı paylaştığını" görsel olarak kanıtlar.

---

## 4. Ekran B: "Yeni Görev Oluştur" (yeniden tasarım)

### 4.1 Mevcut durum

Bugün bu bir modal dialog (`pd-task-dialog`): başlık, atanan kişi, üst görev seçimi + "Ana Görev" checkbox'ı, başlangıç/bitiş tarih-saat, kategori, departman (readonly), açıklama. Standart `pd-dialog` kalıbı kullanılıyor (sade form, tasarım dili AI ekranıyla uyumsuz — numaralandırma, hero, adım göstergesi yok).

### 4.2 Yeni tasarım yönü

Modal olarak kalabilir (tam sayfa geçişi gerekmiyor, mevcut UX akışını bozmayın) ama **içeriği referans AI ekranının dilini konuşmalı**:

1. **Mini hero şerit** (dialog header'ının hemen altı): küçük ikon + "Yeni görev, uygulama sırasındaki yerini alacak" gibi tek satır yönlendirme metni.
2. Form, **numaralı iki mini bölüme** ayrılır (aynı `01`/`02` kalıbı, sadece küçük ölçekte):
   - **01 — Görev kimliği:** başlık, açıklama, kategori.
   - **02 — Yerleşim ve sıra:** "Ana Görev mi, Alt Görev mi?" seçimi (segment/pill toggle, checkbox yerine — daha net); Alt Görev seçilirse üst görev dropdown'u açılır; **yeni eklenecek alan: "Uygulama Sırası"** — bu görevin genel plan içinde hangi ana görev/AI iş paketinden sonra geleceğini seçtiren bir dropdown veya sürükle-bırak sıra göstergesi (Bölüm 3.5'teki birleşik zaman çizelgesiyle aynı görsel dilde, küçük bir önizleme şeridi).
3. **Atanan kişi + tarih/saat alanları** mevcut düzende kalır (`pd-dialog-row` iki kolonlu grid), sadece input stilleri referans ekrandaki `pd-ai-instruction`/`pd-ai-field-label` (etiket + küçük "isteğe bağlı/zorunlu" ibaresi) kalıbına güncellenir.
4. Footer'da birincil buton, AI ekranındaki gibi ikonlu olmalı: "＋ Görevi Oluştur" (mevcut sade "Kaydet" yerine, ikon + iki satırlı buton metni — bkz. `pd-new-task` butonundaki `<span>Yeni Görev</span><strong>Oluştur</strong>` kalıbı, bu zaten var ve korunmalı, sadece dialog içindeki birincil buton da bu enerjiyi yansıtmalı).

### 4.3 Görevler ekranıyla uyum noktaları

- Aynı sıra numarası/rozet biçimi (2 haneli, soluk renk) formda önizleme olarak görünür — kullanıcı görevi oluşturmadan önce "bu görev 04 numaralı adım olacak" bilgisini görür.
- Ana Görev/Alt Görev toggle'ının renk kodu, Görevler ekranındaki ana görev kartı vurgusuyla birebir aynı (navy vurgu = ana görev, açık mavi/gri = alt görev).
- Kategori ve departman etiketleri, Görevler ekranındaki kart üstü etiketlerle aynı pill biçimini kullanır.

---

## 5. Tutarlılık kontrol listesi (tasarım aracına doğrudan verilecek kural seti)

- [ ] Üç ekranda da (AI İş Paketi Oluştur v1, Görevler, Yeni Görev Oluştur) aynı hero header anatomisi kullanılıyor mu? (ikon rozeti solda, eyebrow+başlık+açıklama ortada)
- [ ] Sıra numaraları her yerde aynı görsel biçimde mi (2 haneli, soluk gri, büyük punto, kartın sol üst köşesi)?
- [ ] Renk kullanımı semantik mi? (navy=birincil/ana görev, mavi=bilgi/aktif, yeşil=onay/tamamlandı, turuncu=uyarı/devam ediyor, kırmızı=reddet/gecikme)
- [ ] Kart kenarlık/gölge/köşe yuvarlama değerleri token'lardan mı geliyor (bkz. Bölüm 2), yeni bir değer icat edilmedi mi?
- [ ] Buton hiyerarşisi tutarlı mı (birincil=navy dolgu, ikincil=beyaz+kenarlık, onay=yeşil, reddet/iptal=kırmızı/nötr)?
- [ ] AI rozeti (mor/mavi "AI" küçük etiketi) her üç ekranda da aynı görselde mi kullanılıyor?
- [ ] Boş durumlar (empty state) her bölümde referans ekrandaki `pd-ai-empty-state` kalıbıyla mı çiziliyor?
- [ ] Tipografi ölçeği `--pm-fluid-font-*` / `--pm-fluid-space-*` değişkenleriyle mi tanımlı (sabit px değil)?

---

## 6. Teslimat beklentisi

Tasarım aracından beklenen çıktı:
1. "Görevler" ekranı için tam sayfa mockup/kod (hero + birleşik zaman çizelgesi + Bölüm 1 (Ana/Alt Görevler) + Bölüm 2 (AI İş Paketleri)).
2. "Yeni Görev Oluştur" dialog'unun yeniden tasarlanmış hâli (mini hero + 2 numaralı bölüm + güncellenmiş footer).
3. Her iki ekranın da masaüstü genişliğinde (≥1280px) ve orta genişlikte (~1024px) görünümleri.
4. Yukarıdaki tutarlılık kontrol listesinin karşılandığına dair kısa bir not.

Referans olarak incelenmesi gereken mevcut dosyalar (kod tabanında):
- `src/app/features/projects/project-detail-page/project-detail-page.html` (satır 437-510: mevcut Görevler; satır 512-787: AI İş Paketi Oluştur v1; satır 897-975: mevcut Yeni Görev Oluştur dialog'u)
- `src/app/features/projects/project-detail-page/project-detail-page.scss` (`.pd-ai*`, `.pd-task*`, `.pd-dialog*` sınıfları)
- `src/styles.scss` (renk/tipografi/spacing token'ları, `:root` bloğu ve `--pm-fluid-*` akıcı ölçek sistemi)
- `src/app/features/projects/data/task-api.models.ts` (`TaskItemDto`, `TaskGroupDto` — `depth`, `isMainTask`, `dependsOnTaskId`, `status`, `isAiGenerated` alanları)
- `src/app/features/projects/data/ai-suggestion-api.models.ts` (`WorkPackageSuggestionItemDto`, `AiSuggestionActivityDto` — `decision`, `activities`)
