import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, Input, OnChanges, SimpleChanges, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Icon } from '../../../../shared/icon/icon';
import { ProjectChatApiService } from '../../data/project-chat-api.service';
import { ProjectGuideContext } from './project-guide.models';

interface ProjectGuideMessage {
  id: string;
  role: 'visitor' | 'guide';
  text: string;
  time: string;
  sources: string[];
  usedRealDocumentContext: boolean;
}

const EMPTY_CONTEXT: ProjectGuideContext = {
  projectId: '',
  projectName: '',
  description: '',
  notes: [],
  documents: [],
  tasks: []
};

@Component({
  selector: 'app-project-guide-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './project-guide-panel.html',
  styleUrl: './project-guide-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectGuidePanel implements OnChanges {
  @Input() context: ProjectGuideContext = EMPTY_CONTEXT;
  @Input() currentUserName = '';

  private readonly chatApi = inject(ProjectChatApiService);
  private readonly conversation = viewChild<ElementRef<HTMLElement>>('conversation');
  private activeProjectId = '';
  private messageSequence = 0;

  readonly messages = signal<ProjectGuideMessage[]>([]);
  readonly draft = signal('');
  readonly sending = signal(false);
  // RAG bazen tek bir yanıt için onlarca kaynak bölümü (section) döndürebiliyor — hepsini mesajın altına
  // açık şekilde dökmek (bkz. eski davranış) sohbeti okunamaz hale getiriyordu. Varsayılan kapalı: sadece
  // kullanıcı "Kaynakları göster" derse ilgili mesaj id'si buraya eklenir.
  readonly expandedSourceMessageIds = signal<Set<string>>(new Set());
  // Doküman-odaklı: RAG servisi sadece bu projeye yüklenmiş DOKÜMANLARI biliyor, görev/not veri modelini
  // bilmiyor — bu yüzden eskiden burada olan "Görevlerin durumu nedir?" gibi görev-odaklı sorular artık
  // cevaplanamaz (bunlar Görevler sekmesinde zaten görünür durumda). Kapsamı dürüstçe küçültmek, erişemediği
  // bir veriye erişiyormuş gibi göstermekten daha iyi.
  readonly suggestions = [
    'Yüklenen dokümanlara göre projenin kapsamı nedir?',
    'Dokümanlarda belirtilen teknik gereksinimler neler?',
    'Hangi dokümanlar yüklendi?',
    'Dokümanlara göre teslim tarihleri veya kilometre taşları var mı?'
  ];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['context'] && this.context.projectId && this.context.projectId !== this.activeProjectId) {
      this.activeProjectId = this.context.projectId;
      this.loadPersistedMessages();
    }
  }

  async send(): Promise<void> {
    await this.sendQuestion(this.draft());
  }

  // Sekmeler arası geçiş (@switch) bu bileşeni yok edip yeniden oluşturuyor — sohbet sadece bellekte
  // tutulsaydı her sekme değişiminde kaybolurdu. Tarayıcının kendi belleğinde (localStorage) projeye göre
  // saklayarak hem sekme geçişlerinde hem sayfa yenilemesinde korunmasını sağlıyoruz.
  startNewChat(): void {
    this.messageSequence = 0;
    this.messages.set([]);
    this.expandedSourceMessageIds.set(new Set());
    if (this.activeProjectId) {
      localStorage.removeItem(this.storageKey(this.activeProjectId));
    }
  }

  async askSuggestion(question: string): Promise<void> {
    await this.sendQuestion(question);
  }

  onComposerKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' || event.shiftKey) return;
    event.preventDefault();
    void this.send();
  }

  userInitials(): string {
    return initials(this.currentUserName || 'Kullanıcı');
  }

  isSourcesExpanded(messageId: string): boolean {
    return this.expandedSourceMessageIds().has(messageId);
  }

  toggleSources(messageId: string): void {
    this.expandedSourceMessageIds.update((set) => {
      const next = new Set(set);
      if (next.has(messageId)) next.delete(messageId);
      else next.add(messageId);
      return next;
    });
  }

  private async sendQuestion(value: string): Promise<void> {
    const question = value.trim();
    if (!question || this.sending()) return;

    this.draft.set('');
    this.messages.update((messages) => [...messages, this.createMessage('visitor', question)]);
    this.persistMessages();
    this.sending.set(true);
    this.scrollToBottom();
    try {
      const reply = await this.chatApi.ask(this.context.projectId, question);
      this.messages.update((messages) => [
        ...messages,
        this.createMessage('guide', reply.text, reply.sources, reply.usedRealDocumentContext)
      ]);
    } catch {
      // Sahte servis hiç reddedemediği için bu catch eskiden yoktu — gerçek bir HTTP çağrısı artık
      // ağ/servis hatasıyla reddedebilir, kullanıcıya sessizce takılı kalmak yerine bir mesaj gösterilir.
      this.messages.update((messages) => [...messages, this.createMessage('guide', 'Bir hata oluştu, lütfen tekrar deneyin.')]);
    } finally {
      this.sending.set(false);
      this.persistMessages();
      this.scrollToBottom();
    }
  }

  private storageKey(projectId: string): string {
    return `pm-project-guide-chat:${projectId}`;
  }

  private loadPersistedMessages(): void {
    this.messages.set([]);
    this.messageSequence = 0;
    const raw = localStorage.getItem(this.storageKey(this.activeProjectId));
    if (!raw) return;
    try {
      const stored = JSON.parse(raw) as ProjectGuideMessage[];
      this.messages.set(stored);
      this.messageSequence = stored.length;
    } catch {
      localStorage.removeItem(this.storageKey(this.activeProjectId));
    }
  }

  private persistMessages(): void {
    if (!this.activeProjectId) return;
    localStorage.setItem(this.storageKey(this.activeProjectId), JSON.stringify(this.messages()));
  }

  private createMessage(
    role: ProjectGuideMessage['role'],
    text: string,
    sources: string[] = [],
    usedRealDocumentContext = true
  ): ProjectGuideMessage {
    this.messageSequence += 1;
    return {
      id: `${role}-${this.messageSequence}`,
      role,
      text,
      time: new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit' }).format(new Date()),
      sources,
      usedRealDocumentContext
    };
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const element = this.conversation()?.nativeElement;
      if (element) element.scrollTop = element.scrollHeight;
    });
  }
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (!parts.length) return 'K';
  if (parts.length === 1) return parts[0].slice(0, 2).toLocaleUpperCase('tr-TR');
  return `${parts[0][0]}${parts.at(-1)?.[0] ?? ''}`.toLocaleUpperCase('tr-TR');
}
