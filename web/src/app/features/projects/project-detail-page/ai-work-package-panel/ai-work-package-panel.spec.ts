import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '../../../../shared/toast/toast.service';
import { AiSuggestionApiService } from '../../data/ai-suggestion-api.service';
import { AiSuggestionRequestDto, WorkPackageSuggestionItemDto } from '../../data/ai-suggestion-api.models';
import { AiWorkPackagePanel } from './ai-work-package-panel';

function makeRequest(
  item: WorkPackageSuggestionItemDto,
  usedRealDocumentContext = true,
  possiblyIncomplete = false
): AiSuggestionRequestDto {
  return {
    id: 'request-1',
    projectId: 'project-1',
    projectType: 'Basit',
    extraInstructions: null,
    providerUsed: 'Test',
    createdAtUtc: new Date().toISOString(),
    selectedDocumentNames: [],
    items: [item],
    usedRealDocumentContext,
    possiblyIncomplete
  };
}

describe('AiWorkPackagePanel', () => {
  const aiApi = {
    listByProject: vi.fn(),
    generate: vi.fn(),
    approveItem: vi.fn(),
    rejectItem: vi.fn()
  };

  beforeEach(() => {
    aiApi.listByProject.mockReset().mockResolvedValue([]);
    aiApi.generate.mockReset();
    aiApi.approveItem.mockReset();
    aiApi.rejectItem.mockReset();
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [AiWorkPackagePanel],
      providers: [provideHttpClient(), { provide: AiSuggestionApiService, useValue: aiApi }]
    });
  });

  function createComponent(): AiWorkPackagePanel {
    const component = TestBed.runInInjectionContext(() => new AiWorkPackagePanel());
    component.project = { id: 'project-1', name: 'Test Projesi', type: 'Basit', unit: 'Test Birimi' };
    return component;
  }

  it('removes an approved AI suggestion from the pending list only after the approval API succeeds', async () => {
    const component = createComponent();
    const pendingItem: WorkPackageSuggestionItemDto = {
      id: 'item-1',
      title: 'Onay bekleyen görev',
      department: 'Bilgi Teknolojileri',
      effortHours: 8,
      sourceDocument: null,
      decision: 'Pending',
      description: null,
      sequenceNote: null,
      insertAfterTaskTitle: null,
      sequenceRank: null,
      isAtProjectStart: false,
      activities: []
    };
    (component as unknown as { aiRequests: { set: (v: AiSuggestionRequestDto[]) => void } }).aiRequests.set([
      makeRequest(pendingItem)
    ]);
    aiApi.approveItem.mockResolvedValue(makeRequest({ ...pendingItem, decision: 'Approved' }));

    const suggestion = component.aiSuggestions()[0];
    await component.approveAiSuggestion(suggestion);

    expect(aiApi.approveItem).toHaveBeenCalledWith('request-1', 'item-1');
    expect(component.aiSuggestions()).toEqual([]);
    expect(component.sentThisSession()).toHaveLength(1);
  });

  it('removes a rejected AI suggestion without approving it', async () => {
    const component = createComponent();
    const pendingItem: WorkPackageSuggestionItemDto = {
      id: 'item-2',
      title: 'Reddedilecek görev',
      department: 'Satın Alma',
      effortHours: 4,
      sourceDocument: null,
      decision: 'Pending',
      description: null,
      sequenceNote: null,
      insertAfterTaskTitle: null,
      sequenceRank: null,
      isAtProjectStart: false,
      activities: []
    };
    (component as unknown as { aiRequests: { set: (v: AiSuggestionRequestDto[]) => void } }).aiRequests.set([
      makeRequest(pendingItem)
    ]);
    aiApi.rejectItem.mockResolvedValue(makeRequest({ ...pendingItem, decision: 'Rejected' }));

    const suggestion = component.aiSuggestions()[0];
    await component.dismissAiSuggestion(suggestion);

    expect(aiApi.rejectItem).toHaveBeenCalledWith('request-1', 'item-2');
    expect(aiApi.approveItem).not.toHaveBeenCalled();
    expect(component.aiSuggestions()).toEqual([]);
  });

  it('does not fetch AI requests while the project id is still empty (loading placeholder)', () => {
    const component = createComponent();
    component.project = { id: '', name: 'Yükleniyor…', type: '', unit: '' };

    component.ngOnChanges({ project: {} as never });

    expect(aiApi.listByProject).not.toHaveBeenCalled();
  });

  it('fetches AI requests once the real project id arrives, and does not refetch for the same id', async () => {
    const component = createComponent();
    component.project = { id: '', name: 'Yükleniyor…', type: '', unit: '' };
    component.ngOnChanges({ project: {} as never });

    component.project = { id: 'project-1', name: 'Gerçek Proje', type: 'Basit', unit: 'BT' };
    component.ngOnChanges({ project: {} as never });
    await Promise.resolve();

    expect(aiApi.listByProject).toHaveBeenCalledWith('project-1');
    expect(aiApi.listByProject).toHaveBeenCalledTimes(1);

    component.project = { id: 'project-1', name: 'Gerçek Proje', type: 'Basit', unit: 'BT' };
    component.ngOnChanges({ project: {} as never });
    await Promise.resolve();

    expect(aiApi.listByProject).toHaveBeenCalledTimes(1);
  });

  it('treats every RAG-supported document kind as eligible for AI generation, except video', () => {
    const component = createComponent();

    expect(component.isDocumentEligible('pdf')).toBe(true);
    expect(component.isDocumentEligible('word')).toBe(true);
    expect(component.isDocumentEligible('excel')).toBe(true);
    expect(component.isDocumentEligible('powerpoint')).toBe(true);
    expect(component.isDocumentEligible('image')).toBe(true);
    expect(component.isDocumentEligible('file')).toBe(true);
    expect(component.isDocumentEligible('video')).toBe(false);
  });

  it('auto-selects newly seen eligible documents without re-selecting a manually deselected one', () => {
    const component = createComponent();
    const baseDoc = { noteId: null, uploadedBy: null, size: '1 MB', sizeBytes: 1, createdAtUtc: new Date().toISOString() };
    const docs = [
      { ...baseDoc, id: 'doc-1', name: 'rapor.pdf', kind: 'pdf' as const },
      { ...baseDoc, id: 'doc-2', name: 'video.mp4', kind: 'video' as const }
    ];

    component.documents = docs;
    component.ngOnChanges({ documents: {} as never });
    expect(component.selectedDocumentIds().has('doc-1')).toBe(true);
    expect(component.selectedDocumentIds().has('doc-2')).toBe(false);

    component.toggleDocumentSelection('doc-1');
    expect(component.selectedDocumentIds().has('doc-1')).toBe(false);

    const doc3 = { ...baseDoc, id: 'doc-3', name: 'excel.xlsx', kind: 'excel' as const };
    component.documents = [...docs, doc3];
    component.ngOnChanges({ documents: {} as never });
    expect(component.selectedDocumentIds().has('doc-1')).toBe(false);
    expect(component.selectedDocumentIds().has('doc-3')).toBe(true);
  });

  it('persists the approval summary, document selection and draft instructions across a tab switch (destroy + recreate)', async () => {
    // ARGE-öncesi denetimde bulunan hata: sekmeler arası geçiş (@switch) bu bileşeni yok edip yeniden
    // oluşturuyor (project-guide-panel.ts'teki sohbet kaybıyla birebir aynı kök neden) — "Görevler'e
    // gönderildi" özeti, doküman seçimi ve taslak talimat metni sessizce sıfırlanıyordu.
    const component = createComponent();
    component.ngOnChanges({ project: {} as never });
    await Promise.resolve();

    const pendingItem: WorkPackageSuggestionItemDto = {
      id: 'item-9',
      title: 'Kalıcılık testi görevi',
      department: 'BT',
      effortHours: 6,
      sourceDocument: null,
      decision: 'Pending',
      description: null,
      sequenceNote: null,
      insertAfterTaskTitle: null,
      sequenceRank: null,
      isAtProjectStart: false,
      activities: []
    };
    (component as unknown as { aiRequests: { set: (v: AiSuggestionRequestDto[]) => void } }).aiRequests.set([
      makeRequest(pendingItem)
    ]);
    aiApi.approveItem.mockResolvedValue(makeRequest({ ...pendingItem, decision: 'Approved' }));
    await component.approveAiSuggestion(component.aiSuggestions()[0]);
    expect(component.sentThisSession()).toHaveLength(1);

    component.updateInstructions('Özellikle güvenlik konularına dikkat edilsin');
    component.toggleDocumentSelection('doc-99');

    // Sekme değişimi: bileşen yok edilip aynı proje id'siyle yeniden oluşturuluyor.
    const recreated = createComponent();
    recreated.ngOnChanges({ project: {} as never });
    await Promise.resolve();

    expect(recreated.sentThisSession()).toHaveLength(1);
    expect(recreated.sentThisSession()[0].title).toBe('Kalıcılık testi görevi');
    expect(recreated.aiInstructions()).toBe('Özellikle güvenlik konularına dikkat edilsin');
    expect(recreated.selectedDocumentIds().has('doc-99')).toBe(true);
  });

  it('resumes the elapsed counter and detects completion after being destroyed and recreated mid-generation (tab switch)', async () => {
    // Devam eden bir "İş Paketi Çıkart" isteği, bileşen sekme değişimiyle yok edilse bile arka planda
    // çalışmaya devam eder (HTTP çağrısı iptal edilmiyor). Yeni oluşturulan örnek, sayacı kaldığı yerden
    // göstermeli ve orijinal istek tamamlandığında bunu fark edip "üretiliyor" göstermeyi bırakmalı.
    vi.useFakeTimers();
    try {
      let resolveGenerate!: (value: AiSuggestionRequestDto) => void;
      aiApi.generate.mockReturnValue(
        new Promise<AiSuggestionRequestDto>((resolve) => {
          resolveGenerate = resolve;
        })
      );

      const component = createComponent();
      component.ngOnChanges({ project: {} as never });
      await Promise.resolve();

      const genPromise = component.generateAiSuggestions();
      await vi.advanceTimersByTimeAsync(5000);
      expect(component.generationElapsedSeconds()).toBe(5);

      // Sekme değişimi: bileşen yok ediliyor ama arka plandaki istek devam ediyor.
      component.ngOnDestroy();

      const recreated = createComponent();
      recreated.ngOnChanges({ project: {} as never });
      await Promise.resolve();

      expect(recreated.aiGenerating()).toBe(true);
      expect(recreated.generationElapsedSeconds()).toBe(5);

      const pendingItem: WorkPackageSuggestionItemDto = {
        id: 'item-10',
        title: 'Geç gelen öneri',
        department: 'BT',
        effortHours: 4,
        sourceDocument: null,
        decision: 'Pending',
        description: null,
        sequenceNote: null,
        insertAfterTaskTitle: null,
        sequenceRank: null,
        isAtProjectStart: false,
        activities: []
      };
      resolveGenerate(makeRequest(pendingItem));
      await genPromise;

      await vi.advanceTimersByTimeAsync(1000);

      expect(recreated.aiGenerating()).toBe(false);
      expect(aiApi.listByProject).toHaveBeenCalled();
    } finally {
      vi.useRealTimers();
    }
  });

  it('shows a warning toast when the generation result is flagged as possibly incomplete', async () => {
    // ARGE-öncesi denetimde bulunan bir gedik: son denemede de düşük verimle kabul edilen bir sonuç
    // sessizce "başarılı" gösteriliyordu — backend artık possiblyIncomplete işaretliyor, panel bunu
    // görünür bir uyarıya çevirmeli.
    const component = createComponent();
    component.ngOnChanges({ project: {} as never });
    await Promise.resolve();

    const pendingItem: WorkPackageSuggestionItemDto = {
      id: 'item-11',
      title: 'Kısmi öneri',
      department: 'BT',
      effortHours: 5,
      sourceDocument: null,
      decision: 'Pending',
      description: null,
      sequenceNote: null,
      insertAfterTaskTitle: null,
      sequenceRank: null,
      isAtProjectStart: false,
      activities: []
    };
    aiApi.generate.mockResolvedValue(makeRequest(pendingItem, true, true));

    await component.generateAiSuggestions();

    const toastService = TestBed.inject(ToastService);
    expect(toastService.messages().some((m) => m.type === 'warning')).toBe(true);
  });

  it('does not silently re-select a manually deselected document after a tab switch (destroy + recreate)', async () => {
    // ARGE-öncesi denetimde bulunan hata: seenDocumentIds instance-scoped olduğu için, sekme değişince
    // bileşen yeniden kurulduğunda kullanıcının elle kaldırdığı bir doküman "hiç görülmemiş" sayılıp
    // otomatik olarak yeniden seçiliyordu. seenDocumentIds artık selectedDocumentIds ile birlikte
    // kalıcı — bu test doğrudan bu senaryoyu doğrular.
    const baseDoc = { noteId: null, uploadedBy: null, size: '1 MB', sizeBytes: 1, createdAtUtc: new Date().toISOString() };
    const docs = [{ ...baseDoc, id: 'doc-1', name: 'rapor.pdf', kind: 'pdf' as const }];

    const component = createComponent();
    component.ngOnChanges({ project: {} as never });
    component.documents = docs;
    component.ngOnChanges({ documents: {} as never });
    await Promise.resolve();
    expect(component.selectedDocumentIds().has('doc-1')).toBe(true);

    component.toggleDocumentSelection('doc-1');
    expect(component.selectedDocumentIds().has('doc-1')).toBe(false);

    // Sekme değişimi: bileşen yok edilip aynı proje id'siyle ve aynı dokümanlarla yeniden oluşturuluyor.
    const recreated = createComponent();
    recreated.ngOnChanges({ project: {} as never });
    recreated.documents = docs;
    recreated.ngOnChanges({ documents: {} as never });
    await Promise.resolve();

    expect(recreated.selectedDocumentIds().has('doc-1')).toBe(false);
  });

  it('flags a source group as not using real document context when RAG returned no retrieved context', () => {
    const component = createComponent();
    const pendingItem: WorkPackageSuggestionItemDto = {
      id: 'item-3',
      title: 'Genel bir öneri',
      department: 'Bilgi Teknolojileri',
      effortHours: 8,
      sourceDocument: 'rapor.docx',
      decision: 'Pending',
      description: null,
      sequenceNote: null,
      insertAfterTaskTitle: null,
      sequenceRank: null,
      isAtProjectStart: false,
      activities: []
    };
    (component as unknown as { aiRequests: { set: (v: AiSuggestionRequestDto[]) => void } }).aiRequests.set([
      makeRequest(pendingItem, false)
    ]);

    expect(component.groupedBySource()).toHaveLength(1);
    expect(component.groupedBySource()[0].usedRealDocumentContext).toBe(false);
  });
});
