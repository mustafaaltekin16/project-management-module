import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Icon, IconName } from '../../../shared/icon/icon';
import { AuthService } from '../../../shared/auth/auth.service';
import { BackendProjectType } from '../data/project-api.models';
import {
  CreateTemplateFieldRequest,
  TemplateDto,
  TemplateFieldDto,
  TemplateFieldKind
} from '../data/template-api.models';
import { TemplateApiService } from '../data/template-api.service';

interface RailItem {
  icon: IconName;
  label: string;
  active?: boolean;
}

export interface ToolItem {
  id: string;
  label: string;
  icon: IconName;
}

interface ToolGroup {
  label: string;
  items: ToolItem[];
}

export interface TemplateFormNode {
  id: string;
  label: string;
  hint: string;
  contentType: string;
  listName: string | null;
  isRequired: boolean;
  isActive: boolean;
  kind: TemplateFieldKind;
  systemKey: string | null;
  options: string[];
}

interface TypeSpecificCard {
  icon: IconName;
  title: string;
  description: string;
}

const PROJECT_TYPE_TO_BACKEND: Record<string, BackendProjectType> = {
  'Basit Proje': 'Simple',
  'Çok Birimli Proje': 'MultiUnit'
};

const BACKEND_TO_PROJECT_TYPE: Record<BackendProjectType, string> = {
  Simple: 'Basit Proje',
  MultiUnit: 'Çok Birimli Proje',
  FeasibilityBased: 'Çok Birimli Proje'
};

const LOCKED_REQUIRED_SYSTEM_KEYS = new Set(['projectName', 'unit', 'startDate', 'endDate', 'manager']);
const LOCKED_ACTIVE_SYSTEM_KEYS = new Set(['projectName', 'unit', 'startDate', 'endDate', 'manager']);
function systemNode(
  id: string,
  systemKey: string,
  label: string,
  contentType: string,
  hint: string,
  isRequired: boolean,
  isActive = true
): TemplateFormNode {
  return {
    id,
    label,
    hint,
    contentType,
    listName: null,
    isRequired,
    isActive,
    kind: 'System',
    systemKey,
    options: []
  };
}

function defaultSystemNodes(projectType = 'Çok Birimli Proje'): TemplateFormNode[] {
  const isSimple = projectType === 'Basit Proje';
  const fields = [
    systemNode('system-project-name', 'projectName', 'Proje Adı', 'text', 'Projeyi tanımlayan kısa ve ayırt edici ad', true),
    systemNode('system-start-date', 'startDate', 'Başlangıç Tarihi', 'date', 'Planlanan başlangıç tarihi', true),
    systemNode('system-end-date', 'endDate', 'Bitiş Tarihi', 'date', 'Planlanan bitiş tarihi', true),
    systemNode('system-description', 'description', 'Proje Açıklaması', 'textarea', 'Amaç, kapsam ve beklenen sonucu açıklayın', false),
    systemNode('system-attachments', 'attachments', 'Dosya Ekleyin', 'attachment', 'Talep, analiz veya destekleyici dokümanlar', false),
    systemNode('system-manager', 'manager', 'Proje Yöneticisi', 'employee', 'Yönetici seçiniz', true),
    systemNode('system-unit', 'unit', 'Birim', 'text', 'Projeden sorumlu ana birim', true)
  ];
  if (!isSimple) {
    fields.push(systemNode('system-budget', 'budget', 'Bütçe', 'currency', 'Planlanan proje bütçesi', true));
  }
  return fields;
}

