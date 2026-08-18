import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, HostListener, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FileTypeIcon } from '../../../shared/file-type-icon/file-type-icon';
import { Icon, IconName } from '../../../shared/icon/icon';
import { ToastService } from '../../../shared/toast/toast.service';
import { ProjectApiService } from '../data/project-api.service';
import { TaskApiService } from '../data/task-api.service';
import { RagSyncApiService } from '../data/rag-sync-api.service';
import { FeasibilityApiService } from '../data/feasibility-api.service';
import { EmployeeApiService } from '../data/employee-api.service';
import { EmployeeDto } from '../data/employee-api.models';
import { ArchivedTaskDto, KanbanStatus, ProjectDocumentDto, TaskCommentDto, TaskGroupDto } from '../data/task-api.models';
import {
  BackendProjectType,
  DepartmentAssignmentDto,
  ProjectNoteDto,
  ProjectStatus,
  ProjectTemplateFieldValueDto,
  ProjectTimelineDto,
  ProjectTimelineState
} from '../data/project-api.models';
import {
  ApprovalDecision,
  ApprovalStepDto,
  FeasibilityItemDto,
  FeasibilityItemStatus,
  FeasibilityMainGroupDto
} from '../data/feasibility-api.models';
import { AuthService } from '../../../shared/auth/auth.service';
import { AiWorkPackagePanel } from './ai-work-package-panel/ai-work-package-panel';
import { AiSuggestionApiService } from '../data/ai-suggestion-api.service';
import { ProjectGuidePanel } from './project-guide-panel/project-guide-panel';
import { ProjectGuideContext } from './project-guide-panel/project-guide.models';

type DetailTab = 'overview' | 'tasks' | 'ai' | 'documents' | 'feasibility' | 'activity' | 'assistant';

const PANEL_OVERLAY_BREAKPOINT = 1180;

function usesPanelOverlay(): boolean {
  return typeof window !== 'undefined' && window.innerWidth <= PANEL_OVERLAY_BREAKPOINT;
}

interface RailItem {
  icon: IconName;
  label: string;
  active?: boolean;
}

interface ProjectDetailView {
  id: string;
  name: string;
  description: string;
  manager: string;
  secondManager: string | null;
  unit: string;
  type: string;
  rawType: BackendProjectType;
  rawStatus: ProjectStatus;
  statusLabel: string;
  budget: string;
  progress: number;
  deviation: string;
  deviationDays: number;
  start: string;
  end: string;
  startIso: string;
  endIso: string;
  departments: DepartmentAssignmentDto[];
  enabledComponents: string[];
  templateName: string | null;
  templateValues: ProjectTemplateFieldValueDto[];
}

const STATUS_LABELS: Record<ProjectStatus, string> = {
  Draft: 'Taslak',
  Active: 'Proje Devam Ediyor',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal Edildi'
};

const EMPTY_PROJECT_VIEW: ProjectDetailView = {
  id: '',
  name: 'Yükleniyor…',
  description: '',
  manager: '',
  secondManager: null,
  unit: '',
  type: '',
  rawType: 'Simple',
  rawStatus: 'Draft',
  statusLabel: '',
  budget: '',
  progress: 0,
  deviation: '',
  deviationDays: 0,
  start: '',
  end: '',
  startIso: '',
  endIso: '',
  departments: [],
  enabledComponents: [],
  templateName: null,
  templateValues: []
};

interface DetailMessage {
  id: string;
  isNote: boolean;
  author: string;
  unit: string;
  date: string;
  dateRaw: string;
  title?: string;
  paragraphs: string[];
  canEdit: boolean;
  attachments?: ProjectDocumentView[];
}

interface DetailTask {
  id: string;
  title: string;
  assignee: string;
  assigneeEmployeeId: string | null;
  done: boolean;
  status: KanbanStatus;
  depth: number;
  isMainTask: boolean;
  comments: number;
  commentEntries: TaskCommentDto[];
  effortHours?: number;
  dependency?: string;
  dependsOnTaskId: string | null;
  department?: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  startDateUtc: string | null;
  dueDateUtc: string | null;
  isAiGenerated: boolean;
  category: string | null;
  description: string | null;
  completedAtUtc: string | null;
  completedBy: string | null;
}

// Görevler sekmesinin "tek liste, uygulama sırasına göre" görünümü için: her ana görev + kendi alt
// görevleri, hangi TaskGroupDto'ya ait olduğu bilgisiyle birlikte (mevcut grup bazlı aksiyonlar —
// yorum/dosya ekleme, yeniden atama — grupId'yi gerektiriyor, ama grup adı artık ayrı bir bölüm
// başlığı olarak gösterilmiyor).
interface SequencedTask {
  groupId: string;
  task: DetailTask;
  subtasks: DetailTask[];
}

// Henüz onaylanmamış bir AI iş paketi önerisi — Görevler listesinde gerçek bir görev gibi değil, sıraya
// "yer tutan" ayrı bir satır olarak gösterilir (bkz. sequenceNote: LLM'in, projenin gerçek görev
// sırasına bakarak ürettiği "nereye oturur" açıklaması — gerçek bir sıra numarası DEĞİL, sadece okunabilir
// bir gerekçe metni).
interface PendingAiSuggestionView {
  requestId: string;
  itemId: string;
  title: string;
  department: string;
  effortHours: number;
  description: string | null;
  sequenceNote: string | null;
  // Backend'in gerçek görevlerle eşleştirdiği TAM başlık (bkz. PromptBuilder.AppendExistingTasksList) —
  // sequenceNote'un aksine bu, sıralamayı hesaplamak için Görevler ekranında da (bkz. unifiedSequenceRows)
  // kullanılan makine-okur alan.
  insertAfterTaskTitle: string | null;
  // Aynı üretimdeki (requestId) diğer önerilere göre göreli sıra (1'den başlar) — gerçek bir göreve değil,
  // SADECE bu üretimin kardeşlerine göre sırayı ifade eder. insertAfterTaskTitle çözülemediğinde (null ya
  // da reddedilmiş bir kardeşe aitti) bu, aynı üretimin diğer önerileriyle doğru göreli sırada kalmasını
  // sağlar — bkz. unifiedSequenceRows.
  sequenceRank: number | null;
  // insertAfterTaskTitle null olduğunda bunun İKİ farklı anlamını ayırt eder: true ise öneri GERÇEKTEN
  // projenin en başında (1 numaralı görevden bile önce) yapılabilir; false ise nereye oturduğu belirsiz
  // kaldı. Ayrım olmadan ikisi de aynı şekilde (listenin sonuna) düşerdi — bkz. unifiedSequenceRows.
  isAtProjectStart: boolean;
  activityCount: number;
}

// Görevler listesindeki tek bir satır: ya gerçek bir görev, ya da henüz onaylanmamış bir AI önerisi —
// öneri, insertAfterTaskTitle'ı çözümlenebiliyorsa gerçek sıradaki yerine, isAtProjectStart true ise
// listenin en başına, hiçbiri değilse (referans yok/reddedilmiş bir öneriye aitti/proje henüz o görevi
// içermiyor) listenin sonuna eklenir (bkz. unifiedSequenceRows).
type UnifiedSequenceRow =
  | { kind: 'task'; entry: SequencedTask }
  | { kind: 'pending'; pending: PendingAiSuggestionView };

interface DetailTaskGroup {
  id: string;
  title: string;
  subtitle: string;
  tasks: DetailTask[];
  createdAtUtc: string;
}

interface ActivitySegment {
  prefix: string;
  entity: string;
  middle: string;
  status?: string;
  statusTone?: 'success' | 'danger';
}

interface ActivityItem {
  id: string;
  context: string;
  dateLabel: string;
  dateRaw: string;
  segment: ActivitySegment;
  actor: string | null;
  department: string | null;
  groupId: string | null;
}

type TimelineState = 'done' | 'late' | 'active' | 'pending';

// Süreç aşaması bazlı ayrıntı (Fizibilite/Fiyat Karşılaştırma/Onay/Satın Alma) burada KASITLI OLARAK
// gösterilmiyor — bu aşamalara TaskService/FeasibilityService tarafında gerçek veri bağlanmadıkça
// (ör. Fiyat Karşılaştırma ve Satın Alma Süreci için bugün hiçbir kullanıcı akışı yok) satırlar hep
// yer tutucu/placeholder kalıyordu; gerçek bir süreç takibi izlenimi verip aslında hiçbir işlerliği
// olmaması yanıltıcıydı. Bu görünüm artık sadece iş paketi/birim bazlı planlanan tarih ve sapmayı
// (gerçek veri) gösteriyor.
interface TimelineGroupView {
  id: string;
  date: string;
  endDate: string;
  title: string;
  deviation: string;
  state: TimelineState;
}

interface TaskDraft {
  title: string;
  // Real employee id, not free text (see canonical employee picker in project-create-page.ts) — the
  // department is derived from this at save time, never entered independently (see updateTaskDraft's
  // 'assigneeEmployeeId' case and CreateTaskRequest.department in saveTask()).
  assigneeEmployeeId: string;
  isMainTask: boolean;
  dependsOnTaskId: string;
  startDate: string;
  startTime: string;
  endDate: string;
  endTime: string;
  category: string;
  description: string;
  effortHours: string;
}

interface InlineSubtaskDraft {
  title: string;
  assigneeEmployeeId: string;
  effortHours: string;
}

interface TaskArchiveContext {
  groupId: string;
  task: DetailTask;
  childCount: number;
}

interface TaskStatusConfirmation {
  groupId: string;
  task: DetailTask;
  nextStatus: KanbanStatus;
}

interface DocumentDeleteConfirmation {
  document: ProjectDocumentView;
}

interface TaskCommentContext {
  groupId: string;
  taskId: string;
}

const TASK_CATEGORIES = ['İş İsteği', 'Hata/Arıza', 'İyileştirme', 'Diğer'];

export type DocumentKind = 'word' | 'powerpoint' | 'excel' | 'pdf' | 'file' | 'image' | 'video';

export interface ProjectDocumentView {
  id: string;
  noteId: string | null;
  uploadedBy: string | null;
  name: string;
  kind: DocumentKind;
  size: string;
  sizeBytes: number;
  createdAtUtc: string;
}

interface UploadFileView {
  id: string;
  name: string;
  size: string;
  kind: DocumentKind;
  progress: number;
  file: File;
}

function emptyTaskDraft(): TaskDraft {
  const today = new Date().toISOString().slice(0, 10);
  return {
    title: '',
    assigneeEmployeeId: '',
    isMainTask: true,
    dependsOnTaskId: '',
    startDate: today,
    startTime: '09:00',
    endDate: today,
    endTime: '18:00',
    category: '',
    description: '',
    effortHours: ''
  };
}

function emptyInlineSubtaskDraft(): InlineSubtaskDraft {
  return { title: '', assigneeEmployeeId: '', effortHours: '' };
}

interface FeasibilityItemDraft {
  unit: string;
  description: string;
  amount: string;
  currency: string;
}

function emptyFeasibilityItemDraft(): FeasibilityItemDraft {
  return { unit: '', description: '', amount: '', currency: 'TRY' };
}

