import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Icon, IconName } from '../../../shared/icon/icon';
import { ToastService } from '../../../shared/toast/toast.service';
import { ProjectApiService } from '../data/project-api.service';
import { BackendProjectType, ProjectListItemDto, ProjectStatus } from '../data/project-api.models';
import { AuthService } from '../../../shared/auth/auth.service';
import { ProjectBoardApiService } from '../data/project-board-api.service';
import { ProjectBoardColumnDto } from '../data/project-board-api.models';
import { TaskApiService } from '../data/task-api.service';
import { KanbanStatus, TaskGroupDto, TaskItemDto } from '../data/task-api.models';

type ProjectType = 'Basit' | 'Çoklu Birimli' | 'Fizibiliteye Bağlı';
type ProgressTone = 'green' | 'orange' | 'blue' | 'slate';
type ProjectColumnKey = 'manager' | 'unit' | 'status' | 'progress' | 'deviation' | 'budget' | 'type' | 'start' | 'end';
type ProjectSortKey = 'name' | ProjectColumnKey;
type SortDirection = 'asc' | 'desc';

const TYPE_LABELS: Record<BackendProjectType, ProjectType> = {
  Simple: 'Basit',
  MultiUnit: 'Çoklu Birimli',
  FeasibilityBased: 'Fizibiliteye Bağlı'
};

const STATUS_LABELS: Record<ProjectStatus, string> = {
  Draft: 'Taslak',
  Active: 'Aktif',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal Edildi'
};

interface ProjectColumn {
  key: ProjectColumnKey;
  label: string;
}

interface ProjectRow {
  id: string;
  unitDepartmentId: string | null;
  boardColumnId: string | null;
  boardPosition: number;
  updatedAtUtc: string;
  name: string;
  manager: string;
  unit: string;
  progress: number;
  progressTone: ProgressTone;
  deviation: string;
  deviationTone: 'good' | 'bad' | 'neutral';
  budget: string;
  type: ProjectType;
  start: string;
  end: string;
  startIso: string;
  endIso: string;
  status: ProjectStatus;
  statusLabel: string;
}

interface BoardCard {
  id: string;
  status: ProjectStatus;
  boardColumnId: string | null;
  updatedAtUtc: string;
  name: string;
  manager: string;
  unit: string;
  type: ProjectType;
  budget: string;
  deviation: string;
  deviationTone: 'good' | 'bad' | 'neutral';
  progress: number;
  progressTone: ProgressTone;
}

interface BoardColumn {
  key: string;
  id: string | null;
  title: string;
  color: string;
  isUnassigned: boolean;
  isProtected: boolean;
  cards: BoardCard[];
}

interface GanttTask {
  id: string;
  name: string;
  start: number;
  width: number;
  label: string;
  groupTitle: string;
  depth: number;
  scheduled: boolean;
  status: KanbanStatus;
}

interface GanttRow {
  id: string;
  name: string;
  start: number;
  width: number;
  color: string;
  label: string;
  tasks: GanttTask[];
  tasksLoaded: boolean;
  tasksLoading: boolean;
  tasksLoadFailed: boolean;
}

interface RailItem {
  icon: IconName;
  label: string;
  active?: boolean;
}

const GANTT_BAR_COLORS = ['#ffb795', '#c9befb', '#a9d9ca', '#69e5a0', '#c8e5b5', '#ddd8de', '#a3ecee', '#f8d4c3'];