@Component({
  selector: 'app-project-template-builder-page',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule, Icon],
  templateUrl: './project-template-builder-page.html',
  styleUrl: './project-template-builder-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectTemplateBuilderPage implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly templateApi = inject(TemplateApiService);
  private readonly authService = inject(AuthService);

  readonly railCompact = signal(false);
  readonly profileOpen = signal(false);
  readonly previewMode = signal(false);
  readonly saveDialogOpen = signal(false);
  readonly fieldMenuId = signal<string | null>(null);
  readonly selectedTool = signal<string | null>(null);
  readonly templateName = signal('');
  readonly projectType = signal('Çok Birimli Proje');
  // 'Çok Birimli Proje' etiketi hem MultiUnit hem FeasibilityBased backend türünü temsil eder (bkz.
  // BACKEND_TO_PROJECT_TYPE) — bu, yüklenen şablonun gerçek backend türünü saklar ki kullanıcı toggle'a
  // dokunmadan kaydettiğinde FeasibilityBased sessizce MultiUnit'e düşürülmesin.
  private loadedApplicableProjectType: BackendProjectType | null = null;
  readonly errorMessage = signal('');
  readonly statusMessage = signal('');
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly dirty = signal(false);
  readonly formNodes = signal<TemplateFormNode[]>(defaultSystemNodes());
  readonly selectedFieldId = signal<string | null>(null);
  readonly selectedField = computed(() =>
    this.formNodes().find((field) => field.id === this.selectedFieldId()) ?? null
  );
  readonly fieldHint = computed(() => this.selectedField()?.hint ?? '');
  readonly contentType = computed(() => this.selectedField()?.contentType ?? 'text');
  readonly addedFields = computed(() => this.formNodes().filter((field) => field.kind !== 'System'));
  readonly systemCanvasNodes = computed(() => this.formNodes()
    .filter((node) => node.kind === 'System' && this.isApplicableSystemNode(node)));
  readonly customCanvasNodes = computed(() =>
    this.formNodes().filter((node) => node.kind !== 'System')
  );
  readonly visibleCanvasNodes = computed(() =>
    this.formNodes().filter((node) => this.isApplicableSystemNode(node))
  );
  readonly currentUserName = computed(() => this.authService.currentUser()?.displayName ?? '');
  readonly isEditing = computed(() => Boolean(this.savedTemplateId()));
  readonly pageTitle = computed(() => this.isEditing() ? 'Şablonu Düzenle' : 'Yeni Şablon Oluştur');
  readonly typeSpecificTitle = computed(() => {
    if (this.projectType() === 'Basit Proje') return 'Departmanlar';
    return 'İş Paketleri ve Departmanlar';
  });
  readonly typeSpecificDescription = computed(() => {
    if (this.projectType() === 'Basit Proje') {
      return 'Basit projelerde departmanlar ve sorumlu departman yöneticileri proje genel tarihleriyle tanımlanır.';
    }
    return 'İş paketleri sorumlu departman, yönetici ve tarih aralığıyla tanımlanır.';
  });
  readonly typeSpecificCards = computed<TypeSpecificCard[]>(() => {
    if (this.projectType() === 'Basit Proje') {
      return [
        {
          icon: 'building',
          title: 'Departmanlar',
          description: 'Projeye katılacak aktif departmanlar çalışan dizininden seçilir.'
        },
        {
          icon: 'user',
          title: 'Departman Yöneticileri',
          description: 'Her departman için sorumlu yönetici açıkça belirlenir.'
        }
      ];
    }

    return [
      {
        icon: 'layers',
        title: 'İş Paketi',
        description: 'Her çalışma ayrı bir iş paketi başlığı altında planlanır.'
      },
      {
        icon: 'building',
        title: 'Sorumlu Departman',
        description: 'İş paketine aktif bir departman ve departman yöneticisi atanır.'
      },
      {
        icon: 'calendar',
        title: 'Planlanan Tarihler',
        description: 'Her iş paketi için başlangıç ve bitiş aralığı belirlenir.'
      }
    ];
  });

  private nextFieldId = 1;
  private readonly savedTemplateId = signal<string | null>(null);

  readonly toolGroups: ToolGroup[] = [
    {
      label: 'Düzen Araçları',
      items: [
        { id: 'section', label: 'Bölüm', icon: 'table' },
        { id: 'table', label: 'Tablo', icon: 'table' }
      ]
    },
    {
      label: 'Yazı Araçları',
      items: [
        { id: 'text', label: 'Normal Yazı', icon: 'type' },
        { id: 'paragraph', label: 'Paragraf Yazısı', icon: 'paragraph' },
        { id: 'numbers', label: 'Numaralar', icon: 'list' }
      ]
    },
    {
      label: 'Numaralar',
      items: [
        { id: 'date', label: 'Tarih Seçici', icon: 'calendar' },
        { id: 'datetime', label: 'Tarih ve Saat', icon: 'clock' }
      ]
    },
    {
      label: 'Çoklu Elementler',
      items: [
        { id: 'form', label: 'Form Elemanı', icon: 'form' },
        { id: 'select', label: 'Listeden Seçim', icon: 'checkbox' },
        { id: 'checkbox', label: 'Checkbox', icon: 'checkbox' },
        { id: 'boolean', label: 'Evet/Hayır', icon: 'toggle' },
        { id: 'profiles', label: 'Profiller', icon: 'user' },
        { id: 'checklist', label: 'Checklist', icon: 'list' }
      ]
    },
    {
      label: 'Medya Elementler',
      items: [
        { id: 'attachment', label: 'Dosya Eki', icon: 'paperclip' },
        { id: 'image', label: 'Resim', icon: 'image' },
        { id: 'signature', label: 'İmza Alanı', icon: 'signature' }
      ]
    }
  ];

  readonly primaryRail: RailItem[] = [
    { icon: 'dashboard', label: 'Uygulamalar' },
    { icon: 'user', label: 'Profil' },
    { icon: 'ai', label: 'Destek' }
  ];

  readonly moduleRail: RailItem[] = [
    { icon: 'projects', label: 'Portföy' },
    { icon: 'calendar', label: 'Takvim' },
    { icon: 'tasks', label: 'Görevler' },
    { icon: 'share', label: 'İş Akışları' },
    { icon: 'feasibility', label: 'Dokümanlar' },
    { icon: 'check', label: 'Onaylar' },
    { icon: 'users', label: 'Ekipler' },
    { icon: 'layers', label: 'Varlıklar' },
    { icon: 'building', label: 'Organizasyon' },
    { icon: 'wallet', label: 'Bütçe' },
    { icon: 'apps', label: 'Projeler', active: true },
    { icon: 'dashboard', label: 'Raporlar' }
  ];

  readonly utilityRail: RailItem[] = [
    { icon: 'projects', label: 'Arşiv' },
    { icon: 'feasibility', label: 'Şablonlar' },
    { icon: 'check', label: 'Kontroller' },
    { icon: 'sliders', label: 'Ayarlar' }
  ];

  async ngOnInit(): Promise<void> {
    const templateId = this.route.snapshot.paramMap.get('templateId');
    if (!templateId) return;

    this.loading.set(true);
    try {
      const template = await this.templateApi.getById(templateId);
      this.loadTemplate(template);
      this.savedTemplateId.set(template.id);
      this.dirty.set(false);
    } catch (error) {
      this.errorMessage.set(error instanceof Error ? error.message : 'Şablon yüklenemedi.');
    } finally {
      this.loading.set(false);
    }
  }

  addTool(tool: ToolItem): void {
    const definitions: Record<string, Omit<TemplateFormNode, 'id'>> = {
      section: {
        label: 'Yeni Bölüm',
        hint: 'Bu bölümde toplanacak bilgileri açıklayın',
        contentType: 'section',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Section',
        systemKey: null,
        options: []
      },
      table: {
        label: 'Bilgi Tablosu',
        hint: 'Satır bazlı bilgileri girin',
        contentType: 'table',
        listName: 'manual',
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: ['Başlık', 'Değer']
      },
      text: {
        label: 'Kısa Metin Alanı',
        hint: 'Metin giriniz',
        contentType: 'text',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      paragraph: {
        label: 'Açıklama Alanı',
        hint: 'Açıklama giriniz',
        contentType: 'textarea',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      numbers: {
        label: 'Sayısal Alan',
        hint: 'Değer giriniz',
        contentType: 'number',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      date: {
        label: 'Tarih',
        hint: 'Tarih seçiniz',
        contentType: 'date',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      datetime: {
        label: 'Tarih ve Saat',
        hint: 'Tarih ve saat seçiniz',
        contentType: 'datetime',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      select: {
        label: 'Listeden Seçim',
        hint: 'Bir değer seçiniz',
        contentType: 'select',
        listName: 'manual',
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: ['Seçenek 1', 'Seçenek 2']
      },
      form: {
        label: 'Form Elemanı',
        hint: 'Birlikte doldurulacak alt alanları girin',
        contentType: 'formGroup',
        listName: 'manual',
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: ['Alt Alan 1', 'Alt Alan 2']
      },
      checkbox: {
        label: 'Onay Alanı',
        hint: 'Onaylayınız',
        contentType: 'checkbox',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      boolean: {
        label: 'Evet/Hayır Alanı',
        hint: 'Evet veya hayır seçiniz',
        contentType: 'yesNo',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      profiles: {
        label: 'Çalışan',
        hint: 'Çalışan seçiniz',
        contentType: 'employee',
        listName: 'employee',
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      checklist: {
        label: 'Kontrol Listesi',
        hint: 'Tamamlanan maddeleri işaretleyin',
        contentType: 'checklist',
        listName: 'manual',
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: ['Kontrol maddesi 1', 'Kontrol maddesi 2']
      },
      attachment: {
        label: 'Dosya Eki',
        hint: 'Destekleyici dokümanı seçin',
        contentType: 'attachment',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      image: {
        label: 'Resim',
        hint: 'Görsel dosyası seçin',
        contentType: 'image',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      },
      signature: {
        label: 'İmza Alanı',
        hint: 'İmzalayanın adını ve soyadını girin',
        contentType: 'signature',
        listName: null,
        isRequired: false,
        isActive: true,
        kind: 'Custom',
        systemKey: null,
        options: []
      }
    };
    const definition = definitions[tool.id];
    if (!definition) return;

    this.selectedTool.set(tool.id);
    const node: TemplateFormNode = { id: `field-${this.nextFieldId++}`, ...definition };
    this.formNodes.update((nodes) => [...nodes, node]);
    this.selectField(node.id);
    this.markDirty();
  }

  clearCanvasSelection(): void {
    this.selectedFieldId.set(null);
    this.selectedTool.set(null);
    this.fieldMenuId.set(null);
  }

  closeFieldMenu(): void {
    this.fieldMenuId.set(null);
  }

  selectField(id: string): void {
    if (!this.formNodes().some((node) => node.id === id)) return;
    this.selectedFieldId.set(id);
    this.fieldMenuId.set(null);
    this.errorMessage.set('');
  }

  toggleFieldMenu(id: string, event?: Event): void {
    event?.stopPropagation();
    this.selectedFieldId.set(id);
    this.fieldMenuId.update((openId) => openId === id ? null : id);
  }

  dropField(event: CdkDragDrop<TemplateFormNode[]>): void {
    if (event.previousIndex === event.currentIndex) return;
    const visible = this.visibleCanvasNodes();
    const moving = visible[event.previousIndex];
    const target = visible[event.currentIndex];
    if (!moving || !target) return;
    this.formNodes.update((nodes) => {
      const next = [...nodes];
      const previousIndex = next.findIndex((node) => node.id === moving.id);
      const currentIndex = next.findIndex((node) => node.id === target.id);
      if (previousIndex < 0 || currentIndex < 0) return nodes;
      moveItemInArray(next, previousIndex, currentIndex);
      return next;
    });
    this.markDirty();
  }

  duplicateSelectedField(): void {
    const field = this.selectedField();
    if (!field || field.kind === 'System') return;
    const duplicate: TemplateFormNode = {
      ...field,
      id: `field-${this.nextFieldId++}`,
      label: `${field.label} Kopyası`,
      isRequired: false,
      options: [...field.options]
    };
    this.formNodes.update((nodes) => {
      const index = nodes.findIndex((node) => node.id === field.id);
      const next = [...nodes];
      next.splice(index + 1, 0, duplicate);
      return next;
    });
    this.selectField(duplicate.id);
    this.markDirty();
  }

  resetSelectedField(): void {
    const field = this.selectedField();
    if (!field) return;
    if (field.kind === 'System') {
      const canonical = defaultSystemNodes(this.projectType())
        .find((node) => node.systemKey === field.systemKey);
      if (canonical) {
        this.updateSelectedField({
          label: canonical.label,
          hint: canonical.hint,
          contentType: canonical.contentType,
          isRequired: canonical.isRequired,
          isActive: canonical.isActive
        });
      }
      this.fieldMenuId.set(null);
      return;
    }
    this.updateSelectedField({
      hint: '',
      isRequired: false,
      isActive: true,
      listName: ['select', 'checklist', 'table', 'formGroup'].includes(field.contentType) ? 'manual' : field.listName,
      options: field.contentType === 'select'
        ? ['Seçenek 1', 'Seçenek 2']
        : field.contentType === 'checklist'
          ? ['Kontrol maddesi 1', 'Kontrol maddesi 2']
          : field.contentType === 'table'
            ? ['Başlık', 'Değer']
            : field.contentType === 'formGroup'
              ? ['Alt Alan 1', 'Alt Alan 2']
            : []
    });
    this.fieldMenuId.set(null);
  }

  removeSelectedField(): void {
    const field = this.selectedField();
    if (!field || !this.canDelete(field)) return;
    this.formNodes.update((nodes) => nodes.filter((node) => node.id !== field.id));
    this.selectedFieldId.set(null);
    this.fieldMenuId.set(null);
    this.markDirty();
  }

  setFieldHint(value: string): void {
    this.updateSelectedField({ hint: value });
  }

  setContentType(value: string): void {
    const field = this.selectedField();
    if (!field || field.kind !== 'Custom') return;
    const listName = ['select', 'checklist', 'table', 'formGroup'].includes(value)
      ? 'manual'
      : value === 'employee'
        ? 'employee'
        : value === 'department'
          ? 'department'
          : null;
    const options = value === 'select'
      ? (field.options.length ? field.options : ['Seçenek 1', 'Seçenek 2'])
      : value === 'checklist'
        ? (field.options.length ? field.options : ['Kontrol maddesi 1', 'Kontrol maddesi 2'])
      : value === 'table'
          ? (field.options.length ? field.options : ['Başlık', 'Değer'])
          : value === 'formGroup'
            ? (field.options.length ? field.options : ['Alt Alan 1', 'Alt Alan 2'])
          : [];
    this.updateSelectedField({ contentType: value, listName, options });
  }

  setListName(value: string): void {
    const field = this.selectedField();
    if (!field || field.contentType !== 'select') return;
    this.updateSelectedField({ listName: value, options: value === 'manual' ? field.options : [] });
  }

  setOptionsText(value: string): void {
    const options = value
      .split(/\r?\n/)
      .map((option) => option.trim())
      .filter((option, index, all) => option && all.findIndex((candidate) =>
        candidate.localeCompare(option, 'tr-TR', { sensitivity: 'accent' }) === 0) === index);
    this.updateSelectedField({ options });
  }

  optionsText(field: TemplateFormNode | null): string {
    return field?.options.join('\n') ?? '';
  }

  setIsActive(value: boolean): void {
    const field = this.selectedField();
    if (!field || this.isActiveLocked(field)) return;
    this.updateSelectedField({ isActive: value, isRequired: value ? field.isRequired : false });
  }

  setIsRequired(value: boolean): void {
    const field = this.selectedField();
    if (!field || field.kind === 'Section' || this.isRequiredLocked(field)) return;
    this.updateSelectedField({ isRequired: value, isActive: value ? true : field.isActive });
  }

  setProjectType(value: string): void {
    this.projectType.set(value);
    const isSimple = value === 'Basit Proje';
    this.formNodes.update((nodes) => {
      const next = nodes.map((node) => {
        if (node.systemKey === 'budget') return { ...node, isActive: !isSimple, isRequired: !isSimple };
        if (node.systemKey === 'secondManager') return { ...node, isActive: !isSimple };
        return node;
      });
      if (!isSimple && !next.some((node) => node.systemKey === 'budget')) {
        next.push(systemNode(
          `system-budget-${this.nextFieldId++}`,
          'budget',
          'Bütçe',
          'currency',
          'Planlanan proje bütçesi',
          true
        ));
      }
      return next;
    });
    this.markDirty();
  }

  isActiveLocked(field: TemplateFormNode): boolean {
    return field.kind === 'System' &&
      (LOCKED_ACTIVE_SYSTEM_KEYS.has(field.systemKey ?? '') ||
       (field.systemKey === 'budget' && this.projectType() !== 'Basit Proje'));
  }

  isRequiredLocked(field: TemplateFormNode): boolean {
    return field.kind === 'System' &&
      (LOCKED_REQUIRED_SYSTEM_KEYS.has(field.systemKey ?? '') ||
       (field.systemKey === 'budget' && this.projectType() !== 'Basit Proje'));
  }

  canDelete(field: TemplateFormNode | null): boolean {
    return Boolean(field && (field.kind !== 'System' || !this.isActiveLocked(field)));
  }

  showPreview(): void {
    this.previewMode.set(true);
    this.fieldMenuId.set(null);
  }

  closePreview(): void {
    this.previewMode.set(false);
  }

  async saveTemplate(): Promise<void> {
    const validationError = this.validateTemplate();
    if (validationError) {
      this.errorMessage.set(validationError);
      return;
    }

    this.errorMessage.set('');
    this.statusMessage.set('');
    this.saving.set(true);
    const request = {
      name: this.templateName().trim(),
      applicableProjectType: this.resolveApplicableProjectType(),
      fields: this.formNodes().map((node): CreateTemplateFieldRequest => ({
        label: node.label.trim(),
        hint: node.hint.trim(),
        contentType: node.contentType,
        listName: node.listName,
        isRequired: node.isRequired,
        isActive: node.isActive,
        kind: node.kind,
        systemKey: node.systemKey,
        options: node.options
      }))
    };

    try {
      const templateId = this.savedTemplateId();
      const saved = templateId
        ? await this.templateApi.update(templateId, request)
        : await this.templateApi.create(request);
      this.savedTemplateId.set(saved.id);
      this.loadTemplate(saved);
      this.dirty.set(false);
      this.saveDialogOpen.set(true);
      window.history.replaceState({}, '', `/projects/templates/${saved.id}`);
    } catch (error) {
      this.errorMessage.set(error instanceof Error ? error.message : 'Şablon kaydedilemedi.');
    } finally {
      this.saving.set(false);
    }
  }

  async shareTemplate(): Promise<void> {
    const templateId = this.savedTemplateId();
    if (!templateId) {
      this.statusMessage.set('Paylaşmadan önce şablonu kaydedin.');
      return;
    }
    const url = `${window.location.origin}/projects/templates/${templateId}`;
    try {
      await navigator.clipboard.writeText(url);
      this.statusMessage.set('Şablon bağlantısı panoya kopyalandı.');
    } catch {
      this.statusMessage.set(url);
    }
  }

  returnToProject(): void {
    this.router.navigate(['/projects/new'], {
      state: this.savedTemplateId() ? { newTemplateId: this.savedTemplateId() } : undefined
    });
  }

  goToSavedTemplate(): void {
    this.saveDialogOpen.set(false);
    this.showPreview();
  }

  cancel(): void {
    this.router.navigate(['/projects/new']);
  }

  goToDepartments(): void {
    this.router.navigate(['/organization']);
  }

  goToTemplates(): void {
    this.router.navigate(['/projects/templates']);
  }

  logout(): void {
    this.authService.logout();
  }

  contentTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      section: 'Bölüm Başlığı',
      text: 'Kısa Metin',
      textarea: 'Uzun Metin',
      number: 'Sayı',
      date: 'Tarih',
      datetime: 'Tarih ve Saat',
      select: 'Listeden Seçim',
      checkbox: 'Checkbox',
      yesNo: 'Evet / Hayır',
      employee: 'Çalışan Seçimi',
      department: 'Departman Seçimi',
      attachment: 'Dosya',
      currency: 'Para',
      table: 'Tablo',
      checklist: 'Kontrol Listesi',
      image: 'Resim',
      signature: 'İmza Alanı',
      formGroup: 'Form Elemanı'
    };
    return labels[type] ?? type;
  }

  fieldHintLabel(field: TemplateFormNode): string {
    if (field.kind === 'Section') return 'Bölüm Açıklaması';
    if (field.contentType === 'attachment' || field.contentType === 'image') return 'Yardımcı Metin';
    if (field.contentType === 'checkbox' || field.contentType === 'yesNo') return 'Alan Açıklaması';
    return 'Placeholder / Açıklama';
  }

  previewDateValue(field: TemplateFormNode): string {
    if (field.systemKey === 'startDate') return '10 Kasım 2024-Pzr';
    if (field.systemKey === 'endDate') return '01 Eylül 2026-Cuma';
    return field.hint || 'Tarih seçiniz';
  }

  fieldPropertyDescription(field: TemplateFormNode): string {
    const descriptions: Record<string, string> = {
      section: 'Bu başlık, altındaki form alanlarını görsel olarak gruplandırır.',
      text: 'Kullanıcıdan tek satırlık kısa bir metin alır.',
      textarea: 'Kullanıcıdan çok satırlı açıklama veya detay alır.',
      number: 'Yalnızca sayısal değer girişine izin verir.',
      date: 'Kullanıcının bir tarih seçmesini sağlar.',
      datetime: 'Kullanıcının tarih ve saat seçmesini sağlar.',
      select: 'Tanımlanan liste seçeneklerinden tek bir değer seçtirir.',
      checkbox: 'Bir koşulun onaylanmasını veya işaretlenmesini sağlar.',
      yesNo: 'Kullanıcıdan Evet ya da Hayır yanıtı alır.',
      employee: 'Çalışan listesinden bir kişi seçtirir.',
      department: 'Departman listesinden bir birim seçtirir.',
      checklist: 'Birden fazla kontrol maddesinin işaretlenmesini sağlar.',
      formGroup: 'Birbiriyle ilişkili alt alanları aynı blokta toplar.',
      table: 'Satır ve sütun yapısında veri girişi sağlar.',
      attachment: 'Kullanıcının forma dosya eklemesini sağlar.',
      image: 'Kullanıcının görsel dosyası eklemesini sağlar.',
      signature: 'İmzalayan kişiye ait bilgiyi toplar.',
      currency: 'Tutar ve para birimi bilgisi toplar.'
    };
    return descriptions[field.contentType] ?? 'Seçilen alanın görünüm ve davranış ayarlarını düzenleyin.';
  }

  boundListLabel(field: TemplateFormNode): string {
    if (field.contentType === 'employee') {
      return field.systemKey === 'manager' || field.systemKey === 'secondManager'
        ? 'Yönetici Listesi'
        : 'Çalışan Listesi';
    }
    if (field.contentType === 'department') return 'Departman Listesi';
    if (field.contentType === 'select') {
      if (field.listName === 'employee') return 'Çalışan Listesi';
      if (field.listName === 'department') return 'Departman Listesi';
      return field.listName === 'manual' ? 'Manuel Liste' : '';
    }
    return '';
  }

  kindLabel(kind: TemplateFieldKind): string {
    return kind === 'System' ? 'Sistem Alanı' : kind === 'Section' ? 'Bölüm' : 'Özel Alan';
  }

  private updateSelectedField(patch: Partial<TemplateFormNode>): void {
    const selectedId = this.selectedFieldId();
    if (!selectedId) return;
    this.formNodes.update((nodes) =>
      nodes.map((node) => node.id === selectedId ? { ...node, ...patch } : node)
    );
    this.markDirty();
  }

  private markDirty(): void {
    this.dirty.set(true);
    this.statusMessage.set('');
  }

  private resolveApplicableProjectType(): BackendProjectType {
    if (this.projectType() === 'Çok Birimli Proje' && this.loadedApplicableProjectType === 'FeasibilityBased') {
      return 'FeasibilityBased';
    }
    return PROJECT_TYPE_TO_BACKEND[this.projectType()] ?? 'MultiUnit';
  }

  private isApplicableSystemNode(node: TemplateFormNode): boolean {
    if (node.kind !== 'System') return true;
    // Birim ve bütçe proje oluşturma akışının kendi alanlarıdır; şablon
    // tasarımında tekrar gösterilmeleri referans formu bozuyor ve aynı veriyi
    // iki kez topluyordu. Eski şablonlarda bulunsalar da tasarım/önizlemede
    // görünmezler.
    if (node.systemKey === 'unit' || node.systemKey === 'budget') return false;
    return true;
  }

  private validateTemplate(): string | null {
    if (!this.templateName().trim()) return 'Şablon adı zorunludur.';
    if (!this.formNodes().length) return 'Şablonda en az bir form elemanı bulunmalıdır.';

    const blank = this.formNodes().find((node) => !node.label.trim());
    if (blank) return 'Tüm form elemanlarının etiketi doldurulmalıdır.';

    const seen = new Set<string>();
    for (const node of this.formNodes()) {
      const key = node.label.trim().toLocaleLowerCase('tr-TR');
      if (seen.has(key)) return `"${node.label}" etiketi birden fazla kez kullanılamaz.`;
      seen.add(key);
      if (node.contentType === 'select' && node.listName === 'manual' && node.options.length === 0) {
        return `"${node.label}" alanı için en az bir liste seçeneği girilmelidir.`;
      }
      if (['checklist', 'table', 'formGroup'].includes(node.contentType) && node.options.length === 0) {
        return `"${node.label}" alanı için en az bir başlık veya madde girilmelidir.`;
      }
    }
    return null;
  }

  private loadTemplate(template: TemplateDto): void {
    this.templateName.set(template.name);
    this.loadedApplicableProjectType = template.applicableProjectType;
    this.projectType.set(BACKEND_TO_PROJECT_TYPE[template.applicableProjectType]);
    const mapped = template.fields
      .sort((left, right) => left.sortOrder - right.sortOrder)
      .map((field) => this.normalizeSystemNode(this.toFormNode(field)));
    const hasSystemFields = mapped.some((field) => field.kind === 'System');
    this.formNodes.set(hasSystemFields ? mapped : [...defaultSystemNodes(this.projectType()), ...mapped]);
    this.selectedFieldId.set(null);
    this.nextFieldId = this.formNodes().length + 1;
  }

  private toFormNode(field: TemplateFieldDto): TemplateFormNode {
    const contentType = this.normalizeLegacyContentType(field.contentType);
    const kind = field.kind ?? (contentType === 'section' ? 'Section' : 'Custom');
    return {
      id: field.id,
      label: field.label,
      hint: field.hint,
      contentType,
      listName: field.listName,
      isRequired: kind === 'Section' ? false : field.isRequired,
      isActive: field.isActive !== false,
      kind,
      systemKey: field.systemKey ?? null,
      options: field.options ?? []
    };
  }

  private normalizeLegacyContentType(type: string): string {
    const legacyTypes: Record<string, string> = {
      'Bölüm': 'section',
      'Normal Yazı': 'text',
      'Paragraf Yazısı': 'textarea',
      'Numaralar': 'number',
      'Tarih Seçici': 'date',
      'Tarih Ve Saat': 'datetime',
      'Açılır Menü': 'select',
      Checkbox: 'checkbox',
      'Evet/Hayır': 'yesNo',
      Profiller: 'employee'
    };
    return legacyTypes[type] ?? type;
  }

  private normalizeSystemNode(node: TemplateFormNode): TemplateFormNode {
    if (node.kind !== 'System' || !node.systemKey) return node;
    const canonical = [
      ...defaultSystemNodes('Çok Birimli Proje'),
      systemNode('system-attachments', 'attachments', 'Dosya Ekleyin', 'attachment', 'Talep, analiz veya destekleyici dokümanlar', false),
      systemNode('system-second-manager', 'secondManager', 'İkinci Proje Yöneticisi', 'employee', 'İsteğe bağlı ikinci yönetici', false)
    ].find((field) => field.systemKey === node.systemKey);
    return canonical
      ? {
          ...node,
          label: node.label.trim() || canonical.label,
          hint: node.hint,
          contentType: canonical.contentType
        }
      : node;
  }

}
