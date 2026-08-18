import { TestBed } from '@angular/core/testing';
import { ProjectChatApiService } from '../../data/project-chat-api.service';
import { ProjectGuidePanel } from './project-guide-panel';
import { ProjectGuideContext } from './project-guide.models';

function makeContext(projectId: string): ProjectGuideContext {
  return {
    projectId,
    projectName: 'Test Projesi',
    description: '',
    notes: [],
    documents: [],
    tasks: []
  };
}

describe('ProjectGuidePanel', () => {
  const chatApi = {
    ask: vi.fn()
  };

  beforeEach(() => {
    chatApi.ask.mockReset();
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [ProjectGuidePanel],
      providers: [{ provide: ProjectChatApiService, useValue: chatApi }]
    });
  });

  function createComponent(): ProjectGuidePanel {
    const component = TestBed.runInInjectionContext(() => new ProjectGuidePanel());
    component.context = makeContext('project-1');
    component.ngOnChanges({ context: {} as never });
    return component;
  }

  it('starts with the guided welcome state instead of a synthetic chat message', () => {
    const component = createComponent();

    expect(component.messages()).toEqual([]);
  });

  it('renders the mapped {text, sources} reply after asking a question', async () => {
    const component = createComponent();
    chatApi.ask.mockResolvedValue({ text: 'Cevap metni', sources: ['belge.pdf'], usedRealDocumentContext: true });

    component.draft.set('Projenin kapsamı nedir?');
    await component.send();

    expect(chatApi.ask).toHaveBeenCalledWith('project-1', 'Projenin kapsamı nedir?');
    const lastMessage = component.messages().at(-1);
    expect(lastMessage?.role).toBe('guide');
    expect(lastMessage?.text).toBe('Cevap metni');
    expect(lastMessage?.sources).toEqual(['belge.pdf']);
    expect(lastMessage?.usedRealDocumentContext).toBe(true);
    expect(component.sending()).toBe(false);
  });

  it('flags a reply as not using real document context when RAG returned nothing usable', async () => {
    const component = createComponent();
    chatApi.ask.mockResolvedValue({ text: 'Genel bir cevap', sources: [], usedRealDocumentContext: false });

    component.draft.set('Projenin kapsamı nedir?');
    await component.send();

    const lastMessage = component.messages().at(-1);
    expect(lastMessage?.usedRealDocumentContext).toBe(false);
  });

  it('shows a friendly fallback message when the chat API call is rejected', async () => {
    const component = createComponent();
    chatApi.ask.mockRejectedValue(new Error('network error'));

    component.draft.set('Projenin kapsamı nedir?');
    await component.send();

    const lastMessage = component.messages().at(-1);
    expect(lastMessage?.role).toBe('guide');
    expect(lastMessage?.text).toBe('Bir hata oluştu, lütfen tekrar deneyin.');
    expect(component.sending()).toBe(false);
  });

  it('does not send an empty or whitespace-only question', async () => {
    const component = createComponent();

    component.draft.set('   ');
    await component.send();

    expect(chatApi.ask).not.toHaveBeenCalled();
  });

  it('survives being destroyed and recreated for the same project (tab switch)', async () => {
    const first = createComponent();
    chatApi.ask.mockResolvedValue({ text: 'Cevap metni', sources: [], usedRealDocumentContext: true });
    first.draft.set('Projenin kapsamı nedir?');
    await first.send();
    expect(first.messages()).toHaveLength(2);

    // Sekme değişimi bileşeni yok edip yeniden oluşturur — yeni bir örnek aynı proje id'siyle geldiğinde
    // önceki sohbeti hafızadan değil localStorage'dan geri yüklemeli.
    const recreated = createComponent();

    expect(recreated.messages()).toHaveLength(2);
    expect(recreated.messages()[0].text).toBe('Projenin kapsamı nedir?');
  });

  it('does not leak one project chat into another project', async () => {
    const projectOne = createComponent();
    chatApi.ask.mockResolvedValue({ text: 'Cevap 1', sources: [], usedRealDocumentContext: true });
    projectOne.draft.set('Soru 1');
    await projectOne.send();

    const projectTwo = TestBed.runInInjectionContext(() => new ProjectGuidePanel());
    projectTwo.context = makeContext('project-2');
    projectTwo.ngOnChanges({ context: {} as never });

    expect(projectTwo.messages()).toEqual([]);
  });

  it('starting a new chat clears the visible messages and the persisted history', async () => {
    const component = createComponent();
    chatApi.ask.mockResolvedValue({ text: 'Cevap metni', sources: [], usedRealDocumentContext: true });
    component.draft.set('Projenin kapsamı nedir?');
    await component.send();
    expect(component.messages()).toHaveLength(2);

    component.startNewChat();

    expect(component.messages()).toEqual([]);
    const recreated = createComponent();
    expect(recreated.messages()).toEqual([]);
  });
});