@Component({
  selector: 'app-projects-page',
  standalone: true,
  imports: [CommonModule, DragDropModule, Icon],
  templateUrl: './projects-page.html',
  styleUrl: './projects-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectsPage implements OnInit, OnDestroy {
  private static readonly REFRESH_INTERVAL_MS = 30_000;

  private readonly router = inject(Router);
  private readonly projectApi = inject(ProjectApiService);
  private readonly projectBoardApi = inject(ProjectBoardApiService);
  private readonly taskApi = inject(TaskApiService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  readonly currentUserName = computed(() => (this.authService.currentUser()?.displayName ?? ''));
  readonly canManageProjects = computed(() => this.authService.hasAnyRole(['Admin', 'ProjectManager']));
  // Deleting a project is irreversible, so it's kept narrower than the general "manage" tier —
  // Admin only, not ProjectManager (see Policies.CanDeleteProjects on the backend).
  readonly canDeleteProjects = computed(() => this.authService.hasAnyRole(['Admin']));

  readonly pendingDeleteProject = signal<ProjectRow | BoardCard | null>(null);
  readonly deletingProject = signal(false);

  private readonly initialView = new URLSearchParams(window.location.search).get('view');
  private suppressProjectOpen = false;
  readonly activeView = signal<'list' | 'cards' | 'gantt'>(
    this.initialView === 'cards' || this.initialView === 'gantt' ? this.initialView : 'list'
  );
  readonly activeType = signal<'Tümü' | ProjectType>('Tümü');
  readonly searchOpen = signal(false);
  readonly searchQuery = signal('');
  readonly filtersOpen = signal(false);
  readonly columnsOpen = signal(false);
  readonly profileOpen = signal(false);
  readonly railCompact = signal(false);
  readonly expandedGanttRows = signal<ReadonlySet<string>>(new Set());
  private readonly ganttTaskGroups = signal<ReadonlyMap<string, TaskGroupDto[]>>(new Map());
  private readonly ganttTaskLoadingIds = signal<ReadonlySet<string>>(new Set());
  private readonly ganttTaskErrorIds = signal<ReadonlySet<string>>(new Set());
  readonly showCompleted = signal(true);
  readonly delayedOnly = signal(false);
  readonly assignedOnly = signal(false);
  readonly sortKey = signal<ProjectSortKey | null>(null);
  readonly sortDirection = signal<SortDirection>('asc');

  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly backgroundRefreshFailed = signal(false);
  readonly lastUpdatedAt = signal<Date | null>(null);
  readonly lastUpdatedLabel = computed(() => {
    const updatedAt = this.lastUpdatedAt();
    return updatedAt
      ? new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' }).format(updatedAt)
      : '';
  });
  readonly movingProjectIds = signal<ReadonlySet<string>>(new Set());
  readonly boardMoveInProgress = computed(() => this.movingProjectIds().size > 0);
  readonly boardColumnDefinitions = signal<ProjectBoardColumnDto[]>([]);
  readonly columnDialogOpen = signal(false);
  readonly editingBoardColumn = signal<ProjectBoardColumnDto | null>(null);
  readonly columnNameDraft = signal('');
  readonly columnColorDraft = signal('#4B7DD8');
  readonly columnFormError = signal('');
  readonly savingBoardColumn = signal(false);
  readonly pendingDeleteBoardColumn = signal<BoardColumn | null>(null);
  readonly deleteTargetColumnId = signal('');
  readonly deletingBoardColumn = signal(false);
  readonly reorderingBoardColumns = signal(false);

  private projectLoadPromise: Promise<void> | null = null;
  private refreshTimer: ReturnType<typeof window.setInterval> | null = null;
  private refreshMonitoringStarted = false;
  private readonly handleWindowFocus = () => void this.refreshProjectsWhenVisible();
  private readonly handleWindowOnline = () => void this.refreshProjectsWhenVisible();
  private readonly handleVisibilityChange = () => void this.refreshProjectsWhenVisible();

  private readonly rawProjects = signal<ProjectListItemDto[]>([]);
  readonly hasProjects = computed(() => this.rawProjects().length > 0);

  // Single filtered source of truth so List, Cards and Gantt never disagree on which projects are shown.
  readonly filteredProjects = computed(() => {
    const query = this.searchQuery().trim().toLocaleLowerCase('tr-TR');
    const type = this.activeType();

    return this.rawProjects().filter((dto) => {
      const matchesType = type === 'Tümü' || TYPE_LABELS[dto.type] === type;
      const haystack = `${dto.name} ${dto.managerName} ${dto.unit}`.toLocaleLowerCase('tr-TR');
      const matchesCompletion = this.showCompleted() || dto.status !== 'Completed';
      const matchesDelay = !this.delayedOnly() || dto.deviationDays < 0;
      const currentUser = this.authService.currentUser();
      const matchesAssignment = !this.assignedOnly() ||
        (dto.managerEmployeeId
          ? dto.managerEmployeeId === currentUser?.userId
          : dto.managerName === (currentUser?.displayName ?? ''));
      return matchesType && matchesCompletion && matchesDelay && matchesAssignment && (!query || haystack.includes(query));
    });
  });

  private readonly ganttRange = computed(() => this.computeGanttRange(this.filteredProjects()));
  readonly ganttMonths = computed<string[]>(() => this.ganttRange()?.months ?? []);
  readonly ganttRows = computed<GanttRow[]>(() => this.buildGanttRows(this.filteredProjects(), this.ganttRange()));
  readonly ganttGridMinWidth = computed(() => Math.max(1320, 240 + this.ganttMonths().length * 176));

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

  readonly projectTypes: Array<'Tümü' | ProjectType> = ['Tümü', 'Basit', 'Çoklu Birimli'];

  readonly projectColumns: ProjectColumn[] = [
    { key: 'manager', label: 'Yöneten' },
    { key: 'unit', label: 'Birim' },
    { key: 'status', label: 'Durum' },
    { key: 'progress', label: 'İlerleme' },
    { key: 'deviation', label: 'Sapma Oranı' },
    { key: 'budget', label: 'Bütçe' },
    { key: 'type', label: 'Proje Türü' },
    { key: 'start', label: 'Başlangıç Tarihi' },
    { key: 'end', label: 'Bitiş Tarihi' }
  ];

  readonly visibleColumns = signal<ReadonlySet<ProjectColumnKey>>(
    new Set(this.projectColumns.map((column) => column.key))
  );

  readonly boardColumns = signal<BoardColumn[]>([]);

  // The shared board is authoritative on the backend. This effect only projects the active filters over
  // that shared placement; drag/drop updates are persisted and then reloaded for every user.
  private readonly syncBoardColumnsWithFilters = effect(() => {
    this.boardColumnDefinitions();
    this.boardColumns.set(this.buildBoardColumns(this.filteredProjects().map((dto) => this.toProjectRow(dto))));
  });

  readonly boardColumnIds = computed(() => this.boardColumns().map((_, index) => this.boardColumnId(index)));
  readonly kanbanAnnouncement = signal('');

  readonly visibleRows = computed(() => {
    const sortKey = this.sortKey();
    const direction = this.sortDirection() === 'asc' ? 1 : -1;
    const filteredRows = this.filteredProjects().map((dto) => this.toProjectRow(dto));

    return sortKey
      ? filteredRows.sort((left, right) => this.compareRows(left, right, sortKey) * direction)
      : filteredRows;
  });

  readonly visibleColumnCount = computed(() => this.visibleColumns().size);

  readonly activeFilterCount = computed(() => {
    let count = 0;
    if (this.searchQuery().trim()) count++;
    if (this.activeType() !== 'Tümü') count++;
    if (!this.showCompleted()) count++;
    if (this.delayedOnly()) count++;
    if (this.assignedOnly()) count++;
    return count;
  });

  readonly totalCount = computed(() => this.rawProjects().length);
  readonly activeCount = computed(() => this.rawProjects().filter((p) => p.status === 'Active' || p.status === 'Draft').length);
  readonly completedCount = computed(() => this.rawProjects().filter((p) => p.status === 'Completed').length);

  async ngOnInit(): Promise<void> {
    this.startRefreshMonitoring();
    await this.loadProjects();
  }

  ngOnDestroy(): void {
    if (this.refreshTimer !== null) {
      window.clearInterval(this.refreshTimer);
      this.refreshTimer = null;
    }
    window.removeEventListener('focus', this.handleWindowFocus);
    window.removeEventListener('online', this.handleWindowOnline);
    document.removeEventListener('visibilitychange', this.handleVisibilityChange);
    this.refreshMonitoringStarted = false;
  }

  retryLoadProjects(): void {
    void this.loadProjects();
  }

  clearSearch(): void {
    this.searchQuery.set('');
  }

  clearAllFilters(): void {
    this.searchQuery.set('');
    this.activeType.set('Tümü');
    this.showCompleted.set(true);
    this.delayedOnly.set(false);
    this.assignedOnly.set(false);
  }

  private loadProjects(showLoadingState = true): Promise<void> {
    if (this.projectLoadPromise) {
      return this.projectLoadPromise;
    }

    if (showLoadingState) {
      this.loading.set(true);
      this.loadError.set(null);
    }

    const request = (async () => {
      try {
        const [projects, boardColumns] = await Promise.all([
          this.projectApi.search(),
          this.projectBoardApi.listColumns()
        ]);
        this.rawProjects.set(projects);
        this.boardColumnDefinitions.set(
          [...boardColumns].sort((left, right) => left.sortOrder - right.sortOrder)
        );
        this.lastUpdatedAt.set(new Date());
        this.backgroundRefreshFailed.set(false);
        this.loadError.set(null);
      } catch (error) {
        if (showLoadingState || !this.hasProjects()) {
          this.loadError.set(error instanceof Error ? error.message : 'Projeler yüklenemedi.');
        } else {
          this.backgroundRefreshFailed.set(true);
        }
      } finally {
        if (showLoadingState) {
          this.loading.set(false);
        }
      }
    })();

    this.projectLoadPromise = request.finally(() => {
      this.projectLoadPromise = null;
    });
    return this.projectLoadPromise;
  }

  private async loadGanttTasks(projectId: string, force = false): Promise<void> {
    if (this.ganttTaskLoadingIds().has(projectId) || (!force && this.ganttTaskGroups().has(projectId))) {
      return;
    }

    this.ganttTaskLoadingIds.update((ids) => new Set(ids).add(projectId));
    this.ganttTaskErrorIds.update((ids) => {
      const next = new Set(ids);
      next.delete(projectId);
      return next;
    });

    try {
      const groups = await this.taskApi.listByProject(projectId);
      this.ganttTaskGroups.update((current) => {
        const next = new Map(current);
        next.set(projectId, groups);
        return next;
      });
    } catch {
      this.ganttTaskErrorIds.update((ids) => new Set(ids).add(projectId));
    } finally {
      this.ganttTaskLoadingIds.update((ids) => {
        const next = new Set(ids);
        next.delete(projectId);
        return next;
      });
    }
  }

  openProject(project: Pick<ProjectRow, 'id'> | Pick<BoardCard, 'id'>): void {
    this.router.navigate(['/projects', project.id]);
  }

  openProjectFromListRowKeyboard(project: Pick<ProjectRow, 'id'>, event: Event): void {
    if (event.target !== event.currentTarget) {
      return;
    }

    event.preventDefault();
    this.openProject(project);
  }

  requestDeleteProject(project: ProjectRow | BoardCard, event?: Event): void {
    event?.stopPropagation();
    this.pendingDeleteProject.set(project);
  }

  cancelDeleteProject(): void {
    this.pendingDeleteProject.set(null);
  }

  async confirmDeleteProject(): Promise<void> {
    const project = this.pendingDeleteProject();
    if (!project || this.deletingProject()) {
      return;
    }

    this.deletingProject.set(true);
    try {
      await this.projectApi.delete(project.id);
      this.rawProjects.update((projects) => projects.filter((p) => p.id !== project.id));
      await this.reloadAfterMutation();
      this.toastService.success('Proje silindi.');
      this.pendingDeleteProject.set(null);
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Proje silinemedi.');
    } finally {
      this.deletingProject.set(false);
    }
  }

  openProjectCard(project: BoardCard): void {
    if (!this.suppressProjectOpen) {
      this.openProject(project);
    }
  }

  startProjectCardDrag(): void {
    this.suppressProjectOpen = true;
  }

  finishProjectCardDrag(): void {
    window.setTimeout(() => {
      this.suppressProjectOpen = false;
    });
  }

  createProject(): void {
    this.router.navigate(['/projects/new'], {
      queryParams: { from: this.activeView() }
    });
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

  boardColumnId(index: number): string {
    return `project-kanban-column-${index}`;
  }

  async dropProjectCard(event: CdkDragDrop<BoardCard[]>, targetColumnIndex: number): Promise<void> {
    if (!this.canManageProjects()) {
      return;
    }

    const sourceColumnIndex = this.boardColumns().findIndex(
      (column) => column.cards === event.previousContainer.data
    );

    if (sourceColumnIndex < 0 || this.boardMoveInProgress()) {
      return;
    }

    await this.moveProjectCard(sourceColumnIndex, targetColumnIndex, event.previousIndex, event.currentIndex);
  }

  async moveProjectCardByKeyboard(
    event: KeyboardEvent,
    columnIndex: number,
    cardIndex: number
  ): Promise<void> {
    if (!this.canManageProjects()) {
      return;
    }

    const key = event.key;

    if (key === 'ArrowUp' && cardIndex > 0) {
      event.preventDefault();
      await this.moveProjectCard(columnIndex, columnIndex, cardIndex, cardIndex - 1);
    } else if (key === 'ArrowDown' && cardIndex < this.boardColumns()[columnIndex].cards.length - 1) {
      event.preventDefault();
      await this.moveProjectCard(columnIndex, columnIndex, cardIndex, cardIndex + 1);
    } else if (event.altKey && key === 'ArrowLeft' && columnIndex > 0) {
      event.preventDefault();
      await this.moveProjectCard(
        columnIndex,
        columnIndex - 1,
        cardIndex,
        this.boardColumns()[columnIndex - 1].cards.length
      );
    } else if (event.altKey && key === 'ArrowRight' && columnIndex < this.boardColumns().length - 1) {
      event.preventDefault();
      await this.moveProjectCard(
        columnIndex,
        columnIndex + 1,
        cardIndex,
        this.boardColumns()[columnIndex + 1].cards.length
      );
    }
  }

  openCreateBoardColumnDialog(): void {
    this.editingBoardColumn.set(null);
    this.columnNameDraft.set('');
    this.columnColorDraft.set('#4B7DD8');
    this.columnFormError.set('');
    this.columnDialogOpen.set(true);
  }

  openEditBoardColumnDialog(column: BoardColumn, event?: Event): void {
    event?.stopPropagation();
    if (!column.id) {
      return;
    }
    const definition = this.boardColumnDefinitions().find(item => item.id === column.id);
    if (!definition) {
      return;
    }
    this.editingBoardColumn.set(definition);
    this.columnNameDraft.set(definition.name);
    this.columnColorDraft.set(definition.color);
    this.columnFormError.set('');
    this.columnDialogOpen.set(true);
  }

  closeBoardColumnDialog(): void {
    this.columnDialogOpen.set(false);
    this.columnFormError.set('');
  }

  async saveBoardColumn(): Promise<void> {
    if (this.savingBoardColumn()) {
      return;
    }
    const name = this.columnNameDraft().trim();
    if (!name) {
      this.columnFormError.set('Sütun adı zorunludur.');
      return;
    }

    this.savingBoardColumn.set(true);
    this.columnFormError.set('');
    try {
      const request = { name, color: this.columnColorDraft() };
      const editing = this.editingBoardColumn();
      if (editing) {
        await this.projectBoardApi.updateColumn(editing.id, request);
        this.toastService.success('Pano sütunu güncellendi.');
      } else {
        await this.projectBoardApi.createColumn(request);
        this.toastService.success(`${name} sütunu oluşturuldu.`);
      }
      this.closeBoardColumnDialog();
      await this.reloadAfterMutation();
    } catch (error) {
      this.columnFormError.set(error instanceof Error ? error.message : 'Sütun kaydedilemedi.');
    } finally {
      this.savingBoardColumn.set(false);
    }
  }

  requestDeleteBoardColumn(column: BoardColumn, event?: Event): void {
    event?.stopPropagation();
    if (!column.id || column.isProtected) {
      return;
    }
    this.pendingDeleteBoardColumn.set(column);
    this.deleteTargetColumnId.set('');
  }

  boardColumnProjectCount(columnId: string | null): number {
    return this.rawProjects().filter(project => project.boardColumnId === columnId).length;
  }

  cancelDeleteBoardColumn(): void {
    this.pendingDeleteBoardColumn.set(null);
    this.deleteTargetColumnId.set('');
  }

  async confirmDeleteBoardColumn(): Promise<void> {
    const column = this.pendingDeleteBoardColumn();
    if (!column?.id || this.deletingBoardColumn()) {
      return;
    }
    if (this.boardColumnProjectCount(column.id) > 0 && !this.deleteTargetColumnId()) {
      this.toastService.error('Sütundaki projeler için bir hedef sütun seçmelisiniz.');
      return;
    }

    this.deletingBoardColumn.set(true);
    try {
      await this.projectBoardApi.archiveColumn(column.id, this.deleteTargetColumnId() || null);
      this.toastService.success('Pano sütunu kaldırıldı.');
      this.cancelDeleteBoardColumn();
      await this.reloadAfterMutation();
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Sütun kaldırılamadı.');
    } finally {
      this.deletingBoardColumn.set(false);
    }
  }

  async moveBoardColumn(columnIndex: number, direction: -1 | 1, event?: Event): Promise<void> {
    event?.stopPropagation();
    if (this.reorderingBoardColumns()) {
      return;
    }

    const definitions = [...this.boardColumnDefinitions()];
    const targetIndex = columnIndex + direction;
    if (columnIndex < 0 || targetIndex < 0 || targetIndex >= definitions.length) {
      return;
    }

    [definitions[columnIndex], definitions[targetIndex]] = [definitions[targetIndex], definitions[columnIndex]];
    this.boardColumnDefinitions.set(definitions.map((column, index) => ({ ...column, sortOrder: index })));
    this.reorderingBoardColumns.set(true);
    try {
      await this.projectBoardApi.reorderColumns(definitions.map(column => column.id));
      await this.reloadAfterMutation();
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Sütun sırası güncellenemedi.');
      await this.reloadAfterMutation();
    } finally {
      this.reorderingBoardColumns.set(false);
    }
  }

  toggleSearch(): void {
    this.searchOpen.update((open) => !open);
    if (this.searchOpen()) {
      this.filtersOpen.set(false);
      this.columnsOpen.set(false);
      this.profileOpen.set(false);
    } else {
      this.searchQuery.set('');
    }
  }

  toggleFilters(): void {
    this.filtersOpen.update((open) => !open);
    this.columnsOpen.set(false);
    this.profileOpen.set(false);
  }

  toggleColumns(): void {
    this.columnsOpen.update((open) => !open);
    this.filtersOpen.set(false);
    this.profileOpen.set(false);
  }

  isColumnVisible(key: ProjectColumnKey): boolean {
    return this.visibleColumns().has(key);
  }

  toggleColumn(key: ProjectColumnKey): void {
    const next = new Set(this.visibleColumns());
    if (next.has(key) && next.size > 1) {
      next.delete(key);
    } else {
      next.add(key);
    }
    this.visibleColumns.set(next);
  }

  sortBy(key: ProjectSortKey): void {
    if (this.sortKey() === key) {
      this.sortDirection.update((direction) => direction === 'asc' ? 'desc' : 'asc');
      return;
    }
    this.sortKey.set(key);
    this.sortDirection.set('asc');
  }

  sortIndicator(key: ProjectSortKey): string {
    if (this.sortKey() !== key) return '⇅';
    return this.sortDirection() === 'asc' ? '↑' : '↓';
  }

  toggleProfile(): void {
    this.profileOpen.update((open) => !open);
    this.filtersOpen.set(false);
    this.columnsOpen.set(false);
  }

  async toggleGanttRow(projectId: string): Promise<void> {
    const next = new Set(this.expandedGanttRows());
    if (next.has(projectId)) {
      next.delete(projectId);
      this.expandedGanttRows.set(next);
      return;
    }

    next.add(projectId);
    this.expandedGanttRows.set(next);
    // Her açılışta zorla yeniden çek: arşivleme/geri yükleme gibi değişiklikler proje detay sayfasında
    // yapılıyor, bu sayfa navigasyon olmadan açık kalırsa önbellek bu değişiklikleri hiç görmüyordu.
    await this.loadGanttTasks(projectId, true);
  }

  async retryGanttTasks(projectId: string, event?: Event): Promise<void> {
    event?.stopPropagation();
    await this.loadGanttTasks(projectId, true);
  }

  setView(view: 'list' | 'cards' | 'gantt'): void {
    this.activeView.set(view);
    const url = view === 'list' ? '/projects' : `/projects?view=${view}`;
    window.history.replaceState({}, '', url);
  }

  private toProjectRow(dto: ProjectListItemDto): ProjectRow {
    const deviationTone: ProjectRow['deviationTone'] =
      dto.deviationDays < 0 ? 'bad' : dto.deviationDays > 0 ? 'good' : 'neutral';
    const progressTone: ProgressTone =
      dto.status === 'Cancelled' ? 'slate' : dto.progressPercent >= 70 ? 'green' : dto.progressPercent >= 40 ? 'orange' : dto.progressPercent > 0 ? 'blue' : 'slate';

    return {
      id: dto.id,
      unitDepartmentId: dto.unitDepartmentId ?? null,
      boardColumnId: dto.boardColumnId,
      boardPosition: dto.boardPosition,
      updatedAtUtc: dto.updatedAtUtc,
      name: dto.name,
      manager: dto.managerName,
      unit: dto.unit,
      progress: dto.progressPercent,
      progressTone,
      deviation: `${dto.deviationDays > 0 ? '+' : ''}${dto.deviationDays} Gün`,
      deviationTone,
      budget: this.formatCurrency(dto.budget, dto.currency),
      type: TYPE_LABELS[dto.type],
      start: this.formatDate(dto.startDate),
      end: this.formatDate(dto.endDate),
      startIso: dto.startDate,
      endIso: dto.endDate,
      status: dto.status,
      statusLabel: STATUS_LABELS[dto.status]
    };
  }

  private toBoardCard(row: ProjectRow): BoardCard {
    return {
      id: row.id,
      status: row.status,
      boardColumnId: row.boardColumnId,
      updatedAtUtc: row.updatedAtUtc,
      name: row.name,
      manager: row.manager,
      unit: row.unit,
      type: row.type,
      budget: row.budget,
      deviation: row.deviation,
      deviationTone: row.deviationTone,
      progress: row.progress,
      progressTone: row.progressTone
    };
  }

  private buildBoardColumns(rows: ProjectRow[]): BoardColumn[] {
    const orderedRows = [...rows].sort((left, right) =>
      left.boardPosition - right.boardPosition || left.name.localeCompare(right.name, 'tr-TR')
    );
    const definitions = this.boardColumnDefinitions();
    const activeColumnIds = new Set(definitions.map(column => column.id));
    const columns: BoardColumn[] = definitions.map(column => ({
      key: column.id,
      id: column.id,
      title: column.name,
      color: column.color,
      isUnassigned: false,
      isProtected: column.isProtected,
      cards: orderedRows
        .filter(row => row.boardColumnId === column.id)
        .map(row => this.toBoardCard(row))
    }));
    const unassignedCards = orderedRows
      .filter(row => row.boardColumnId === null || !activeColumnIds.has(row.boardColumnId))
      .map(row => this.toBoardCard(row));

    if (unassignedCards.length > 0) {
      columns.push({
        key: 'unassigned',
        id: null,
        title: 'Gruplanmamış',
        color: '#98A1B3',
        isUnassigned: true,
        isProtected: true,
        cards: unassignedCards
      });
    }

    return columns;
  }

  private computeGanttRange(projects: ProjectListItemDto[]): { rangeStart: Date; totalMs: number; months: string[] } | null {
    if (!projects.length) {
      return null;
    }

    const starts = projects.map((p) => new Date(p.startDate).getTime());
    const ends = projects.map((p) => new Date(p.endDate).getTime());
    const rangeStart = new Date(Math.min(...starts));
    rangeStart.setDate(1);
    const rangeEndSource = new Date(Math.max(...ends));
    const monthCount = Math.max(
      1,
      (rangeEndSource.getFullYear() - rangeStart.getFullYear()) * 12 + (rangeEndSource.getMonth() - rangeStart.getMonth()) + 1
    );
    const rangeEnd = new Date(rangeStart);
    rangeEnd.setMonth(rangeEnd.getMonth() + monthCount);
    const totalMs = rangeEnd.getTime() - rangeStart.getTime();

    const monthFormatter = new Intl.DateTimeFormat('tr-TR', { month: 'long', year: 'numeric' });
    const months: string[] = [];
    for (let i = 0; i < monthCount; i++) {
      const month = new Date(rangeStart);
      month.setMonth(month.getMonth() + i);
      months.push(monthFormatter.format(month));
    }

    return { rangeStart, totalMs, months };
  }

  private buildGanttRows(
    projects: ProjectListItemDto[],
    range: { rangeStart: Date; totalMs: number; months: string[] } | null
  ): GanttRow[] {
    if (!range) {
      return [];
    }

    const taskGroups = this.ganttTaskGroups();
    const loadingIds = this.ganttTaskLoadingIds();
    const errorIds = this.ganttTaskErrorIds();
    return projects.map((project, index) => {
      const start = new Date(project.startDate).getTime();
      const end = new Date(project.endDate).getTime();
      const startPercent = Math.max(0, Math.min(100, ((start - range.rangeStart.getTime()) / range.totalMs) * 100));
      const widthPercent = Math.max(2, Math.min(100 - startPercent, ((end - start) / range.totalMs) * 100));
      const tasksLoaded = taskGroups.has(project.id);

      return {
        id: project.id,
        name: project.name,
        start: startPercent,
        width: widthPercent,
        color: GANTT_BAR_COLORS[index % GANTT_BAR_COLORS.length],
        label: `${this.formatDate(project.startDate)} - ${this.formatDate(project.endDate)}`,
        tasks: tasksLoaded ? this.buildGanttTasks(taskGroups.get(project.id) ?? [], range) : [],
        tasksLoaded,
        tasksLoading: loadingIds.has(project.id),
        tasksLoadFailed: errorIds.has(project.id)
      };
    });
  }

  private buildGanttTasks(
    groups: TaskGroupDto[],
    range: { rangeStart: Date; totalMs: number; months: string[] }
  ): GanttTask[] {
    return [...groups]
      .sort((left, right) => left.timelineSortOrder - right.timelineSortOrder)
      .flatMap((group) => group.tasks.map((task) => this.toGanttTask(task, group.title, range)));
  }

  private toGanttTask(
    task: TaskItemDto,
    groupTitle: string,
    range: { rangeStart: Date; totalMs: number; months: string[] }
  ): GanttTask {
    const startValue = task.startDateUtc ? new Date(task.startDateUtc).getTime() : null;
    const dueValue = task.dueDateUtc ? new Date(task.dueDateUtc).getTime() : null;
    const scheduled = startValue !== null || dueValue !== null;
    const rangeStart = range.rangeStart.getTime();
    const oneDayMs = 24 * 60 * 60 * 1000;
    const taskStart = startValue ?? dueValue ?? rangeStart;
    const taskEnd = Math.max(taskStart + oneDayMs, dueValue ?? taskStart + oneDayMs);
    const startPercent = Math.max(0, Math.min(100, ((taskStart - rangeStart) / range.totalMs) * 100));
    const widthPercent = scheduled
      ? Math.max(1.2, Math.min(100 - startPercent, ((taskEnd - taskStart) / range.totalMs) * 100))
      : 0;
    const dateLabel = !scheduled
      ? 'Tarih planlanmadı'
      : task.startDateUtc && task.dueDateUtc
        ? `${this.formatDate(task.startDateUtc)} - ${this.formatDate(task.dueDateUtc)}`
        : this.formatDate(task.startDateUtc ?? task.dueDateUtc!);

    return {
      id: task.id,
      name: task.title,
      start: startPercent,
      width: widthPercent,
      label: dateLabel,
      groupTitle,
      depth: task.depth,
      scheduled,
      status: task.status
    };
  }

  ganttTaskStatusLabel(status: KanbanStatus): string {
    return status === 'Done' ? 'Tamamlandı' : status === 'InProgress' ? 'Devam Ediyor' : 'Bekliyor';
  }

  private formatCurrency(amount: number, currency: string): string {
    const formatted = new Intl.NumberFormat('tr-TR').format(amount);
    return currency === 'TRY' ? `${formatted} ₺` : `${formatted} ${currency}`;
  }

  private formatDate(isoDate: string): string {
    const date = new Date(isoDate);
    return new Intl.DateTimeFormat('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
  }

  private async moveProjectCard(
    sourceColumnIndex: number,
    targetColumnIndex: number,
    sourceCardIndex: number,
    targetCardIndex: number
  ): Promise<void> {
    if (sourceColumnIndex === targetColumnIndex && sourceCardIndex === targetCardIndex) {
      return;
    }

    const columns = this.boardColumns().map((column) => ({
      ...column,
      cards: [...column.cards]
    }));
    const sourceColumn = columns[sourceColumnIndex];
    const targetColumn = columns[targetColumnIndex];
    const [card] = sourceColumn.cards.splice(sourceCardIndex, 1);

    if (!card) {
      return;
    }

    targetColumn.cards.splice(targetCardIndex, 0, card);
    this.boardColumns.set(columns);

    this.movingProjectIds.update(ids => new Set(ids).add(card.id));
    try {
      const beforeProjectId = targetColumn.cards[targetCardIndex + 1]?.id ?? null;
      const afterProjectId = targetColumn.cards[targetCardIndex - 1]?.id ?? null;
      await this.projectBoardApi.moveCard(card.id, {
        columnId: targetColumn.id,
        beforeProjectId,
        afterProjectId,
        expectedUpdatedAtUtc: card.updatedAtUtc
      });
      this.kanbanAnnouncement.set(
        `${card.name}, ${targetColumn.title} sütununda ${targetCardIndex + 1}. sıraya taşındı.`
      );
      await this.reloadAfterMutation();
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Proje kartı taşınamadı.');
      await this.reloadAfterMutation();
    } finally {
      this.movingProjectIds.update(ids => {
        const next = new Set(ids);
        next.delete(card.id);
        return next;
      });
    }
  }

  private compareRows(left: ProjectRow, right: ProjectRow, key: ProjectSortKey): number {
    if (key === 'progress') return left.progress - right.progress;
    if (key === 'budget') return this.numericValue(left.budget) - this.numericValue(right.budget);
    if (key === 'deviation') return this.numericValue(left.deviation) - this.numericValue(right.deviation);
    if (key === 'status') return left.statusLabel.localeCompare(right.statusLabel, 'tr-TR');
    // "start"/"end" formatlanmış "GG.AA.YYYY" metnini değil, ham ISO ("YYYY-MM-DD") tarihini
    // karşılaştırır — aksi halde gün önce geldiği için sıralama kronolojik olmuyordu.
    if (key === 'start') return left.startIso.localeCompare(right.startIso);
    if (key === 'end') return left.endIso.localeCompare(right.endIso);
    return String(left[key]).localeCompare(String(right[key]), 'tr-TR', { numeric: true });
  }

  private numericValue(value: string): number {
    const sign = value.trim().startsWith('-') ? -1 : 1;
    const numeric = Number(value.replace(/[^0-9]/g, ''));
    return Number.isFinite(numeric) ? numeric * sign : 0;
  }

  private startRefreshMonitoring(): void {
    if (this.refreshMonitoringStarted) {
      return;
    }

    this.refreshMonitoringStarted = true;
    window.addEventListener('focus', this.handleWindowFocus);
    window.addEventListener('online', this.handleWindowOnline);
    document.addEventListener('visibilitychange', this.handleVisibilityChange);
    this.refreshTimer = window.setInterval(
      () => void this.refreshProjectsWhenVisible(),
      ProjectsPage.REFRESH_INTERVAL_MS
    );
  }

  private async refreshProjectsWhenVisible(): Promise<void> {
    if (document.visibilityState === 'visible' && navigator.onLine !== false) {
      await this.loadProjects(false);
    }
  }

  private async reloadAfterMutation(): Promise<void> {
    const currentLoad = this.projectLoadPromise;
    if (currentLoad) {
      await currentLoad;
    }
    await this.loadProjects(false);
  }

}