@Component({
  selector: 'app-project-detail-page',
  standalone: true,
  imports: [CommonModule, FormsModule, Icon, FileTypeIcon, AiWorkPackagePanel, ProjectGuidePanel],
  templateUrl: './project-detail-page.html',
  styleUrl: './project-detail-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly projectApi = inject(ProjectApiService);
  private readonly taskApi = inject(TaskApiService);
  private readonly ragSyncApi = inject(RagSyncApiService);
  private readonly feasibilityApi = inject(FeasibilityApiService);
  // Yalnızca Görevler sekmesinde bekleyen (Pending) önerileri sıraya "yer tutucu" olarak göstermek için —
  // asıl AI onay/red akışı ai-work-package-panel'de, kendi bağımsız fetch'iyle çalışıyor (bkz. o
  // bileşenin kendi AiSuggestionApiService kullanımı). İkisi aynı anda açık değil (sekmeler birbirini
  // devre dışı bırakır), bu yüzden aynı veriye iki ayrı erişim kabul edilebilir bir küçük tekrar.
  private readonly pendingSuggestionsApi = inject(AiSuggestionApiService);
  private readonly employeeApi = inject(EmployeeApiService);
  private readonly authService = inject(AuthService);

  readonly currentUserName = computed(() => (this.authService.currentUser()?.displayName ?? ''));
  readonly canManageProjects = computed(() => this.authService.hasAnyRole(['Admin', 'ProjectManager']));

  readonly allEmployees = signal<EmployeeDto[]>([]);
  readonly taskCategories = TASK_CATEGORIES;
  // Departman artık elle seçilmiyor — seçilen kişinin sisteme kayıtlı gerçek departmanından geliyor.
  readonly taskDraftDepartmentName = computed(() => {
    const employeeId = this.taskDraft().assigneeEmployeeId;
    if (!employeeId) return '';
    return this.allEmployees().find((employee) => employee.id === employeeId)?.departmentName ?? '';
  });

  private readonly projectId = this.route.snapshot.paramMap.get('projectId') ?? '';
  private compactPanelViewport = usesPanelOverlay();

  readonly activeTab = signal<DetailTab>(this.initialTab());
  readonly summaryCollapsed = signal(this.compactPanelViewport);
  readonly timelineCollapsed = signal(this.compactPanelViewport);
  readonly railCompact = signal(false);
  readonly profileOpen = signal(false);
  readonly commentDraft = signal('');
  readonly openTaskMenuId = signal<string | null>(null);
  // Only Admin/ProjectManager can reassign a task — see canManageProjects and Policies.CanManageProjects
  // on the backend's Reassign endpoint.
  readonly reassignTaskContext = signal<{ groupId: string; taskId: string } | null>(null);
  readonly reassignEmployeeId = signal('');
  readonly savingReassign = signal(false);
  readonly taskDialogOpen = signal(false);
  readonly taskDialogMode = signal<'create' | 'edit'>('create');
  readonly editingTaskId = signal<string | null>(null);
  readonly uploadDialogOpen = signal(false);
  readonly taskFormError = signal('');
  readonly taskDraft = signal<TaskDraft>(emptyTaskDraft());
  readonly savingTask = signal(false);
  readonly inlineSubtaskContext = signal<{ groupId: string; parentTaskId: string } | null>(null);
  readonly inlineSubtaskDraft = signal<InlineSubtaskDraft>(emptyInlineSubtaskDraft());
  readonly inlineSubtaskError = signal('');
  readonly savingInlineSubtask = signal(false);
  readonly archiveTaskContext = signal<TaskArchiveContext | null>(null);
  readonly savingArchive = signal(false);
  readonly archivedTasks = signal<ArchivedTaskDto[]>([]);
  readonly archiveListOpen = signal(false);
  readonly restoringTaskId = signal<string | null>(null);
  readonly taskCommentContext = signal<TaskCommentContext | null>(null);
  readonly taskCommentDraft = signal('');
  readonly savingTaskComment = signal(false);
  readonly savingUpload = signal(false);
  private taskDialogGroupId: string | null = null;
  private descriptionMessage: DetailMessage | null = null;
  readonly sendingComment = signal(false);
  readonly commentAttachments = signal<File[]>([]);
  // Auto-grow target for the description composer — reset to its single-line height after a
  // successful send, since clearing commentDraft() doesn't undo the inline height JS set while typing.
  private readonly composerTextarea = viewChild<ElementRef<HTMLTextAreaElement>>('composerInput');
  readonly editingNoteId = signal<string | null>(null);
  readonly noteEditDraft = signal('');
  readonly savingNoteEdit = signal(false);

  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly project = signal<ProjectDetailView>(EMPTY_PROJECT_VIEW);
  // Same convention as the list/card views: behind schedule = red, ahead = green, on track = neutral.
  readonly deviationTone = computed(() => {
    const days = this.project().deviationDays;
    return days < 0 ? 'bad' : days > 0 ? 'good' : 'neutral';
  });

  private static readonly NON_EDITABLE_CONTENT_TYPES = new Set(['attachment', 'image', 'signature', 'section']);
  readonly editingTemplateValues = signal(false);
  readonly templateValueDrafts = signal<Record<string, string>>({});
  readonly templateValuesError = signal('');
  readonly savingTemplateValues = signal(false);
  readonly editableTemplateFields = computed(() =>
    this.project().templateValues.filter((field) => !ProjectDetailPage.NON_EDITABLE_CONTENT_TYPES.has(field.contentType))
  );

  isTemplateFieldEditable(field: ProjectTemplateFieldValueDto): boolean {
    return !ProjectDetailPage.NON_EDITABLE_CONTENT_TYPES.has(field.contentType);
  }

  readonly uploadFileName = signal('');

  readonly feasibilityGroups = signal<FeasibilityMainGroupDto[]>([]);
  readonly timeline = signal<ProjectTimelineDto | null>(null);
  readonly timelineWarnings = signal<string[]>([]);
  readonly timelineStartLabel = computed(() => {
    const startDate = this.timeline()?.startDate || this.project().startIso;
    return startDate ? this.formatTimelineMoment(startDate) : '';
  });
  readonly timelineWorkPackageOptions = computed(() => {
    const departments = this.project().departments;
    if (departments.length) {
      return departments.map((department) => ({
        id: department.id,
        label: department.title || department.departmentName
      }));
    }
    return this.project().id
      ? [{ id: this.project().id, label: this.project().unit || this.project().name }]
      : [];
  });
  readonly timelineGroups = computed<TimelineGroupView[]>(() => {
    return (this.timeline()?.workPackages ?? []).map((workPackage) => ({
      id: workPackage.id,
      date: this.formatTimelineMoment(workPackage.startDate),
      endDate: workPackage.endDate,
      title: workPackage.title,
      deviation: `${workPackage.deviationDays} Gün`,
      state: this.timelineState(workPackage.state, workPackage.deviationDays)
    }));
  });
  readonly feasibilityTotalRequested = computed(() =>
    this.formatCurrencyAmount(this.feasibilityGroups().reduce((sum, g) => sum + g.totalRequestedAmount, 0))
  );
  readonly feasibilityTotalApproved = computed(() =>
    this.formatCurrencyAmount(this.feasibilityGroups().reduce((sum, g) => sum + g.totalApprovedAmount, 0))
  );
  readonly feasibilityFormError = signal('');
  readonly savingFeasibility = signal(false);
  readonly mainGroupDialogOpen = signal(false);
  readonly newMainGroupName = signal('');
  readonly newMainGroupWorkPackageId = signal('');
  readonly itemDialogOpen = signal(false);
  readonly itemDraft = signal<FeasibilityItemDraft>(emptyFeasibilityItemDraft());
  private itemDialogGroupId: string | null = null;
  readonly submitDialogOpen = signal(false);
  readonly approverNames = signal<string[]>(['']);
  private submitContext: { mainGroupId: string; itemId: string } | null = null;
  readonly decideDialogOpen = signal(false);
  readonly decideApprove = signal(true);
  readonly decideComment = signal('');
  private decideContext: { mainGroupId: string; itemId: string; approverName: string } | null = null;

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

  private static readonly BASE_TABS: Array<{ id: DetailTab; label: string; icon: IconName }> = [
    { id: 'overview', label: 'Proje Açıklaması', icon: 'file-text' },
    { id: 'tasks', label: 'Görevler', icon: 'tasks' },
    { id: 'ai', label: 'AI İş Paketi Oluştur', icon: 'ai' },
    { id: 'assistant', label: 'Proje Rehberi', icon: 'comment' },
    { id: 'documents', label: 'Dokümanlar', icon: 'projects' },
    { id: 'feasibility', label: 'Fizibilite', icon: 'wallet' },
    { id: 'activity', label: 'Akış', icon: 'hash' }
  ];

  private static readonly TAB_COMPONENT_KEY: Partial<Record<DetailTab, string>> = {
    tasks: 'tasks',
    assistant: 'chat',
    ai: 'ai',
    documents: 'documents',
    activity: 'flow'
  };

  readonly tabs = computed(() => {
    const enabled = this.project().enabledComponents;
    return ProjectDetailPage.BASE_TABS.filter((tab) => {
      if (tab.id === 'feasibility') {
        return this.project().rawType === 'FeasibilityBased';
      }
      const componentKey = ProjectDetailPage.TAB_COMPONENT_KEY[tab.id];
      // overview always shows; other tabs only if the project was created with that component enabled
      // (or the project predates component tracking and the list is empty, in which case default to showing everything).
      return !componentKey || !enabled.length || enabled.includes(componentKey);
    });
  });

  readonly messages = signal<DetailMessage[]>([]);
  readonly taskGroups = signal<DetailTaskGroup[]>([]);
  readonly taskCommentTask = computed<DetailTask | null>(() => {
    const context = this.taskCommentContext();
    if (!context) return null;
    return this.taskGroups()
      .find((group) => group.id === context.groupId)?.tasks
      .find((task) => task.id === context.taskId) ?? null;
  });
  readonly documents = signal<ProjectDocumentView[]>([]);
  readonly uploadFiles = signal<UploadFileView[]>([]);
  readonly documentDeleteConfirmation = signal<DocumentDeleteConfirmation | null>(null);
  readonly deletingDocumentId = signal<string | null>(null);

  readonly taskListFilter = signal<'all' | KanbanStatus | 'ai' | 'unassigned'>('all');
  readonly expandedMainTaskIds = signal<Set<string>>(new Set());
  readonly allTaskRowsExpanded = signal(false);
  readonly openStatusMenuId = signal<string | null>(null);
  readonly savingStatusTaskId = signal<string | null>(null);
  readonly statusConfirmation = signal<TaskStatusConfirmation | null>(null);

  // "Tek liste, uygulama sırasına göre": tüm gruplardaki ana görevler tek bir sıraya dizilir (gerçek
  // başlangıç tarihine göre — henüz tarihi olmayanlar sona), her birinin kendi alt görevleri
  // (dependsOnTaskId ile eşleşen) altında toplanır. Grup adı artık ayrı bir bölüm başlığı olarak
  // gösterilmiyor, ama grupId aksiyonlar (yorum/dosya ekle, yeniden ata) için saklanıyor.
  readonly sequencedTasks = computed<SequencedTask[]>(() => {
    const entries: SequencedTask[] = [];
    for (const group of this.taskGroups()) {
      for (const task of group.tasks) {
        if (!task.isMainTask) continue;
        entries.push({
          groupId: group.id,
          task,
          subtasks: group.tasks.filter((t) => !t.isMainTask && t.dependsOnTaskId === task.id)
        });
      }
    }
    return entries.sort((a, b) => {
      const left = a.task.startDateUtc;
      const right = b.task.startDateUtc;
      if (left && right) return left.localeCompare(right);
      if (left) return -1;
      if (right) return 1;
      return a.task.createdAtUtc.localeCompare(b.task.createdAtUtc);
    });
  });

  readonly taskStatusCounts = computed(() => {
    const all = this.sequencedTasks();
    return {
      all: all.length,
      InProgress: all.filter((e) => e.task.status === 'InProgress').length,
      Todo: all.filter((e) => e.task.status === 'Todo').length,
      Done: all.filter((e) => e.task.status === 'Done').length,
      ai: all.filter((e) => e.task.isAiGenerated).length,
      unassigned: all.filter((e) => this.isTaskUnassigned(e.task) || e.subtasks.some((task) => this.isTaskUnassigned(task))).length
    };
  });

  readonly filteredSequencedTasks = computed(() => {
    const filter = this.taskListFilter();
    if (filter === 'all') return this.sequencedTasks();
    if (filter === 'ai') return this.sequencedTasks().filter((e) => e.task.isAiGenerated);
    if (filter === 'unassigned') {
      return this.sequencedTasks().filter((e) => this.isTaskUnassigned(e.task) || e.subtasks.some((task) => this.isTaskUnassigned(task)));
    }
    return this.sequencedTasks().filter((e) => e.task.status === filter);
  });

  readonly taskSubtaskTotal = computed(() =>
    this.sequencedTasks().reduce((total, e) => total + e.subtasks.length, 0)
  );

  readonly pendingAiSuggestions = signal<PendingAiSuggestionView[]>([]);

  // Bekleyen AI önerilerini listenin sonuna değil, insertAfterTaskTitle'ın işaret ettiği gerçek görevin
  // hemen ardına yerleştirir — böylece "onaylarsam kaçıncı sıraya oturur" onaydan ÖNCE görülebilir.
  // Eşleşme, o an FİLTRELENMİŞ görev kümesi içinde aranır (filtre değişince numaralar zaten değişiyor —
  // bu satırlar da aynı mantığa uyar). insertAfterTaskTitle null olan bir öneri iki gruptan birine
  // girer: isAtProjectStart true ise listenin EN BAŞINA (1 numaralı görevden bile önce) konur — model
  // bunun gerçekten projenin başında yapılabileceğini söylüyor demektir; isAtProjectStart false ise
  // (referans yok/reddedilmiş bir kardeş öneriye aitti/o görev şu an filtreyle gizli) öneri listenin
  // SONUNA düşer — sahte bir sıra numarası uydurmak yerine bu daha dürüst bir geri dönüş.
  //
  // Aynı anchor'a (ya da aynı gruba) düşen öneriler arasındaki sırayı ham API dizisi sırasına değil,
  // sequenceRank'a göre kurar: LLM aynı üretimdeki önerileri A→B→C şeklinde birbirine (kardeş başlıkla)
  // bağlamaya çalışsa bile insertAfterTaskTitle artık SADECE gerçek görevlere işaret edebiliyor — bu
  // yüzden aynı üretimin diğer önerileri de aynı gruba (başlangıç ya da sona düşenler) toplanabiliyor.
  // O gruptaki göreli sırayı, dizi sırası (LLM'in ürettiği sıra her zaman mantıksal sırayla örtüşmeyebilir)
  // yerine sequenceRank belirler; biri reddedilse bile kalanlar kendi rank'ıyla doğru sırada kalır.
  readonly unifiedSequenceRows = computed<UnifiedSequenceRow[]>(() => {
    const tasks = this.filteredSequencedTasks();
    const filter = this.taskListFilter();
    const showPending = filter === 'all' || filter === 'ai';
    const pendingList = showPending ? this.pendingAiSuggestions() : [];

    const pendingByAnchorTaskId = new Map<string, PendingAiSuggestionView[]>();
    const startPending: PendingAiSuggestionView[] = [];
    const trailingPending: PendingAiSuggestionView[] = [];
    for (const pending of pendingList) {
      const anchor = pending.insertAfterTaskTitle
        ? tasks.find((e) => e.task.title.localeCompare(pending.insertAfterTaskTitle!, 'tr-TR', { sensitivity: 'base' }) === 0)
        : undefined;
      if (anchor) {
        const bucket = pendingByAnchorTaskId.get(anchor.task.id) ?? [];
        bucket.push(pending);
        pendingByAnchorTaskId.set(anchor.task.id, bucket);
      } else if (pending.isAtProjectStart) {
        startPending.push(pending);
      } else {
        trailingPending.push(pending);
      }
    }

    const byRankThenOriginalOrder = (list: PendingAiSuggestionView[]): PendingAiSuggestionView[] =>
      list
        .map((pending, index) => ({ pending, index }))
        .sort((a, b) => {
          const rankA = a.pending.sequenceRank ?? Number.POSITIVE_INFINITY;
          const rankB = b.pending.sequenceRank ?? Number.POSITIVE_INFINITY;
          return rankA !== rankB ? rankA - rankB : a.index - b.index;
        })
        .map((entry) => entry.pending);

    const rows: UnifiedSequenceRow[] = [];
    for (const pending of byRankThenOriginalOrder(startPending)) {
      rows.push({ kind: 'pending', pending });
    }
    for (const entry of tasks) {
      rows.push({ kind: 'task', entry });
      for (const pending of byRankThenOriginalOrder(pendingByAnchorTaskId.get(entry.task.id) ?? [])) {
        rows.push({ kind: 'pending', pending });
      }
    }
    for (const pending of byRankThenOriginalOrder(trailingPending)) {
      rows.push({ kind: 'pending', pending });
    }

    return rows;
  });

  private async loadPendingAiSuggestions(): Promise<void> {
    try {
      const aiRequests = await this.pendingSuggestionsApi.listByProject(this.projectId);
      const pending: PendingAiSuggestionView[] = [];
      for (const request of aiRequests) {
        for (const item of request.items) {
          if (item.decision === 'Pending') {
            pending.push({
              requestId: request.id,
              itemId: item.id,
              title: item.title,
              department: item.department,
              effortHours: item.effortHours,
              description: item.description,
              sequenceNote: item.sequenceNote,
              insertAfterTaskTitle: item.insertAfterTaskTitle,
              sequenceRank: item.sequenceRank,
              isAtProjectStart: item.isAtProjectStart,
              activityCount: item.activities.length
            });
          }
        }
      }
      this.pendingAiSuggestions.set(pending);
    } catch {
      // Non-fatal — AIGatewayService kısa süreliğine erişilemeyebilir; Görevler listesi öneri
      // olmadan da gösterilmeye devam eder.
    }
  }

  // Notes and documents load independently (see ngOnInit's Promise.all) — this join is a computed,
  // not assembled once in loadProject, so attachments show up correctly whichever finishes loading last.
  // The department shown next to each message is likewise resolved here rather than baked in at build
  // time: ProjectNoteDto only carries an author name (see ProjectDtos.cs), not a department, so every
  // message used to fall back to the PROJECT's own unit — showing e.g. "BT Departmanı" next to Admin
  // even though Admin has no department at all (DepartmentId is null in the seed data). Resolving each
  // author's real department from the employee directory fixes that, and stays correct whichever of
  // loadProject()/loadEmployees() finishes first.
  readonly overviewMessages = computed<DetailMessage[]>(() => {
    const docs = this.documents();
    return this.messages().map((message) => ({
      ...message,
      unit: this.resolveAuthorUnit(message.author),
      attachments: message.isNote ? docs.filter((doc) => doc.noteId === message.id) : []
    }));
  });

  readonly projectGuideContext = computed<ProjectGuideContext>(() => ({
    projectId: this.project().id,
    projectName: this.project().name,
    description: this.project().description,
    notes: this.overviewMessages()
      .filter((message) => message.isNote)
      .map((message) => ({
        author: message.author,
        text: message.paragraphs.join('\n'),
        date: message.date
      })),
    documents: this.documents().map((document) => ({
      name: document.name,
      kind: document.kind,
      size: document.size,
      uploadedBy: document.uploadedBy
    })),
    tasks: this.taskGroups().flatMap((group) => group.tasks.map((task) => ({
      title: task.title,
      description: task.description,
      status: task.status,
      assignee: task.assignee,
      department: task.department ?? null,
      effortHours: task.effortHours ?? null,
      dueDateUtc: task.dueDateUtc,
      groupTitle: group.title,
      isMainTask: task.isMainTask
    })))
  }));

  // '' (not the project's unit) when the author has no department on file — e.g. the Admin seed account
  // has DepartmentId = null — so the UI shows nothing rather than a fabricated, misleading department.
  private resolveAuthorUnit(authorName: string): string {
    return this.allEmployees().find((employee) => employee.displayName === authorName)?.departmentName ?? '';
  }

  readonly activityFeed = computed<ActivityItem[]>(() => {
    const items: ActivityItem[] = [];

    for (const message of this.messages()) {
      if (!message.isNote) continue;
      items.push({
        id: `note-${message.id}`,
        context: 'Proje Açıklaması',
        dateLabel: message.date,
        dateRaw: message.dateRaw,
        segment: { prefix: '', entity: message.author, middle: ' proje açıklamasına not ekledi.' },
        actor: message.author,
        department: null,
        groupId: null
      });
    }

    for (const group of this.taskGroups()) {
      for (const task of group.tasks) {
        items.push({
          id: `task-created-${task.id}`,
          context: group.title,
          dateLabel: this.formatDateTime(task.createdAtUtc),
          dateRaw: task.createdAtUtc,
          segment: { prefix: '', entity: task.title, middle: ` görevi ${task.assignee} kişisine atandı.` },
          actor: task.assignee,
          department: task.department ?? null,
          groupId: group.id
        });

        if (task.done && task.completedAtUtc) {
          items.push({
            id: `task-done-${task.id}`,
            context: group.title,
            dateLabel: this.formatDateTime(task.completedAtUtc),
            dateRaw: task.completedAtUtc,
            segment: {
              prefix: `${task.completedBy || task.assignee} tarafından `,
              entity: task.title,
              middle: ' görevi ',
              status: 'Tamamlandı',
              statusTone: 'success'
            },
            actor: task.assignee,
            department: task.department ?? null,
            groupId: group.id
          });
        }

        for (const comment of task.commentEntries) {
          items.push({
            id: `comment-${comment.id}`,
            context: group.title,
            dateLabel: this.formatDateTime(comment.createdAtUtc),
            dateRaw: comment.createdAtUtc,
            // Show the actual comment text (bold), not just a generic "yorum ekledi" filler — this is
            // also how task-reassignment audit entries (see saveReassign) become traceable here, since
            // those are recorded as ordinary comments rather than a separate history table.
            segment: { prefix: `${comment.author} — ${task.title}: `, entity: comment.text, middle: '' },
            actor: comment.author,
            department: task.department ?? null,
            groupId: group.id
          });
        }
      }
    }

    for (const document of this.documents()) {
      items.push({
        id: `document-${document.id}`,
        context: 'Dokümanlar',
        dateLabel: this.formatDateTime(document.createdAtUtc),
        dateRaw: document.createdAtUtc,
        segment: {
          prefix: document.uploadedBy ? `${document.uploadedBy} tarafından ` : '',
          entity: document.name,
          middle: ' dosyası dökümanlara eklendi.'
        },
        actor: document.uploadedBy,
        department: null,
        groupId: null
      });
    }

    for (const group of this.feasibilityGroups()) {
      for (const item of group.items) {
        for (const step of item.steps) {
          if (step.decision === 'Pending' || !step.decidedAtUtc) continue;
          items.push({
            id: `step-${step.id}`,
            context: group.name,
            dateLabel: this.formatDateTime(step.decidedAtUtc),
            dateRaw: step.decidedAtUtc,
            segment: {
              prefix: `${step.approverName} `,
              entity: group.name,
              middle: ' kalemini ',
              status: step.decision === 'Approved' ? 'Onayladı' : 'Reddetti',
              statusTone: step.decision === 'Approved' ? 'success' : 'danger'
            },
            actor: step.approverName,
            department: null,
            groupId: null
          });
        }
      }
    }

    for (const group of this.timelineGroups()) {
      if (group.state !== 'late') continue;
      const departmentEndDate = group.endDate;
      if (!departmentEndDate) continue;
      items.push({
        id: `deviation-${group.id}`,
        context: 'Sapma Nedeni Oluştu',
        dateLabel: this.formatDate(departmentEndDate),
        dateRaw: departmentEndDate,
        segment: {
          prefix: '',
          entity: group.title,
          middle: ' görevini zamanında tamamlayamadı — ',
          status: group.deviation,
          statusTone: 'danger'
        },
        actor: null,
        department: null,
        groupId: null
      });
    }

    return items.sort((left, right) => right.dateRaw.localeCompare(left.dateRaw));
  });

  async ngOnInit(): Promise<void> {
    if (!this.projectId) {
      this.loadError.set('Proje kimliği bulunamadı.');
      this.loading.set(false);
      return;
    }

    await Promise.all([
      this.loadProject(),
      this.loadTaskGroups(),
      this.loadDocuments(),
      this.loadFeasibilityGroups(),
      this.loadEmployees(),
      this.loadPendingAiSuggestions()
    ]);
    await this.loadTimeline();

    // Self-heals drift on every visit — e.g. if a task or feasibility item changed while this page
    // wasn't open, the stored progress/deviation catch up as soon as someone opens the project again.
    await this.syncProgress();
  }

  private async loadEmployees(): Promise<void> {
    try {
      this.allEmployees.set(await this.employeeApi.list());
    } catch {
      // Non-fatal — scope filtering by department just falls back to showing everything.
    }
  }

  private async loadProject(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const dto = await this.projectApi.getById(this.projectId);
      this.project.set({
        id: dto.id,
        name: dto.name,
        description: dto.description,
        manager: dto.managerName,
        secondManager: dto.secondManagerName,
        unit: dto.unit,
        type: this.typeLabel(dto.type),
        rawType: dto.type,
        rawStatus: dto.status,
        statusLabel: STATUS_LABELS[dto.status],
        budget: `${new Intl.NumberFormat('tr-TR').format(dto.budget)} ${dto.currency === 'TRY' ? '₺' : dto.currency}`,
        progress: dto.progressPercent,
        deviation: `${dto.deviationDays > 0 ? '+' : ''}${dto.deviationDays} Gün`,
        deviationDays: dto.deviationDays,
        start: this.formatDate(dto.startDate),
        end: this.formatDate(dto.endDate),
        startIso: dto.startDate,
        endIso: dto.endDate,
        departments: dto.departments,
        enabledComponents: dto.enabledComponents,
        templateName: dto.templateName,
        templateValues: dto.templateValues ?? []
      });
      this.descriptionMessage = dto.description
        ? {
            id: 'description',
            isNote: false,
            author: dto.managerName,
            unit: dto.unit,
            date: this.formatDate(dto.startDate),
            dateRaw: dto.startDate,
            paragraphs: [dto.description],
            canEdit: false
          }
        : null;
      this.messages.set(this.buildMessages(dto.notes, dto.unit));
    } catch (error) {
      this.loadError.set(error instanceof Error ? error.message : 'Proje yüklenemedi.');
    } finally {
      this.loading.set(false);
    }
  }

  private typeLabel(type: string): string {
    return type === 'Simple' ? 'Basit' : type === 'MultiUnit' ? 'Çoklu Birimli' : 'Fizibiliteye Bağlı';
  }

  private formatDate(isoDate: string): string {
    return new Intl.DateTimeFormat('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(isoDate));
  }

  private formatTimelineMoment(isoDate: string): string {
    // Project/work-package dates are DateOnly values today. The reference layout reserves a time
    // slot, so 12:00 is a presentation default until time planning becomes part of project creation.
    return `${this.formatDate(isoDate)} 12:00`;
  }

  formatDateTime(isoDate: string): string {
    return new Intl.DateTimeFormat('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(new Date(isoDate));
  }

  formatTemplateValue(field: ProjectTemplateFieldValueDto): string {
    if (field.contentType === 'checkbox') {
      return field.value?.toLowerCase() === 'true' ? 'Evet' : 'Hayır';
    }
    if (field.contentType === 'checklist' && field.value) {
      try {
        const items = JSON.parse(field.value);
        return Array.isArray(items) && items.length ? items.join(' • ') : '—';
      } catch {
        return field.value;
      }
    }
    if (field.contentType === 'table' && field.value) {
      try {
        const cells = JSON.parse(field.value) as Record<string, string>;
        const values = Object.entries(cells)
          .filter(([, value]) => value?.trim())
          .sort(([left], [right]) => left.localeCompare(right, undefined, { numeric: true }))
          .map(([, value]) => value.trim());
        return values.length ? values.join(' | ') : '—';
      } catch {
        return field.value;
      }
    }
    if (field.contentType === 'formGroup' && field.value) {
      try {
        const values = JSON.parse(field.value) as Record<string, string>;
        const entries = Object.entries(values)
          .filter(([, value]) => value?.trim())
          .map(([label, value]) => `${label}: ${value.trim()}`);
        return entries.length ? entries.join(' | ') : '—';
      } catch {
        return field.value;
      }
    }
    if (field.contentType === 'datetime' && field.value) {
      return new Intl.DateTimeFormat('tr-TR', {
        dateStyle: 'medium',
        timeStyle: 'short'
      }).format(new Date(field.value));
    }
    if (field.contentType === 'date' && field.value) {
      return this.formatDate(field.value);
    }
    return field.value || '—';
  }

  startEditTemplateValues(): void {
    const drafts: Record<string, string> = {};
    for (const field of this.editableTemplateFields()) {
      drafts[field.templateFieldId] = field.value ?? '';
    }
    this.templateValueDrafts.set(drafts);
    this.templateValuesError.set('');
    this.editingTemplateValues.set(true);
  }

  cancelEditTemplateValues(): void {
    this.editingTemplateValues.set(false);
    this.templateValuesError.set('');
  }

  getTemplateValueDraft(fieldId: string): string {
    return this.templateValueDrafts()[fieldId] ?? '';
  }

  updateTemplateValueDraft(fieldId: string, value: string): void {
    this.templateValueDrafts.update((values) => ({ ...values, [fieldId]: value }));
  }

  isChecklistDraftChecked(fieldId: string, option: string): boolean {
    return this.readChecklistDraft(fieldId).includes(option);
  }

  toggleChecklistDraftItem(fieldId: string, option: string, checked: boolean): void {
    const selected = new Set(this.readChecklistDraft(fieldId));
    checked ? selected.add(option) : selected.delete(option);
    this.updateTemplateValueDraft(fieldId, JSON.stringify([...selected]));
  }

  getTableDraftCell(fieldId: string, option: string): string {
    return this.readTableDraft(fieldId)[option] ?? '';
  }

  updateTableDraftCell(fieldId: string, option: string, value: string): void {
    const table = this.readTableDraft(fieldId);
    table[option] = value;
    this.updateTemplateValueDraft(fieldId, JSON.stringify(table));
  }

  async saveTemplateValues(): Promise<void> {
    if (this.savingTemplateValues()) {
      return;
    }

    const missing = this.editableTemplateFields().find((field) => {
      if (!field.isRequired) {
        return false;
      }
      if (field.contentType === 'checklist') {
        return this.readChecklistDraft(field.templateFieldId).length === 0;
      }
      if (field.contentType === 'table' || field.contentType === 'formGroup') {
        return !Object.values(this.readTableDraft(field.templateFieldId)).some((cell) => cell.trim());
      }
      const value = this.getTemplateValueDraft(field.templateFieldId);
      return !value || (field.contentType === 'checkbox' && value !== 'true');
    });
    if (missing) {
      this.templateValuesError.set(`"${missing.label}" alanı zorunludur.`);
      return;
    }

    this.templateValuesError.set('');
    this.savingTemplateValues.set(true);
    try {
      const dto = await this.projectApi.updateTemplateValues(this.projectId, {
        values: this.editableTemplateFields().map((field) => ({
          fieldId: field.templateFieldId,
          value: this.getTemplateValueDraft(field.templateFieldId)
        }))
      });
      this.project.update((view) => ({ ...view, templateValues: dto.templateValues ?? [] }));
      this.editingTemplateValues.set(false);
      this.toastService.success('Şablon alanları güncellendi.');
    } catch (error) {
      this.templateValuesError.set(error instanceof Error ? error.message : 'Şablon alanları güncellenemedi.');
    } finally {
      this.savingTemplateValues.set(false);
    }
  }

  private readChecklistDraft(fieldId: string): string[] {
    const value = this.getTemplateValueDraft(fieldId);
    if (!value) {
      return [];
    }
    try {
      const parsed = JSON.parse(value);
      return Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === 'string') : [];
    } catch {
      return [];
    }
  }

  private readTableDraft(fieldId: string): Record<string, string> {
    const value = this.getTemplateValueDraft(fieldId);
    if (!value) {
      return {};
    }
    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? (parsed as Record<string, string>) : {};
    } catch {
      return {};
    }
  }

  private buildMessages(notes: ProjectNoteDto[], unit: string): DetailMessage[] {
    const noteMessages = notes.map((note) => this.toDetailMessage(note, unit));
    return this.descriptionMessage ? [this.descriptionMessage, ...noteMessages] : noteMessages;
  }

  private toDetailMessage(note: ProjectNoteDto, unit: string): DetailMessage {
    return {
      id: note.id,
      isNote: true,
      author: note.author,
      unit,
      date: this.formatDateTime(note.createdAtUtc),
      dateRaw: note.createdAtUtc,
      paragraphs: [note.text],
      canEdit: note.author === this.currentUserName()
    };
  }

  private async loadTaskGroups(): Promise<void> {
    try {
      const groups = await this.taskApi.listByProject(this.projectId);
      this.taskGroups.set(groups.map((g) => this.toDetailTaskGroup(g)));
      await this.loadArchivedTasks();
    } catch {
      // Non-fatal — the tasks tab will just show no groups yet.
    }
  }

  private async loadTimeline(): Promise<void> {
    try {
      const timeline = await this.projectApi.getTimeline(this.projectId);
      this.timeline.set(timeline);
      this.timelineWarnings.set(timeline.warnings);
    } catch {
      this.timeline.set(null);
      this.timelineWarnings.set(['Proje zaman çizelgesi şu anda yüklenemedi.']);
    }
  }

  private async refreshTasksAndTimeline(): Promise<void> {
    await this.loadTaskGroups();
    await this.loadTimeline();
  }

  private toDetailTaskGroup(group: TaskGroupDto): DetailTaskGroup {
    const titleById = new Map(group.tasks.map((t) => [t.id, t.title]));
    return {
      id: group.id,
      title: group.title,
      subtitle: group.subtitle,
      createdAtUtc: group.createdAtUtc,
      tasks: group.tasks.map((t) => ({
        id: t.id,
        title: t.title,
        assignee: t.assigneeName,
        assigneeEmployeeId: t.assigneeEmployeeId,
        done: t.status === 'Done',
        status: t.status,
        depth: t.depth,
        isMainTask: t.isMainTask,
        comments: t.comments.length,
        commentEntries: t.comments,
        effortHours: t.effortHours ?? undefined,
        dependency: t.dependsOnTaskId ? titleById.get(t.dependsOnTaskId) : undefined,
        dependsOnTaskId: t.dependsOnTaskId,
        department: t.department ?? undefined,
        createdAtUtc: t.createdAtUtc,
        updatedAtUtc: t.updatedAtUtc,
        startDateUtc: t.startDateUtc,
        dueDateUtc: t.dueDateUtc,
        isAiGenerated: t.isAiGenerated,
        category: t.category,
        description: t.description,
        completedAtUtc: t.completedAtUtc,
        completedBy: t.completedBy
      }))
    };
  }

  private timelineState(state: ProjectTimelineState, deviationDays: number): TimelineState {
    if (state === 'Completed') return 'done';
    if (state === 'Blocked' || deviationDays < 0) return 'late';
    if (state === 'Active') return 'active';
    return 'pending';
  }

  private async loadDocuments(): Promise<void> {
    try {
      const docs = await this.taskApi.listDocuments(this.projectId);
      this.documents.set(docs.map((d) => ({
        id: d.id,
        noteId: d.noteId,
        uploadedBy: d.uploadedBy,
        name: d.name,
        kind: d.kind.toLowerCase() as DocumentKind,
        size: this.formatFileSize(d.sizeBytes),
        sizeBytes: d.sizeBytes,
        createdAtUtc: d.createdAtUtc
      })));
    } catch {
      // Non-fatal.
    }
  }

  private async loadFeasibilityGroups(): Promise<void> {
    try {
      this.feasibilityGroups.set(await this.feasibilityApi.listByProject(this.projectId));
    } catch {
      // Non-fatal — only relevant for FeasibilityBased projects, and only once FeasibilityService responds.
    }
  }

  // İlerleme/sapma artık backend'de hesaplanıyor (bkz. ProjectService ProjectProgressCalculator) —
  // TaskService/FeasibilityService'te bir şey değiştiğinde olay tabanlı olarak kendiliğinden güncelleniyor
  // (ProjectProgressInputsChangedEvent). Bu çağrı sadece kullanıcının az önceki işleminden (görev
  // işaretleme, fizibilite kararı) hemen sonra ekranda anlık doğru sayıyı görebilmek için — o async
  // event'i beklemeden backend'e "şimdi yeniden hesapla" der ve sonucu okur. Sessiz çalışır: az önceki
  // asıl işlem zaten kendi başarı/hata bildirimini gösterdi; burada başarısız olursa ekstra bir hata
  // mesajı göstermeye gerek yok, olay tabanlı güncelleme (ya da gece toplu yeniden hesaplama) zaten
  // kendini düzeltir.
  private async syncProgress(): Promise<void> {
    if (this.project().rawStatus === 'Completed' || this.project().rawStatus === 'Cancelled') {
      return;
    }

    try {
      const dto = await this.projectApi.recomputeProgress(this.projectId);
      this.project.update((view) => ({
        ...view,
        progress: dto.progressPercent,
        deviation: `${dto.deviationDays > 0 ? '+' : ''}${dto.deviationDays} Gün`,
        deviationDays: dto.deviationDays,
        rawStatus: dto.status,
        statusLabel: STATUS_LABELS[dto.status]
      }));
    } catch (error) {
      console.warn('İlerleme senkronize edilemedi.', error);
    }
  }

  selectTab(tab: DetailTab): void {
    this.activeTab.set(tab);
    if (tab === 'tasks') {
      // AI panelinde onaylanan/reddedilen değişiklikleri yakalamak için — o bileşen ayrı yaşam
      // döngüsüne sahip (sekme değişince yok edilip yeniden oluşturuluyor), bu yüzden en güncel
      // bekleyen öneri listesini her Görevler'e dönüşte tazelemek en basit tutarlı yol.
      void this.loadPendingAiSuggestions();
    }
    this.openTaskMenuId.set(null);
    this.closeDialogs();
    this.archiveListOpen.set(false);
    this.cancelInlineSubtask();
    this.closeFeasibilityDialogs();
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: tab === 'overview' ? null : tab },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  toggleSummaryPanel(): void {
    const willOpen = this.summaryCollapsed();
    this.summaryCollapsed.set(!willOpen);
    if (willOpen && this.compactPanelViewport) {
      this.timelineCollapsed.set(true);
    }
  }

  toggleTimelinePanel(): void {
    const willOpen = this.timelineCollapsed();
    this.timelineCollapsed.set(!willOpen);
    if (willOpen && this.compactPanelViewport) {
      this.summaryCollapsed.set(true);
    }
  }

  @HostListener('window:resize')
  onPanelViewportResize(): void {
    const compact = usesPanelOverlay();
    if (compact === this.compactPanelViewport) return;

    this.compactPanelViewport = compact;
    this.summaryCollapsed.set(compact);
    this.timelineCollapsed.set(compact);
  }

  @HostListener('document:click', ['$event'])
  closeTaskPopoversOnOutsideClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target) return;

    if (!target.closest('.pd-seq-add-wrap, .pd-seq-subtask-actions')) {
      this.openTaskMenuId.set(null);
    }
    if (!target.closest('.pd-status-control, .pd-status-pill')) {
      this.openStatusMenuId.set(null);
    }
    if (!target.closest('.pd-task-owner-wrap')) {
      this.reassignTaskContext.set(null);
    }
  }

  toggleTaskMenu(taskId: string): void {
    this.openTaskMenuId.update((openId) => openId === taskId ? null : taskId);
    this.openStatusMenuId.set(null);
    this.reassignTaskContext.set(null);
  }

  openReassignPopover(groupId: string, task: DetailTask): void {
    this.openTaskMenuId.set(null);
    this.openStatusMenuId.set(null);
    this.reassignTaskContext.set({ groupId, taskId: task.id });
    this.reassignEmployeeId.set(task.assigneeEmployeeId ?? '');
  }

  closeReassignPopover(): void {
    this.reassignTaskContext.set(null);
  }

  async saveReassign(): Promise<void> {
    const context = this.reassignTaskContext();
    const employee = this.allEmployees().find((e) => e.id === this.reassignEmployeeId());
    if (!context || !employee || this.savingReassign()) {
      return;
    }

    this.savingReassign.set(true);
    try {
      const updated = await this.taskApi.reassignTask(context.groupId, context.taskId, {
        assigneeEmployeeId: employee.id,
        assigneeName: employee.displayName,
        department: employee.departmentName,
        changedByName: this.currentUserName()
      });
      this.taskGroups.update((groups) => groups.map((g) => g.id === updated.id ? this.toDetailTaskGroup(updated) : g));
      this.reassignTaskContext.set(null);
      this.toastService.success('Görev yeniden atandı.');
      await this.loadTimeline();
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Görev yeniden atanamadı.');
    } finally {
      this.savingReassign.set(false);
    }
  }

  // AI İş Paketi Oluştur artık kendi bileşeninde (ai-work-package-panel) çalışıyor; onay sonrası
  // WorkPackageApprovedConsumer'ın işlemesi için aynı 1500ms'lik gecikme burada korunuyor.
  onAiSuggestionApproved(): void {
    // Onay senkron olarak AiSuggestionRequest üzerinde işlenir — bekleyen öneri listesi hemen
    // tazelenebilir. Gerçek görev ise WorkPackageApprovedEvent'in asenkron işlenmesini bekliyor,
    // bu yüzden görev/zaman çizelgesi yenilemesi 1500ms gecikmeli kalıyor.
    void this.loadPendingAiSuggestions();
    window.setTimeout(() => void this.refreshTasksAndTimeline(), 1500);
  }

  async openTaskDialog(groupId?: string): Promise<void> {
    this.uploadDialogOpen.set(false);
    this.taskFormError.set('');
    this.taskDraft.set(emptyTaskDraft());
    this.taskDialogMode.set('create');
    this.editingTaskId.set(null);

    let targetGroupId = groupId ?? this.taskGroups().find((g) => g.title === 'Genel Görevler')?.id ?? null;
    if (!targetGroupId) {
      try {
        const created = await this.taskApi.createGroup({ projectId: this.projectId, title: 'Genel Görevler', subtitle: '' });
        this.taskGroups.update((groups) => [...groups, this.toDetailTaskGroup(created)]);
        targetGroupId = created.id;
        await this.loadTimeline();
      } catch {
        this.taskFormError.set('Görev grubu oluşturulamadı.');
        return;
      }
    }

    this.taskDialogGroupId = targetGroupId;
    this.taskDialogOpen.set(true);
  }

  openEditTaskDialog(groupId: string, task: DetailTask): void {
    this.openTaskMenuId.set(null);
    this.uploadDialogOpen.set(false);
    this.taskFormError.set('');
    this.taskDialogGroupId = groupId;
    this.taskDialogMode.set('edit');
    this.editingTaskId.set(task.id);
    const start = this.dateInputParts(task.startDateUtc, '09:00');
    const end = this.dateInputParts(task.dueDateUtc, '18:00');
    this.taskDraft.set({
      title: task.title,
      assigneeEmployeeId: task.assigneeEmployeeId ?? '',
      isMainTask: task.isMainTask,
      dependsOnTaskId: task.dependsOnTaskId ?? '',
      startDate: start.date,
      startTime: start.time,
      endDate: end.date,
      endTime: end.time,
      category: task.category ?? '',
      description: task.description ?? '',
      effortHours: task.effortHours?.toString() ?? ''
    });
    this.taskDialogOpen.set(true);
  }

  updateTaskDraft<K extends keyof TaskDraft>(field: K, value: TaskDraft[K]): void {
    this.taskDraft.update((draft) => ({ ...draft, [field]: value }));
  }

  currentGroupTasks(): DetailTask[] {
    return this.taskGroups().find((g) => g.id === this.taskDialogGroupId)?.tasks ?? [];
  }

  private dateInputParts(value: string | null, fallbackTime: string): { date: string; time: string } {
    if (!value) return { date: '', time: fallbackTime };
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return { date: '', time: fallbackTime };
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString();
    return { date: local.slice(0, 10), time: local.slice(11, 16) };
  }

  private positiveNumberOrNull(value: string): number | null {
    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? Math.round(parsed) : null;
  }

  async saveTask(): Promise<void> {
    if (this.savingTask()) {
      return;
    }

    const draft = this.taskDraft();
    const title = draft.title.trim();

    if (!title) {
      this.taskFormError.set('Görev başlığı zorunludur.');
      return;
    }
    const assignee = this.allEmployees().find((employee) => employee.id === draft.assigneeEmployeeId);
    const editingTask = this.editingTaskId()
      ? this.currentGroupTasks().find((task) => task.id === this.editingTaskId())
      : null;
    if (!assignee && this.taskDialogMode() === 'create') {
      this.taskFormError.set('Atanan kişi zorunludur.');
      return;
    }
    if (!this.taskDialogGroupId) {
      this.taskFormError.set('Görev grubu bulunamadı.');
      return;
    }

    this.savingTask.set(true);
    try {
      const payload = {
        title,
        assigneeName: assignee?.displayName ?? editingTask?.assignee ?? '',
        assigneeEmployeeId: assignee?.id ?? editingTask?.assigneeEmployeeId ?? null,
        department: assignee?.departmentName ?? editingTask?.department ?? null,
        effortHours: this.positiveNumberOrNull(draft.effortHours),
        startDateUtc: draft.startDate ? new Date(`${draft.startDate}T${draft.startTime || '00:00'}`).toISOString() : null,
        dueDateUtc: draft.endDate ? new Date(`${draft.endDate}T${draft.endTime || '00:00'}`).toISOString() : null,
        category: draft.category || null,
        description: draft.description.trim() || null
      };
      const updated = this.taskDialogMode() === 'edit' && editingTask
        ? await this.taskApi.updateTask(this.taskDialogGroupId, editingTask.id, payload)
        : await this.taskApi.addTask(this.taskDialogGroupId, {
        ...payload,
        isMainTask: draft.isMainTask,
        dependsOnTaskId: draft.isMainTask ? null : (draft.dependsOnTaskId || null)
      });
      this.taskGroups.update((groups) => groups.map((g) => g.id === updated.id ? this.toDetailTaskGroup(updated) : g));
      this.taskDialogOpen.set(false);
      this.taskFormError.set('');
      this.toastService.success(this.taskDialogMode() === 'edit' ? 'Görev güncellendi.' : 'Görev oluşturuldu.');
      await this.syncProgress();
      await this.loadTimeline();
    } catch (error) {
      this.taskFormError.set(error instanceof Error ? error.message : 'Görev kaydedilemedi.');
    } finally {
      this.savingTask.set(false);
    }
  }

  openInlineSubtaskForm(groupId: string, parent: DetailTask): void {
    this.openTaskMenuId.set(null);
    this.inlineSubtaskContext.set({ groupId, parentTaskId: parent.id });
    this.inlineSubtaskDraft.set({
      title: '',
      assigneeEmployeeId: parent.assigneeEmployeeId ?? '',
      effortHours: ''
    });
    this.inlineSubtaskError.set('');
    this.expandedMainTaskIds.update((ids) => new Set(ids).add(parent.id));
  }

  updateInlineSubtaskDraft<K extends keyof InlineSubtaskDraft>(field: K, value: InlineSubtaskDraft[K]): void {
    this.inlineSubtaskDraft.update((draft) => ({ ...draft, [field]: value }));
  }

  cancelInlineSubtask(): void {
    this.inlineSubtaskContext.set(null);
    this.inlineSubtaskDraft.set(emptyInlineSubtaskDraft());
    this.inlineSubtaskError.set('');
  }

  async saveInlineSubtask(): Promise<void> {
    const context = this.inlineSubtaskContext();
    const draft = this.inlineSubtaskDraft();
    const title = draft.title.trim();
    const assignee = this.allEmployees().find((employee) => employee.id === draft.assigneeEmployeeId);
    if (!context || this.savingInlineSubtask()) return;
    if (!title) {
      this.inlineSubtaskError.set('Alt görev başlığı zorunludur.');
      return;
    }
    if (!assignee) {
      this.inlineSubtaskError.set('Atanan kişiyi seçin.');
      return;
    }

    this.savingInlineSubtask.set(true);
    try {
      const updated = await this.taskApi.addTask(context.groupId, {
        title,
        assigneeName: assignee.displayName,
        assigneeEmployeeId: assignee.id,
        department: assignee.departmentName,
        effortHours: this.positiveNumberOrNull(draft.effortHours),
        isMainTask: false,
        dependsOnTaskId: context.parentTaskId,
        startDateUtc: null,
        dueDateUtc: null,
        category: null,
        description: null
      });
      this.taskGroups.update((groups) => groups.map((group) => group.id === updated.id ? this.toDetailTaskGroup(updated) : group));
      this.cancelInlineSubtask();
      this.toastService.success('Alt görev eklendi.');
      await this.syncProgress();
      await this.loadTimeline();
    } catch (error) {
      this.inlineSubtaskError.set(error instanceof Error ? error.message : 'Alt görev eklenemedi.');
    } finally {
      this.savingInlineSubtask.set(false);
    }
  }

  async copyTask(groupId: string, task: DetailTask): Promise<void> {
    this.openTaskMenuId.set(null);
    try {
      const result = await this.taskApi.copyTask(groupId, task.id);
      this.taskGroups.update((groups) => groups.map((group) => group.id === result.group.id ? this.toDetailTaskGroup(result.group) : group));
      this.toastService.success(result.copiedTaskCount > 1
        ? `Görev ve ${result.copiedTaskCount - 1} alt görevi kopyalandı.`
        : 'Görev kopyalandı.');
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Görev kopyalanamadı.');
    }
  }

  openArchiveTaskConfirmation(groupId: string, task: DetailTask, childCount = 0): void {
    this.openTaskMenuId.set(null);
    this.archiveTaskContext.set({ groupId, task, childCount });
  }

  closeArchiveTaskConfirmation(): void {
    if (!this.savingArchive()) this.archiveTaskContext.set(null);
  }

  async confirmArchiveTask(): Promise<void> {
    const context = this.archiveTaskContext();
    if (!context || this.savingArchive()) return;

    this.savingArchive.set(true);
    try {
      const result = await this.taskApi.archiveTask(context.groupId, context.task.id);
      this.taskGroups.update((groups) => groups.map((group) => group.id === result.group.id ? this.toDetailTaskGroup(result.group) : group));
      this.archiveTaskContext.set(null);
      this.toastService.success(result.archivedTaskCount > 1
        ? `Görev ve ${result.archivedTaskCount - 1} alt görevi arşivlendi.`
        : 'Görev arşivlendi.');
      await this.loadArchivedTasks();
      await this.syncProgress();
      await this.loadTimeline();
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Görev arşivlenemedi.');
    } finally {
      this.savingArchive.set(false);
    }
  }

  openArchiveList(): void {
    this.openTaskMenuId.set(null);
    this.archiveListOpen.set(true);
  }

  closeArchiveList(): void {
    if (!this.restoringTaskId()) this.archiveListOpen.set(false);
  }

  async restoreArchivedTask(task: ArchivedTaskDto): Promise<void> {
    if (this.restoringTaskId()) return;
    this.restoringTaskId.set(task.taskId);
    try {
      const result = await this.taskApi.restoreTask(task.groupId, task.taskId);
      this.taskGroups.update((groups) => groups.map((group) => group.id === result.group.id ? this.toDetailTaskGroup(result.group) : group));
      this.archivedTasks.update((items) => items.filter((item) => item.taskId !== task.taskId));
      this.toastService.success(result.restoredTaskCount > 1
        ? `Görev ve ${result.restoredTaskCount - 1} alt görevi geri yüklendi.`
        : 'Görev geri yüklendi.');
      await this.syncProgress();
      await this.loadTimeline();
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Görev geri yüklenemedi.');
    } finally {
      this.restoringTaskId.set(null);
    }
  }

  private async loadArchivedTasks(): Promise<void> {
    if (!this.canManageProjects()) return;
    try {
      this.archivedTasks.set(await this.taskApi.listArchivedTasks(this.projectId));
    } catch {
      // Aktif görev işlemi başarılı olduysa arşiv sayacının yenilenememesi ana akışı bozmamalı.
    }
  }

  isTaskUnassigned(task: DetailTask): boolean {
    const name = task.assignee.trim().toLocaleLowerCase('tr-TR');
    return !task.assigneeEmployeeId || !name || name === 'atanmamış';
  }

  canChangeTaskStatus(task: DetailTask): boolean {
    return this.canManageProjects() || (
      !this.isTaskUnassigned(task) &&
      task.assignee.localeCompare(this.currentUserName(), 'tr-TR', { sensitivity: 'base' }) === 0
    );
  }

  completedSubtaskCount(entry: SequencedTask): number {
    return entry.subtasks.filter((task) => task.status === 'Done').length;
  }

  taskProgressPercent(entry: SequencedTask): number {
    return entry.subtasks.length ? Math.round((this.completedSubtaskCount(entry) / entry.subtasks.length) * 100) : 0;
  }

  toggleStatusMenu(taskId: string): void {
    this.openTaskMenuId.set(null);
    this.reassignTaskContext.set(null);
    this.openStatusMenuId.update((openId) => openId === taskId ? null : taskId);
  }

  async selectTaskStatus(groupId: string, task: DetailTask, nextStatus: KanbanStatus): Promise<void> {
    this.openStatusMenuId.set(null);
    if (task.status === nextStatus || this.savingStatusTaskId()) return;
    if (!this.canChangeTaskStatus(task)) {
      this.toastService.error('Yalnızca görev sorumlusu veya proje yöneticisi durumu değiştirebilir.');
      return;
    }
    if (this.isTaskUnassigned(task) && nextStatus !== 'Todo') {
      this.toastService.error('Atanmamış görev başlatılamaz. Önce bir sorumlu atayın.');
      return;
    }

    if (nextStatus === 'Done' && task.isMainTask) {
      const openSubtasks = this.taskGroups()
        .find((group) => group.id === groupId)?.tasks
        .filter((item) => !item.isMainTask && item.dependsOnTaskId === task.id && item.status !== 'Done') ?? [];
      if (openSubtasks.length) {
        this.toastService.error(`Önce ${openSubtasks.length} açık alt görevi tamamlayın.`);
        this.expandedMainTaskIds.update((ids) => new Set(ids).add(task.id));
        return;
      }
    }

    if (task.status === 'Done' && nextStatus !== 'Done') {
      this.statusConfirmation.set({ groupId, task, nextStatus });
      return;
    }

    await this.applyTaskStatus(groupId, task, nextStatus);
  }

  closeStatusConfirmation(): void {
    if (!this.savingStatusTaskId()) this.statusConfirmation.set(null);
  }

  async confirmTaskReopen(): Promise<void> {
    const context = this.statusConfirmation();
    if (!context) return;
    await this.applyTaskStatus(context.groupId, context.task, context.nextStatus);
    if (!this.savingStatusTaskId()) this.statusConfirmation.set(null);
  }

  private async applyTaskStatus(groupId: string, task: DetailTask, status: KanbanStatus): Promise<void> {
    this.savingStatusTaskId.set(task.id);
    try {
      const updated = await this.taskApi.changeStatus(groupId, task.id, { status });
      this.taskGroups.update((groups) => groups.map((group) => group.id === updated.id ? this.toDetailTaskGroup(updated) : group));
      this.statusConfirmation.set(null);
      this.toastService.success(`Görev durumu “${this.taskStatusLabel(status)}” olarak güncellendi.`);
      await this.syncProgress();
      await this.loadTimeline();
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Görev durumu güncellenemedi.');
    } finally {
      this.savingStatusTaskId.set(null);
    }
  }

  onTaskRowClick(event: MouseEvent, taskId: string, hasSubtasks: boolean): void {
    if (!hasSubtasks) return;
    const target = event.target as HTMLElement;
    if (target.closest('button, input, select, textarea, .pd-task-menu, .pd-reassign-popover')) return;
    this.toggleTaskRowExpanded(taskId);
  }

  onTaskRowKeydown(event: KeyboardEvent, taskId: string, hasSubtasks: boolean): void {
    if (!hasSubtasks || (event.key !== 'Enter' && event.key !== ' ')) return;
    const target = event.target as HTMLElement;
    if (target.closest('button, input, select, textarea')) return;
    event.preventDefault();
    this.toggleTaskRowExpanded(taskId);
  }

  setTaskListFilter(filter: 'all' | KanbanStatus | 'ai' | 'unassigned'): void {
    this.taskListFilter.set(filter);
    if (filter === 'unassigned') {
      this.expandedMainTaskIds.set(new Set(
        this.sequencedTasks()
          .filter((entry) => entry.subtasks.some((task) => this.isTaskUnassigned(task)))
          .map((entry) => entry.task.id)
      ));
    }
  }

  toggleTaskRowExpanded(taskId: string): void {
    this.expandedMainTaskIds.update((set) => {
      const next = new Set(set);
      if (next.has(taskId)) next.delete(taskId);
      else next.add(taskId);
      return next;
    });
  }

  isTaskRowExpanded(taskId: string): boolean {
    return this.allTaskRowsExpanded() || this.expandedMainTaskIds().has(taskId);
  }

  toggleAllTaskRows(): void {
    this.allTaskRowsExpanded.update((open) => !open);
    this.expandedMainTaskIds.set(new Set());
  }

  trackSequenceRow(row: UnifiedSequenceRow): string {
    return row.kind === 'task' ? `task-${row.entry.task.id}` : `pending-${row.pending.itemId}`;
  }

  taskStatusLabel(status: KanbanStatus): string {
    return status === 'Done' ? 'Tamamlandı' : status === 'InProgress' ? 'Devam Ediyor' : 'Bekliyor';
  }

  taskDateRange(entry: SequencedTask): string | null {
    if (!entry.task.startDateUtc && !entry.task.dueDateUtc) return null;
    const start = entry.task.startDateUtc ? this.formatDate(entry.task.startDateUtc) : '—';
    const end = entry.task.dueDateUtc ? this.formatDate(entry.task.dueDateUtc) : '—';
    return `${start} – ${end}`;
  }

  taskTotalHours(entry: SequencedTask): number {
    const own = entry.task.effortHours ?? 0;
    return entry.subtasks.reduce((total, s) => total + (s.effortHours ?? 0), own);
  }

  taskInitials(name: string): string {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (!parts.length) return '?';
    return parts.length === 1 ? parts[0].slice(0, 2).toUpperCase() : (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  async toggleTaskDone(groupId: string, task: DetailTask): Promise<void> {
    const nextStatus: KanbanStatus = task.done ? 'Todo' : 'Done';
    try {
      const updated = await this.taskApi.changeStatus(groupId, task.id, { status: nextStatus });
      this.taskGroups.update((groups) => groups.map((g) => g.id === updated.id ? this.toDetailTaskGroup(updated) : g));
      await this.syncProgress();
      await this.loadTimeline();
    } catch {
      this.toastService.error('Görev durumu güncellenemedi.');
    }
  }

  openTaskComments(groupId: string, taskId: string): void {
    this.openTaskMenuId.set(null);
    this.openStatusMenuId.set(null);
    this.taskCommentDraft.set('');
    this.taskCommentContext.set({ groupId, taskId });
  }

  closeTaskComments(): void {
    if (this.savingTaskComment()) return;
    this.taskCommentContext.set(null);
    this.taskCommentDraft.set('');
  }

  async saveTaskComment(): Promise<void> {
    const context = this.taskCommentContext();
    const text = this.taskCommentDraft().trim();
    if (!context || !text || this.savingTaskComment()) return;

    this.savingTaskComment.set(true);
    try {
      const author = this.currentUserName().trim() || 'Kullanıcı';
      const updated = await this.taskApi.addComment(context.groupId, context.taskId, { author, text });
      this.taskGroups.update((groups) => groups.map((g) => g.id === updated.id ? this.toDetailTaskGroup(updated) : g));
      this.taskCommentDraft.set('');
      this.toastService.success('Yorum göreve eklendi.');
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Yorum eklenemedi.');
    } finally {
      this.savingTaskComment.set(false);
    }
  }

  isTaskActivityComment(comment: TaskCommentDto): boolean {
    return comment.text.startsWith('Görev durumu ') ||
      (comment.text.startsWith('Görev "') && comment.text.includes(' kişisine devredildi.'));
  }

  taskUserCommentCount(task: DetailTask): number {
    return task.commentEntries.filter((comment) => !this.isTaskActivityComment(comment)).length;
  }

  openUploadDialog(): void {
    this.taskDialogOpen.set(false);
    this.openTaskMenuId.set(null);
    this.uploadFileName.set('');
    this.uploadFiles.set([]);
    this.uploadDialogOpen.set(true);
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.addUploadFiles(Array.from(input.files ?? []));
    input.value = '';
  }

  onFilesDropped(event: DragEvent): void {
    event.preventDefault();
    this.addUploadFiles(Array.from(event.dataTransfer?.files ?? []));
  }

  removeUploadFile(fileId: string): void {
    this.uploadFiles.update((files) => files.filter((file) => file.id !== fileId));
  }

  async saveUpload(): Promise<void> {
    const files = this.uploadFiles();
    if (!files.length || this.savingUpload()) {
      return;
    }

    this.savingUpload.set(true);
    const customName = this.uploadFileName().trim();

    for (const uploadFile of files) {
      try {
        const renamed = customName && files.length === 1
          ? new File([uploadFile.file], customName, { type: uploadFile.file.type })
          : uploadFile.file;
        const doc = await this.taskApi.uploadDocument(this.projectId, renamed, { uploadedBy: this.currentUserName() });
        this.documents.update((documents) => [
          {
            id: doc.id,
            noteId: doc.noteId,
            uploadedBy: doc.uploadedBy,
            name: doc.name,
            kind: doc.kind.toLowerCase() as DocumentKind,
            size: this.formatFileSize(doc.sizeBytes),
            sizeBytes: doc.sizeBytes,
            createdAtUtc: doc.createdAtUtc
          },
          ...documents
        ]);
        this.triggerRagSync(doc.id, doc.name);
      } catch {
        this.toastService.error(`${uploadFile.name} yüklenemedi.`);
      }
    }

    this.savingUpload.set(false);
    this.uploadDialogOpen.set(false);
    this.uploadFiles.set([]);
  }

  // Fires RAG sync in the background right after upload instead of waiting for the next AI İş Paketi/
  // sohbet call to lazily discover the document — gives the user an immediate, explicit signal whether
  // this specific file actually became usable by the AI features (RAG may reject unsupported formats
  // or fail to extract content, e.g. table-only .docx layouts).
  private triggerRagSync(documentId: string, fileName: string): void {
    this.ragSyncApi
      .syncDocument(this.projectId, documentId)
      .then((result) => {
        if (result.fullySynced && result.confirmedIndexedFileNames.includes(fileName)) {
          this.toastService.success(`${fileName} RAG'e eklendi, AI özelliklerinde kullanılabilir.`);
        } else {
          this.toastService.error(`${fileName} RAG'e eklenemedi, AI özellikleri bu dosyayı kullanamayacak.`);
        }
      })
      .catch(() => {
        this.toastService.error(`${fileName} RAG'e eklenemedi, AI özellikleri bu dosyayı kullanamayacak.`);
      });
  }

  closeDialogs(): void {
    this.taskDialogOpen.set(false);
    this.uploadDialogOpen.set(false);
    this.taskFormError.set('');
    this.editingTaskId.set(null);
  }

  @HostListener('document:keydown.escape')
  closeDialogWithEscape(): void {
    if (this.taskDialogOpen() || this.uploadDialogOpen()) {
      this.closeDialogs();
    }
    if (this.mainGroupDialogOpen() || this.itemDialogOpen() || this.submitDialogOpen() || this.decideDialogOpen()) {
      this.closeFeasibilityDialogs();
    }
    if (this.reassignTaskContext()) {
      this.closeReassignPopover();
    }
    if (this.archiveTaskContext()) {
      this.closeArchiveTaskConfirmation();
    }
    if (this.archiveListOpen()) {
      this.closeArchiveList();
    }
    if (this.taskCommentContext()) {
      this.closeTaskComments();
    }
    if (this.inlineSubtaskContext()) {
      this.cancelInlineSubtask();
    }
    this.openStatusMenuId.set(null);
    if (this.statusConfirmation()) {
      this.closeStatusConfirmation();
    }
    if (this.documentDeleteConfirmation()) {
      this.closeDocumentDeleteConfirmation();
    }
  }

  onCommentFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.commentAttachments.update((files) => [...files, ...Array.from(input.files ?? [])]);
    input.value = '';
  }

  removeCommentAttachment(index: number): void {
    this.commentAttachments.update((files) => files.filter((_, i) => i !== index));
  }

  // Lets the composer grow with multi-line text instead of clipping it — plain CSS can't do this
  // since height needs to track scrollHeight, which only exists once content is laid out.
  autoGrowComposer(textarea: HTMLTextAreaElement): void {
    textarea.style.height = 'auto';
    textarea.style.height = `${textarea.scrollHeight}px`;
  }

  // Ctrl/Cmd+Enter sends, like most multi-line composers — plain Enter must stay a newline since this
  // is a <textarea> (unlike the old single-line <input>, which submitted the form on every Enter).
  onComposerKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.sendComment();
    }
  }

  async sendComment(): Promise<void> {
    const text = this.commentDraft().trim();
    if (!text || this.sendingComment()) {
      return;
    }

    this.sendingComment.set(true);
    try {
      const author = (this.authService.currentUser()?.displayName ?? '');
      const dto = await this.projectApi.addNote(this.projectId, { author, text });
      this.messages.set(this.buildMessages(dto.notes, dto.unit));
      this.commentDraft.set('');
      const textareaEl = this.composerTextarea()?.nativeElement;
      if (textareaEl) {
        textareaEl.style.height = 'auto';
      }

      const newestNoteId = dto.notes.length ? dto.notes[dto.notes.length - 1].id : null;
      const pendingFiles = this.commentAttachments();
      if (newestNoteId && pendingFiles.length) {
        for (const file of pendingFiles) {
          try {
            const doc = await this.taskApi.uploadDocument(this.projectId, file, { noteId: newestNoteId, uploadedBy: author });
            this.triggerRagSync(doc.id, doc.name);
          } catch {
            this.toastService.error(`${file.name} eklenemedi.`);
          }
        }
        this.commentAttachments.set([]);
        await this.loadDocuments();
      }
    } catch {
      this.toastService.error('Açıklama kaydedilemedi.');
    } finally {
      this.sendingComment.set(false);
    }
  }

  startEditNote(message: DetailMessage): void {
    this.editingNoteId.set(message.id);
    this.noteEditDraft.set(message.paragraphs[0] ?? '');
  }

  cancelEditNote(): void {
    this.editingNoteId.set(null);
    this.noteEditDraft.set('');
  }

  async saveEditNote(): Promise<void> {
    const noteId = this.editingNoteId();
    const text = this.noteEditDraft().trim();
    if (!noteId || !text || this.savingNoteEdit()) {
      return;
    }

    this.savingNoteEdit.set(true);
    try {
      const author = this.currentUserName();
      const dto = await this.projectApi.updateNote(this.projectId, noteId, { author, text });
      this.messages.set(this.buildMessages(dto.notes, dto.unit));
      this.editingNoteId.set(null);
      this.toastService.success('Not güncellendi.');
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Not güncellenemedi.');
    } finally {
      this.savingNoteEdit.set(false);
    }
  }

  async shareNote(message: DetailMessage): Promise<void> {
    const url = `${window.location.origin}${window.location.pathname}?tab=overview#note-${message.id}`;
    try {
      await navigator.clipboard.writeText(url);
      this.toastService.success('Bağlantı kopyalandı.');
    } catch {
      this.toastService.error('Bağlantı kopyalanamadı.');
    }
  }

  async downloadDocument(document: ProjectDocumentView): Promise<void> {
    try {
      await this.taskApi.downloadDocument(this.projectId, document.id, document.name);
    } catch {
      this.toastService.error(`${document.name} indirilemedi.`);
    }
  }

  openDocumentDeleteConfirmation(document: ProjectDocumentView): void {
    if (!this.canManageProjects() || this.deletingDocumentId()) return;
    this.documentDeleteConfirmation.set({ document });
  }

  closeDocumentDeleteConfirmation(): void {
    if (!this.deletingDocumentId()) this.documentDeleteConfirmation.set(null);
  }

  async confirmDeleteDocument(): Promise<void> {
    const confirmation = this.documentDeleteConfirmation();
    if (!confirmation || this.deletingDocumentId()) return;

    const { document } = confirmation;
    this.deletingDocumentId.set(document.id);
    try {
      await this.taskApi.deleteDocument(this.projectId, document.id);
      this.documents.update((documents) => documents.filter((item) => item.id !== document.id));
      this.documentDeleteConfirmation.set(null);
      this.toastService.success(`${document.name} silindi.`);
    } catch (error) {
      // The document may have been deleted by another request while this page was still open.
      // Reconcile with the server before reporting a failure so a stale card does not get stuck onscreen.
      await this.loadDocuments();
      if (!this.documents().some((item) => item.id === document.id)) {
        this.documentDeleteConfirmation.set(null);
        this.toastService.success(`${document.name} silindi.`);
      } else {
        this.toastService.error(error instanceof Error ? error.message : `${document.name} silinemedi.`);
      }
    } finally {
      this.deletingDocumentId.set(null);
    }
  }

  async renameTaskGroup(group: DetailTaskGroup): Promise<void> {
    const newTitle = window.prompt('Görev grubunun yeni başlığı:', group.title);
    if (!newTitle?.trim() || newTitle.trim() === group.title) {
      return;
    }

    try {
      const updated = await this.taskApi.renameGroup(group.id, { title: newTitle.trim(), subtitle: group.subtitle });
      this.taskGroups.update((groups) => groups.map((g) => g.id === updated.id ? this.toDetailTaskGroup(updated) : g));
      await this.loadTimeline();
    } catch {
      this.toastService.error('Görev grubu yeniden adlandırılamadı.');
    }
  }

  nextPendingApprover(item: FeasibilityItemDto): ApprovalStepDto | null {
    return item.steps.filter((s) => s.decision === 'Pending').sort((a, b) => a.order - b.order)[0] ?? null;
  }

  private static readonly FEASIBILITY_STATUS_LABELS: Record<FeasibilityItemStatus, string> = {
    Draft: 'Taslak',
    PendingApproval: 'Onay Bekliyor',
    Approved: 'Onaylandı',
    Rejected: 'Reddedildi'
  };

  private static readonly APPROVAL_DECISION_LABELS: Record<ApprovalDecision, string> = {
    Pending: 'Bekliyor',
    Approved: 'Onayladı',
    Rejected: 'Reddetti'
  };

  feasibilityStatusLabel(status: FeasibilityItemStatus): string {
    return ProjectDetailPage.FEASIBILITY_STATUS_LABELS[status];
  }

  feasibilityDecisionLabel(decision: ApprovalDecision): string {
    return ProjectDetailPage.APPROVAL_DECISION_LABELS[decision];
  }

  formatFeasibilityAmount(amount: number, currency: string): string {
    const formatted = new Intl.NumberFormat('tr-TR').format(amount);
    return currency === 'TRY' ? `${formatted} ₺` : `${formatted} ${currency}`;
  }

  private formatCurrencyAmount(amount: number): string {
    return `${new Intl.NumberFormat('tr-TR').format(amount)} ₺`;
  }

  openMainGroupDialog(): void {
    this.closeFeasibilityDialogs();
    this.newMainGroupName.set('');
    const workPackages = this.timelineWorkPackageOptions();
    this.newMainGroupWorkPackageId.set(workPackages.length === 1 ? workPackages[0].id : '');
    this.mainGroupDialogOpen.set(true);
  }

  async saveMainGroup(): Promise<void> {
    if (this.savingFeasibility()) {
      return;
    }
    const name = this.newMainGroupName().trim();
    if (!name) {
      this.feasibilityFormError.set('Ana grup adı zorunludur.');
      return;
    }
    if (!this.newMainGroupWorkPackageId()) {
      this.feasibilityFormError.set('Ana grubun bağlı olduğu iş paketi seçilmelidir.');
      return;
    }
    this.savingFeasibility.set(true);
    try {
      await this.feasibilityApi.createMainGroup({
        projectId: this.projectId,
        name,
        workPackageId: this.newMainGroupWorkPackageId()
      });
      await this.loadFeasibilityGroups();
      await this.loadTimeline();
      this.mainGroupDialogOpen.set(false);
      this.feasibilityFormError.set('');
    } catch (error) {
      this.feasibilityFormError.set(error instanceof Error ? error.message : 'Ana grup oluşturulamadı.');
    } finally {
      this.savingFeasibility.set(false);
    }
  }

  openItemDialog(mainGroupId: string): void {
    this.closeFeasibilityDialogs();
    this.itemDialogGroupId = mainGroupId;
    this.itemDraft.set(emptyFeasibilityItemDraft());
    this.itemDialogOpen.set(true);
  }

  updateItemDraft<K extends keyof FeasibilityItemDraft>(field: K, value: FeasibilityItemDraft[K]): void {
    this.itemDraft.update((draft) => ({ ...draft, [field]: value }));
  }

  async saveItem(): Promise<void> {
    if (this.savingFeasibility()) {
      return;
    }

    const draft = this.itemDraft();
    const amount = Number(draft.amount);

    if (!draft.unit.trim()) {
      this.feasibilityFormError.set('Birim zorunludur.');
      return;
    }
    if (!draft.description.trim()) {
      this.feasibilityFormError.set('Açıklama zorunludur.');
      return;
    }
    if (!amount || amount <= 0) {
      this.feasibilityFormError.set('Tutar sıfırdan büyük olmalıdır.');
      return;
    }
    if (!this.itemDialogGroupId) {
      return;
    }

    this.savingFeasibility.set(true);
    try {
      await this.feasibilityApi.addItem(this.itemDialogGroupId, {
        unit: draft.unit.trim(),
        description: draft.description.trim(),
        amount,
        currency: draft.currency
      });
      await this.loadFeasibilityGroups();
      await this.loadTimeline();
      this.itemDialogOpen.set(false);
      this.feasibilityFormError.set('');
    } catch (error) {
      this.feasibilityFormError.set(error instanceof Error ? error.message : 'Bütçe kalemi eklenemedi.');
    } finally {
      this.savingFeasibility.set(false);
    }
  }

  openSubmitDialog(mainGroupId: string, itemId: string): void {
    this.closeFeasibilityDialogs();
    this.submitContext = { mainGroupId, itemId };
    this.approverNames.set(['']);
    this.submitDialogOpen.set(true);
  }

  updateApproverName(index: number, value: string): void {
    this.approverNames.update((names) => names.map((name, i) => (i === index ? value : name)));
  }

  addApproverField(): void {
    this.approverNames.update((names) => [...names, '']);
  }

  removeApproverField(index: number): void {
    this.approverNames.update((names) => (names.length > 1 ? names.filter((_, i) => i !== index) : names));
  }

  async saveSubmit(): Promise<void> {
    if (this.savingFeasibility()) {
      return;
    }

    const names = this.approverNames().map((name) => name.trim()).filter(Boolean);
    if (!names.length) {
      this.feasibilityFormError.set('En az bir onaylayıcı belirtilmelidir.');
      return;
    }
    if (!this.submitContext) {
      return;
    }

    this.savingFeasibility.set(true);
    try {
      await this.feasibilityApi.submitForApproval(this.submitContext.mainGroupId, this.submitContext.itemId, {
        approverNamesInOrder: names
      });
      await this.loadFeasibilityGroups();
      await this.loadTimeline();
      this.submitDialogOpen.set(false);
      this.feasibilityFormError.set('');
      await this.syncProgress();
    } catch (error) {
      this.feasibilityFormError.set(error instanceof Error ? error.message : 'Onaya gönderilemedi.');
    } finally {
      this.savingFeasibility.set(false);
    }
  }

  openDecideDialog(mainGroupId: string, item: FeasibilityItemDto, approve: boolean): void {
    const nextApprover = this.nextPendingApprover(item);
    this.closeFeasibilityDialogs();
    this.decideContext = {
      mainGroupId,
      itemId: item.id,
      approverName: nextApprover?.approverName ?? (this.authService.currentUser()?.displayName ?? '')
    };
    this.decideApprove.set(approve);
    this.decideComment.set('');
    this.decideDialogOpen.set(true);
  }

  async saveDecide(): Promise<void> {
    if (this.savingFeasibility() || !this.decideContext) {
      return;
    }

    this.savingFeasibility.set(true);
    try {
      await this.feasibilityApi.decide(this.decideContext.mainGroupId, this.decideContext.itemId, {
        approverName: this.decideContext.approverName,
        approve: this.decideApprove(),
        comment: this.decideComment().trim() || null
      });
      await this.loadFeasibilityGroups();
      await this.loadTimeline();
      this.decideDialogOpen.set(false);
      this.feasibilityFormError.set('');
      await this.syncProgress();
    } catch (error) {
      this.feasibilityFormError.set(error instanceof Error ? error.message : 'Karar kaydedilemedi.');
    } finally {
      this.savingFeasibility.set(false);
    }
  }

  closeFeasibilityDialogs(): void {
    this.mainGroupDialogOpen.set(false);
    this.itemDialogOpen.set(false);
    this.submitDialogOpen.set(false);
    this.decideDialogOpen.set(false);
    this.feasibilityFormError.set('');
  }

  goBack(): void {
    this.router.navigate(['/projects']);
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

  private addUploadFiles(files: File[]): void {
    if (!files.length) {
      return;
    }

    const additions: UploadFileView[] = files.map((file, index) => ({
      id: `upload-${Date.now()}-${index}`,
      name: file.name,
      size: this.formatFileSize(file.size),
      kind: this.documentKind(file.name),
      progress: 100,
      file
    }));
    this.uploadFiles.update((current) => [...current, ...additions]);
  }

  private documentKind(fileName: string): DocumentKind {
    const extension = fileName.split('.').pop()?.toLocaleLowerCase('tr-TR');
    if (extension === 'doc' || extension === 'docx') return 'word';
    if (extension === 'ppt' || extension === 'pptx') return 'powerpoint';
    if (extension === 'xls' || extension === 'xlsx') return 'excel';
    if (extension === 'pdf') return 'pdf';
    if (extension === 'jpg' || extension === 'jpeg' || extension === 'png' || extension === 'webp') return 'image';
    if (extension === 'mov' || extension === 'mp4' || extension === 'webm') return 'video';
    return 'file';
  }

  private formatFileSize(bytes: number): string {
    if (bytes < 1024 * 1024) {
      return `${Math.max(1, Math.round(bytes / 1024))}.Kb`;
    }
    return `${(bytes / (1024 * 1024)).toFixed(1)}.Mb`;
  }

  private initialTab(): DetailTab {
    const requested = this.route.snapshot.queryParamMap.get('tab');
    return requested === 'tasks' || requested === 'ai' || requested === 'documents' || requested === 'feasibility' || requested === 'activity' || requested === 'assistant'
      ? requested
      : 'overview';
  }
}
