import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  computed,
  inject,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FileTypeIcon } from '../../../../shared/file-type-icon/file-type-icon';
import { Icon } from '../../../../shared/icon/icon';
import { ToastService } from '../../../../shared/toast/toast.service';
import { AiSuggestionApiService } from '../../data/ai-suggestion-api.service';
import { AiSuggestionRequestDto, WorkPackageSuggestionItemDto } from '../../data/ai-suggestion-api.models';
import type { DocumentKind, ProjectDocumentView } from '../project-detail-page';

export interface AiWorkPackageProject {
  id: string;
  name: string;
  type: string;
  unit: string;
}

interface AiSuggestionActivityView {
  id: string;
  title: string;
  effortHours: number | null;
}

interface AiSuggestionView {
  requestId: string;
  itemId: string;
  title: string;
  department: string;
  effortHours: number;
  source: string | null;
  description: string | null;
  sequenceNote: string | null;
  activities: AiSuggestionActivityView[];
  usedRealDocumentContext: boolean;
}

// Backend'in gerçek RAG kabul listesiyle aynı — bkz. RagDocumentSyncService.RagSupportedExtensions
// (.pdf/.docx/.txt/.xlsx/.xls/.pptx/.ppt/.png/.jpg/.jpeg/.webp/.bmp/.tiff/.csv). 'video' hariç hemen her
// tür burada eligible: .txt/.csv/.bmp/.tiff DocumentKind tarafında 'file' kovasına düşüyor, o yüzden
// 'file' de listede. Bu iki liste birbirinden bağımsız değişirse (ör. backend yeni bir tür eklerse) burası
// da güncellenmeli.
const AI_ELIGIBLE_KINDS: ReadonlySet<DocumentKind> = new Set<DocumentKind>([
  'pdf', 'word', 'excel', 'powerpoint', 'image', 'file'
]);

interface PersistedAiWorkPackageState {
  sentThisSession: AiSuggestionView[];
  selectedDocumentIds: string[];
  seenDocumentIds: string[];
  aiInstructions: string;
}

// ApiGateway'in ai-gateway-service cluster'ına tanıdığı üst sınırla aynı (bkz. backend
// ApiGateway/appsettings.json → ReverseProxy:Clusters:ai-gateway-service:HttpRequest:ActivityTimeout) —
// bu süreden uzun süredir "üretiliyor" görünen bir bayrak muhtemelen arka planda bir hata/timeout ile
// sonuçlanmış ama bu paneli hiç bilgilendirememiştir; sonsuza kadar "üretiliyor" göstermek yerine temizlenir.
const GENERATION_TIMEOUT_MS = 6 * 60 * 1000;

@Component({
  selector: 'app-ai-work-package-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, Icon, FileTypeIcon],
  templateUrl: './ai-work-package-panel.html',
  styleUrl: './ai-work-package-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiWorkPackagePanel implements OnChanges, OnDestroy {
  @Input({ required: true }) project!: AiWorkPackageProject;
  @Input() documents: ProjectDocumentView[] = [];
  @Output() readonly approved = new EventEmitter<void>();
  @Output() readonly jumpToTasks = new EventEmitter<void>();

  private readonly aiApi = inject(AiSuggestionApiService);
  private readonly toastService = inject(ToastService);

  private readonly aiRequests = signal<AiSuggestionRequestDto[]>([]);
  private generationTimer: ReturnType<typeof setInterval> | null = null;
  private lastLoadedProjectId: string | null = null;

  readonly aiGenerating = signal(false);
  readonly generationElapsedSeconds = signal(0);
  readonly aiInstructions = signal('');
  readonly selectedDocumentIds = signal<Set<string>>(new Set());
  readonly aiDecisionItemIds = signal<Set<string>>(new Set());
  readonly selectedSuggestionIds = signal<Set<string>>(new Set());
  readonly activeFilter = signal<string>('all'); // 'all' | department name
  readonly sentThisSession = signal<AiSuggestionView[]>([]);
  readonly documentListExpanded = signal(false);
  // Bu panelde daha önce görülmüş doküman id'leri — yeni gelen (ilk yükleme dahil) eligible bir dokümanı
  // otomatik seçili başlatmak için, ama kullanıcının kendi elleriyle işaretini kaldırdığı bir dokümanı
  // documents[] referansı her değiştiğinde (ör. başka bir doküman yüklendiğinde) yeniden seçili hâle
  // GERİ GETİRMEMEK için kullanılır — sadece "hiç görülmemiş" id'ler bu seçime dahil edilir.
  private readonly seenDocumentIds = new Set<string>();

  readonly aiEligibleKinds = AI_ELIGIBLE_KINDS;

  readonly aiSuggestions = computed<AiSuggestionView[]>(() => {
    const views: AiSuggestionView[] = [];
    for (const request of this.aiRequests()) {
      for (const item of request.items) {
        if (item.decision === 'Pending') {
          views.push(this.toView(request, item));
        }
      }
    }
    return views;
  });

  readonly departmentCounts = computed(() => {
    const counts = new Map<string, number>();
    for (const suggestion of this.aiSuggestions()) {
      counts.set(suggestion.department, (counts.get(suggestion.department) ?? 0) + 1);
    }
    return [...counts.entries()].map(([department, count]) => ({ department, count }));
  });

  readonly filteredSuggestions = computed(() => {
    const filter = this.activeFilter();
    if (filter === 'all') return this.aiSuggestions();
    return this.aiSuggestions().filter((suggestion) => suggestion.department === filter);
  });

  readonly groupedBySource = computed(() => {
    const groups = new Map<string, AiSuggestionView[]>();
    for (const suggestion of this.filteredSuggestions()) {
      const key = suggestion.source ?? 'Diğer kaynaklar';
      const bucket = groups.get(key) ?? [];
      bucket.push(suggestion);
      groups.set(key, bucket);
    }
    return [...groups.entries()].map(([source, items]) => ({
      source,
      items,
      // Bir grubun tüm önerileri aynı üretim çağrısından geldiği için tek bir öğeye bakmak yeterli —
      // RAG gerçek doküman bağlamı döndürmediyse bu, öneri metninin dokümana değil sadece proje
      // bilgisine dayandığı anlamına gelir (bkz. AiSuggestionAppService.CollectRagDocumentExcerptsAsync).
      usedRealDocumentContext: items.every((item) => item.usedRealDocumentContext)
    }));
  });

  constructor() {}

  // Kullanıcı "İş Paketi Çıkart" derken bir dokümanı manuel işaretlemeyi unutursa (veya bunun gerektiğini
  // hiç bilmezse) RAG o dokümana hiç bakmadan üretim proje açıklamasına dayanarak devam eder — bkz.
  // AiSuggestionAppService.CollectRagDocumentExcerptsAsync: SelectedDocumentIds boşsa RAG'e hiç
  // gidilmez. Bunu varsayılan davranışla önlemek için, panel her yeni (daha önce görülmemiş) eligible
  // dokümanla karşılaştığında onu otomatik seçili başlatır — RAG'in dökümanı ne zaman senkronize edeceği
  // zaten kendi mekanizmasıyla (RagDocumentSyncService, "eskiden yüklenmiş" fark etmeksizin) çözülüyor.
  ngOnChanges(changes: SimpleChanges): void {
    // [project] üst bileşende her change detection turunda yeni bir obje literaliyle yeniden bağlanıyor,
    // bu yüzden id'ye göre kontrol ediyoruz: sayfa ilk açıldığında proje henüz yüklenmemişken id boş
    // geliyor (bkz. EMPTY_PROJECT_VIEW) — o anda çağrı yapmak "/projects/" (boş id) isteğiyle 404'e yol
    // açıyordu ve gerçek id geldiğinde bir daha hiç yeniden denenmiyordu.
    if (changes['project'] && this.project.id && this.project.id !== this.lastLoadedProjectId) {
      this.lastLoadedProjectId = this.project.id;
      this.loadPersistedState(this.project.id);
      void this.loadAiSuggestions();
    }

    if (!changes['documents']) {
      return;
    }

    const newlyEligibleIds = this.documents
      .filter((document) => !this.seenDocumentIds.has(document.id) && this.isDocumentEligible(document.kind))
      .map((document) => document.id);
    for (const document of this.documents) {
      this.seenDocumentIds.add(document.id);
    }

    if (newlyEligibleIds.length > 0) {
      this.selectedDocumentIds.update((set) => {
        const next = new Set(set);
        for (const id of newlyEligibleIds) next.add(id);
        return next;
      });
    }
    this.persistState();
  }

  ngOnDestroy(): void {
    // Bilinçli olarak generatingStorageKey'i BURADA temizlemiyoruz — sekme değişince bu bileşen yok
    // edilir (bkz. project-detail-page.html'deki @switch), ama arka planda hâlâ süren bir üretim isteği
    // varsa "üretiliyor" bayrağının yaşamaya devam etmesi lazım ki kullanıcı sekmeye geri döndüğünde
    // yeni oluşturulan örnek bunu görüp sayacı kaldığı yerden gösterebilsin (bkz. resumeGenerationIfStillInProgress).
    this.clearGenerationTimer();
  }

  private async loadAiSuggestions(): Promise<void> {
    try {
      this.aiRequests.set(await this.aiApi.listByProject(this.project.id));
    } catch {
      // Non-fatal — AIGatewayService kısa süreliğine erişilemez olabilir.
    }
  }

  private stateStorageKey(projectId: string): string {
    return `pm-ai-work-package:${projectId}`;
  }

  private generatingStorageKey(projectId: string): string {
    return `pm-ai-work-package-generating:${projectId}`;
  }

  // Sekmeler arası geçiş (@switch) bu bileşeni yok edip yeniden oluşturuyor (bkz. project-guide-panel.ts'teki
  // sohbet kaybı için uygulanan aynı çözüm) — üretim sırasında/onay sonrasında sekme değiştirilirse sayaç,
  // "Görevler'e gönderildi" özeti, doküman seçimi ve taslak talimat metni sessizce sıfırlanıyordu. Bekleyen
  // öneri LİSTESİ zaten backend'den yeniden çekiliyor (veri kaybı yoktu), ama bu salt-UI durumu yoktu.
  private loadPersistedState(projectId: string): void {
    const raw = localStorage.getItem(this.stateStorageKey(projectId));
    if (raw) {
      try {
        const stored = JSON.parse(raw) as Partial<PersistedAiWorkPackageState>;
        this.sentThisSession.set(stored.sentThisSession ?? []);
        this.selectedDocumentIds.set(new Set(stored.selectedDocumentIds ?? []));
        this.seenDocumentIds.clear();
        for (const id of stored.seenDocumentIds ?? []) this.seenDocumentIds.add(id);
        this.aiInstructions.set(stored.aiInstructions ?? '');
      } catch {
        localStorage.removeItem(this.stateStorageKey(projectId));
      }
    }

    this.resumeGenerationIfStillInProgress(projectId);
  }

  private persistState(): void {
    const projectId = this.project?.id;
    if (!projectId) return;
    const payload: PersistedAiWorkPackageState = {
      sentThisSession: this.sentThisSession(),
      selectedDocumentIds: Array.from(this.selectedDocumentIds()),
      seenDocumentIds: Array.from(this.seenDocumentIds),
      aiInstructions: this.aiInstructions()
    };
    localStorage.setItem(this.stateStorageKey(projectId), JSON.stringify(payload));
  }

  // Sekme değiştirilirken devam eden bir "İş Paketi Çıkart" isteği, o anki bileşen örneği yok edilse bile
  // arka planda çalışmaya devam eder (HTTP çağrısı iptal edilmiyor). Bu, yeni kurulan örneğin bunu
  // bilebilmesi için: üretim başlarken bir zaman damgası yazılır, generateAiSuggestions'ın kendi
  // finally'si (hangi örnekte çalışırsa çalışsın) bunu temizler. Burada bu bayrağı okuyup hâlâ makul bir
  // süre içindeyse sayacı kaldığı yerden gösterir ve bayrak silinene/süre dolana kadar arka planda izler.
  private resumeGenerationIfStillInProgress(projectId: string): void {
    const raw = localStorage.getItem(this.generatingStorageKey(projectId));
    if (!raw) return;

    let startedAt: number;
    try {
      startedAt = (JSON.parse(raw) as { startedAtEpoch: number }).startedAtEpoch;
    } catch {
      localStorage.removeItem(this.generatingStorageKey(projectId));
      return;
    }

    const elapsedMs = Date.now() - startedAt;
    if (!Number.isFinite(startedAt) || elapsedMs < 0 || elapsedMs > GENERATION_TIMEOUT_MS) {
      localStorage.removeItem(this.generatingStorageKey(projectId));
      return;
    }

    this.aiGenerating.set(true);
    this.generationElapsedSeconds.set(Math.floor(elapsedMs / 1000));
    this.clearGenerationTimer();
    this.generationTimer = setInterval(() => {
      const currentElapsedMs = Date.now() - startedAt;
      this.generationElapsedSeconds.set(Math.floor(currentElapsedMs / 1000));

      const stillMarkedGenerating = localStorage.getItem(this.generatingStorageKey(projectId)) !== null;
      if (stillMarkedGenerating && currentElapsedMs < GENERATION_TIMEOUT_MS) {
        return;
      }

      // Bayrak ya silindi (orijinal istek — bu örnekte ya da eski örnekte — tamamlandı) ya da makul süre
      // aşıldı (muhtemelen paneli hiç bilgilendiremeyen bir hata/timeout) — her iki durumda da en güncel
      // öneri listesini çekip "üretiliyor" göstermeyi bırakıyoruz.
      this.clearGenerationTimer();
      this.aiGenerating.set(false);
      localStorage.removeItem(this.generatingStorageKey(projectId));
      void this.loadAiSuggestions();
    }, 1000);
  }

  private toView(request: AiSuggestionRequestDto, item: WorkPackageSuggestionItemDto): AiSuggestionView {
    return {
      requestId: request.id,
      itemId: item.id,
      title: item.title,
      department: item.department,
      effortHours: item.effortHours,
      source: item.sourceDocument,
      description: item.description,
      sequenceNote: item.sequenceNote,
      activities: item.activities.map((activity) => ({
        id: activity.id,
        title: activity.title,
        effortHours: activity.effortHours
      })),
      usedRealDocumentContext: request.usedRealDocumentContext
    };
  }

  isDocumentEligible(kind: DocumentKind): boolean {
    return this.aiEligibleKinds.has(kind);
  }

  toggleDocumentSelection(id: string): void {
    this.selectedDocumentIds.update((set) => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
    this.persistState();
  }

  updateInstructions(value: string): void {
    this.aiInstructions.set(value);
    this.persistState();
  }

  toggleDocumentListExpanded(): void {
    this.documentListExpanded.update((open) => !open);
  }

  setFilter(filter: string): void {
    this.activeFilter.set(filter);
    this.selectedSuggestionIds.set(new Set());
  }

  toggleSuggestionSelected(itemId: string): void {
    this.selectedSuggestionIds.update((set) => {
      const next = new Set(set);
      if (next.has(itemId)) next.delete(itemId);
      else next.add(itemId);
      return next;
    });
  }

  isSuggestionSelected(itemId: string): boolean {
    return this.selectedSuggestionIds().has(itemId);
  }

  isAiSuggestionBusy(itemId: string): boolean {
    return this.aiDecisionItemIds().has(itemId);
  }

  async generateAiSuggestions(): Promise<void> {
    const projectId = this.project.id;
    this.aiGenerating.set(true);
    this.generationElapsedSeconds.set(0);
    const startedAt = Date.now();
    localStorage.setItem(this.generatingStorageKey(projectId), JSON.stringify({ startedAtEpoch: startedAt }));
    this.clearGenerationTimer();
    this.generationTimer = setInterval(() => {
      this.generationElapsedSeconds.set(Math.floor((Date.now() - startedAt) / 1000));
    }, 1000);

    try {
      const result = await this.aiApi.generate({
        projectId,
        extraInstructions: this.aiInstructions().trim() || null,
        selectedDocumentIds: Array.from(this.selectedDocumentIds())
      });
      this.aiRequests.update((requests) => [...requests, result]);
      this.aiInstructions.set('');
      this.selectedDocumentIds.set(new Set());
      this.persistState();
      // Backend, modelin mevcut/daha önce üretilmiş bir başlıkla birebir eşleşen önerilerini sessizce
      // eleyebiliyor (bkz. AiSuggestionAppService.GenerateAsync) — bu durumda kullanıcıya hiçbir yeni
      // satır görünmez; nedenini açıklamazsak "tıkladım, hiçbir şey olmadı" hissi oluşur.
      if (result.items.length === 0) {
        this.toastService.success('Yeni öneri bulunamadı — AI, mevcut kapsamın zaten karşılandığını değerlendirdi.');
      } else if (result.possiblyIncomplete) {
        // AI'nın yanıtı beklenenden az sayıda öneri içeriyor olabilir (bkz. backend
        // GenerateAndParseSuggestionsAsync'in düşük-verim tekrar denemesi son denemede de yetersiz
        // kalırsa) — sessizce "başarılı" göstermek yerine kullanıcıyı bilgilendiriyoruz.
        this.toastService.warning('AI beklenenden az sayıda öneri üretebildi, tekrar denemek isteyebilirsiniz.');
      }
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Öneri üretilemedi.');
    } finally {
      this.aiGenerating.set(false);
      this.clearGenerationTimer();
      // Bu bileşen örneği sekme değişimiyle yok edilmiş olsa bile (bkz. ngOnDestroy) bu satır çalışır —
      // localStorage işlemi Angular yaşam döngüsünden bağımsızdır. Panele geri dönüldüğünde yeni örnek
      // resumeGenerationIfStillInProgress ile bu bayrağın silindiğini görüp "üretiliyor" göstermeyi bırakır.
      localStorage.removeItem(this.generatingStorageKey(projectId));
    }
  }

  private clearGenerationTimer(): void {
    if (this.generationTimer !== null) {
      clearInterval(this.generationTimer);
      this.generationTimer = null;
    }
  }

  private setAiSuggestionBusy(itemId: string, busy: boolean): void {
    this.aiDecisionItemIds.update((current) => {
      const next = new Set(current);
      if (busy) next.add(itemId);
      else next.delete(itemId);
      return next;
    });
  }

  private applyUpdatedAiRequest(updated: AiSuggestionRequestDto): void {
    this.aiRequests.update((requests) => requests.map((r) => (r.id === updated.id ? updated : r)));
  }

  private async performApprove(suggestion: AiSuggestionView): Promise<boolean> {
    this.setAiSuggestionBusy(suggestion.itemId, true);
    try {
      const updated = await this.aiApi.approveItem(suggestion.requestId, suggestion.itemId);
      this.applyUpdatedAiRequest(updated);
      this.sentThisSession.update((list) => [suggestion, ...list]);
      this.persistState();
      return true;
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Onay işlemi başarısız oldu.');
      return false;
    } finally {
      this.setAiSuggestionBusy(suggestion.itemId, false);
    }
  }

  private async performReject(suggestion: AiSuggestionView): Promise<boolean> {
    this.setAiSuggestionBusy(suggestion.itemId, true);
    try {
      const updated = await this.aiApi.rejectItem(suggestion.requestId, suggestion.itemId);
      this.applyUpdatedAiRequest(updated);
      return true;
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Reddetme işlemi başarısız oldu.');
      return false;
    } finally {
      this.setAiSuggestionBusy(suggestion.itemId, false);
    }
  }

  async approveAiSuggestion(suggestion: AiSuggestionView): Promise<void> {
    if (this.isAiSuggestionBusy(suggestion.itemId)) return;
    const success = await this.performApprove(suggestion);
    if (success) {
      this.toastService.success('Onaylandı — iş paketi ve alt görevleri kısa bir gecikmeyle görev listesinde görünecek.');
      this.approved.emit();
    }
  }

  async dismissAiSuggestion(suggestion: AiSuggestionView): Promise<void> {
    if (this.isAiSuggestionBusy(suggestion.itemId)) return;
    await this.performReject(suggestion);
  }

  async bulkApproveSelected(): Promise<void> {
    const targets = this.aiSuggestions().filter((s) => this.selectedSuggestionIds().has(s.itemId));
    if (!targets.length) return;

    let successCount = 0;
    for (const suggestion of targets) {
      if (await this.performApprove(suggestion)) successCount++;
    }
    this.selectedSuggestionIds.set(new Set());
    if (successCount) {
      this.toastService.success(`${successCount} öneri onaylandı — görevler kısa bir gecikmeyle görev listesinde görünecek.`);
      this.approved.emit();
    }
  }

  async bulkRejectSelected(): Promise<void> {
    const targets = this.aiSuggestions().filter((s) => this.selectedSuggestionIds().has(s.itemId));
    if (!targets.length) return;

    let successCount = 0;
    for (const suggestion of targets) {
      if (await this.performReject(suggestion)) successCount++;
    }
    this.selectedSuggestionIds.set(new Set());
    if (successCount) {
      this.toastService.success(`${successCount} öneri reddedildi.`);
    }
  }

  goToTasks(): void {
    this.jumpToTasks.emit();
  }
}
