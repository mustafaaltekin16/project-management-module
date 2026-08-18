import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from '../../../shared/auth/auth.service';
import { ProjectBoardApiService } from '../data/project-board-api.service';
import { MoveProjectBoardCardRequest, ProjectBoardColumnDto } from '../data/project-board-api.models';
import { ProjectApiService } from '../data/project-api.service';
import { ProjectListItemDto } from '../data/project-api.models';
import { TaskApiService } from '../data/task-api.service';
import { TaskGroupDto } from '../data/task-api.models';
import { ProjectsPage } from './projects-page';

describe('ProjectsPage', () => {
  const boardColumns: ProjectBoardColumnDto[] = [
    { id: 'column-new', name: 'Yeni Projeler', color: '#4B7DD8', sortOrder: 0, updatedAtUtc: '2026-07-29T08:00:00Z', isProtected: true },
    { id: 'column-active', name: 'Devam Edenler', color: '#2F9E68', sortOrder: 1, updatedAtUtc: '2026-07-29T08:00:00Z', isProtected: true },
    { id: 'column-completed', name: 'Tamamlananlar', color: '#697386', sortOrder: 2, updatedAtUtc: '2026-07-29T08:00:00Z', isProtected: true }
  ];

  const projects: ProjectListItemDto[] = [
    {
      id: 'project-1',
      name: 'Geciken Küçük Proje',
      managerName: 'Ayşe Yılmaz',
      unitDepartmentId: 'department-it',
      unit: 'Bilgi Teknolojileri',
      progressPercent: 35,
      deviationDays: -3,
      budget: 23_000,
      currency: 'TRY',
      type: 'Simple',
      status: 'Active',
      startDate: '2026-01-01',
      endDate: '2026-05-01',
      updatedAtUtc: '2026-07-29T08:00:01Z',
      boardColumnId: 'column-active',
      boardPosition: 1024
    },
    {
      id: 'project-2',
      name: 'Büyük Proje',
      managerName: 'Mehmet Kaya',
      unitDepartmentId: 'department-operations',
      unit: 'Operasyon',
      progressPercent: 60,
      deviationDays: 2,
      budget: 9_800_000,
      currency: 'TRY',
      type: 'MultiUnit',
      status: 'Active',
      startDate: '2026-02-01',
      endDate: '2026-11-01',
      updatedAtUtc: '2026-07-29T08:00:02Z',
      boardColumnId: 'column-active',
      boardPosition: 2048
    },
    {
      id: 'project-3',
      name: 'Tamamlanan Proje',
      managerName: 'Can Demir',
      unitDepartmentId: 'department-finance',
      unit: 'Finans',
      progressPercent: 100,
      deviationDays: 0,
      budget: 450_000,
      currency: 'TRY',
      type: 'FeasibilityBased',
      status: 'Completed',
      startDate: '2025-04-01',
      endDate: '2026-01-01',
      updatedAtUtc: '2026-07-29T08:00:03Z',
      boardColumnId: 'column-completed',
      boardPosition: 1024
    },
    {
      id: 'project-4',
      name: 'İkinci BT Projesi',
      managerName: 'Ayşe Yılmaz',
      unitDepartmentId: 'department-it',
      unit: 'Bilgi Teknolojileri',
      progressPercent: 15,
      deviationDays: 1,
      budget: 100_000,
      currency: 'TRY',
      type: 'Simple',
      status: 'Active',
      startDate: '2026-03-01',
      endDate: '2026-08-01',
      updatedAtUtc: '2026-07-29T08:00:04Z',
      boardColumnId: 'column-active',
      boardPosition: 3072
    },
    {
      id: 'project-5',
      name: 'Yeni Taslak Proje',
      managerName: 'Ayşe Yılmaz',
      unitDepartmentId: 'department-it',
      unit: 'Bilgi Teknolojileri',
      progressPercent: 0,
      deviationDays: 0,
      budget: 75_000,
      currency: 'TRY',
      type: 'Simple',
      status: 'Draft',
      startDate: '2026-08-01',
      endDate: '2026-10-01',
      updatedAtUtc: '2026-07-29T08:00:05Z',
      boardColumnId: 'column-new',
      boardPosition: 1024
    }
  ];

  let projectState: ProjectListItemDto[] = [];
  let columnState: ProjectBoardColumnDto[] = [];
  const projectApi = {
    search: vi.fn(),
    delete: vi.fn().mockResolvedValue(undefined)
  };
  const projectBoardApi = {
    listColumns: vi.fn(),
    createColumn: vi.fn(),
    updateColumn: vi.fn(),
    reorderColumns: vi.fn(),
    archiveColumn: vi.fn(),
    moveCard: vi.fn()
  };
  const taskApi = {
    listByProject: vi.fn()
  };
  const currentUser = signal({
    userId: 'admin-user',
    displayName: 'Test Admin',
    roles: ['Admin']
  });
  const authService = {
    currentUser,
    hasAnyRole: vi.fn((roles: string[]) => roles.some((role) => currentUser().roles.includes(role))),
    logout: vi.fn()
  };
  const createdComponents: ProjectsPage[] = [];

  beforeEach(async () => {
    localStorage.clear();
    currentUser.set({ userId: 'admin-user', displayName: 'Test Admin', roles: ['Admin'] });
    projectState = projects.map(project => ({ ...project }));
    columnState = boardColumns.map(column => ({ ...column }));
    projectApi.search.mockReset().mockImplementation(async () => projectState.map(project => ({ ...project })));
    projectApi.delete.mockReset().mockResolvedValue(undefined);
    taskApi.listByProject.mockReset().mockResolvedValue([]);
    projectBoardApi.listColumns.mockReset().mockImplementation(async () => columnState.map(column => ({ ...column })));
    projectBoardApi.createColumn.mockReset().mockImplementation(async (request: { name: string; color: string }) => {
      const created = {
        id: `column-${columnState.length + 1}`,
        name: request.name,
        color: request.color,
        sortOrder: columnState.length,
        updatedAtUtc: '2026-07-29T09:00:00Z',
        isProtected: false
      };
      columnState.push(created);
      return created;
    });
    projectBoardApi.updateColumn.mockReset().mockResolvedValue(undefined);
    projectBoardApi.reorderColumns.mockReset().mockResolvedValue(undefined);
    projectBoardApi.archiveColumn.mockReset().mockResolvedValue(undefined);
    projectBoardApi.moveCard.mockReset().mockImplementation(
      async (projectId: string, request: MoveProjectBoardCardRequest) => {
        const moving = projectState.find(project => project.id === projectId);
        if (!moving) return;
        const target = projectState
          .filter(project => project.id !== projectId && project.boardColumnId === request.columnId)
          .sort((left, right) => left.boardPosition - right.boardPosition);
        let targetIndex = target.length;
        if (request.beforeProjectId) targetIndex = target.findIndex(project => project.id === request.beforeProjectId);
        else if (request.afterProjectId) targetIndex = target.findIndex(project => project.id === request.afterProjectId) + 1;
        target.splice(Math.max(0, targetIndex), 0, moving);
        target.forEach((project, index) => {
          project.boardColumnId = request.columnId;
          project.boardPosition = (index + 1) * 1024;
          project.updatedAtUtc = `2026-07-29T09:00:${String(index).padStart(2, '0')}Z`;
        });
      }
    );

    await TestBed.configureTestingModule({
      imports: [ProjectsPage],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: ProjectApiService, useValue: projectApi },
        { provide: ProjectBoardApiService, useValue: projectBoardApi },
        { provide: TaskApiService, useValue: taskApi },
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();
  });

  afterEach(() => {
    createdComponents.forEach((component) => component.ngOnDestroy());
    createdComponents.length = 0;
    vi.useRealTimers();
  });

  async function createComponent(): Promise<ProjectsPage> {
    const component = TestBed.runInInjectionContext(() => new ProjectsPage());
    createdComponents.push(component);
    await component.ngOnInit();
    TestBed.flushEffects();
    return component;
  }

  it('filters the list to delayed projects', async () => {
    const component = await createComponent();

    component.delayedOnly.set(true);

    expect(component.visibleRows().length).toBeGreaterThan(0);
    expect(component.visibleRows().every((row) => row.deviationTone === 'bad')).toBe(true);
  });

  it('offers only project types that can be created from the new project form', async () => {
    const component = await createComponent();

    expect(component.projectTypes).toEqual(['Tümü', 'Basit', 'Çoklu Birimli']);
  });

  it('sorts project budgets in both directions', async () => {
    const component = await createComponent();

    component.sortBy('budget');
    const ascending = component.visibleRows().map((row) => row.name);
    component.sortBy('budget');
    const descending = component.visibleRows().map((row) => row.name);

    expect(ascending[0]).toBe('Geciken Küçük Proje');
    expect(descending[0]).toBe('Büyük Proje');
  });

  it('sorts by start/end date chronologically instead of by day-of-month', async () => {
    // Ağustos'un 10'u, Eylül'ün 2'sinden önce gelir — ama eski "GG.AA.YYYY" metin
    // karşılaştırması günü önce kıyasladığı için tam tersini üretiyordu.
    projectApi.search.mockResolvedValue([
      { ...projects[0], id: 'p-late', name: 'Geç Başlayan', startDate: '2026-09-02', endDate: '2026-09-02' },
      { ...projects[0], id: 'p-early', name: 'Erken Başlayan', startDate: '2026-08-10', endDate: '2026-08-10' }
    ]);
    const component = await createComponent();

    component.sortBy('start');

    expect(component.visibleRows().map((row) => row.name)).toEqual(['Erken Başlayan', 'Geç Başlayan']);
  });

  it('opens project details from a focused list row with the keyboard', async () => {
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const component = await createComponent();
    const row = component.visibleRows()[0];
    const rowElement = document.createElement('tr');
    const event = {
      target: rowElement,
      currentTarget: rowElement,
      preventDefault: vi.fn()
    } as unknown as Event;

    component.openProjectFromListRowKeyboard(row, event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/projects', row.id]);
  });

  it('keeps at least one optional column visible', async () => {
    const component = await createComponent();

    for (const column of component.projectColumns) component.toggleColumn(column.key);

    expect(component.visibleColumnCount()).toBe(1);
  });

  it('moves a project card to another shared column with the keyboard shortcut', async () => {
    const component = await createComponent();
    const sourceColumnIndex = component.boardColumns().findIndex(column => column.id === 'column-new');
    const event = new KeyboardEvent('keydown', { key: 'ArrowRight', altKey: true });

    await component.moveProjectCardByKeyboard(event, sourceColumnIndex, 0);

    expect(projectBoardApi.moveCard).toHaveBeenCalledWith(
      'project-5',
      expect.objectContaining({ columnId: 'column-active' })
    );
    expect(component.boardColumns().find(column => column.id === 'column-active')?.cards)
      .toContainEqual(expect.objectContaining({ id: 'project-5' }));
  });

  it('moves a card between Kanban columns with drag and drop', async () => {
    const component = await createComponent();
    const sourceColumnIndex = component.boardColumns().findIndex(column => column.id === 'column-new');
    const targetColumnIndex = component.boardColumns().findIndex(column => column.id === 'column-active');
    const sourceCards = component.boardColumns()[sourceColumnIndex].cards;

    await component.dropProjectCard({
      previousContainer: { data: sourceCards },
      previousIndex: 0,
      currentIndex: 0
    } as never, targetColumnIndex);

    expect(projectBoardApi.moveCard).toHaveBeenCalledWith(
      'project-5',
      expect.objectContaining({ columnId: 'column-active' })
    );
    expect(component.boardColumns()[targetColumnIndex].cards[0].id).toBe('project-5');
  });

  it('persists project order inside the same shared column', async () => {
    const component = await createComponent();
    const columnIndex = component.boardColumns().findIndex(column => column.id === 'column-active');
    const firstCard = component.boardColumns()[columnIndex].cards[0];
    const count = component.boardColumns()[columnIndex].cards.length;
    const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });

    await component.moveProjectCardByKeyboard(event, columnIndex, 0);

    expect(projectBoardApi.moveCard).toHaveBeenCalledWith(
      firstCard.id,
      expect.objectContaining({ columnId: 'column-active' })
    );
    expect(component.boardColumns()[columnIndex].cards[1].id).toBe(firstCard.id);
    expect(component.boardColumns()[columnIndex].cards.length).toBe(count);
  });

  it('shows the persisted shared order to the next page instance', async () => {
    const component = await createComponent();
    const columnIndex = component.boardColumns().findIndex(column => column.id === 'column-active');
    const firstCardId = component.boardColumns()[columnIndex].cards[0].id;

    await component.moveProjectCardByKeyboard(
      new KeyboardEvent('keydown', { key: 'ArrowDown' }),
      columnIndex,
      0
    );

    const reloaded = await createComponent();
    const reloadedColumn = reloaded.boardColumns().find((column) =>
      column.cards.some((card) => card.id === firstCardId)
    );

    expect(reloadedColumn?.cards[1].id).toBe(firstCardId);
  });

  it('refreshes project data every 30 seconds while the page is visible', async () => {
    vi.useFakeTimers();
    await createComponent();

    await vi.advanceTimersByTimeAsync(30_000);

    expect(projectApi.search).toHaveBeenCalledTimes(2);
    expect(projectBoardApi.listColumns).toHaveBeenCalledTimes(2);
  });

  it('creates a custom shared board column', async () => {
    const component = await createComponent();
    component.openCreateBoardColumnDialog();
    component.columnNameDraft.set('Oteller');
    component.columnColorDraft.set('#B66A3C');

    await component.saveBoardColumn();
    TestBed.flushEffects();

    expect(projectBoardApi.createColumn).toHaveBeenCalledWith({ name: 'Oteller', color: '#B66A3C' });
    expect(component.boardColumns().some(column => column.title === 'Oteller')).toBe(true);
  });

  it('keeps the shared board read-only for a normal member', async () => {
    currentUser.set({ userId: 'member-user', displayName: 'Test Üye', roles: ['Member'] });
    const component = await createComponent();
    const sourceColumnIndex = component.boardColumns().findIndex(column => column.cards.length > 0);

    await component.moveProjectCardByKeyboard(
      new KeyboardEvent('keydown', { key: 'ArrowRight', altKey: true }),
      sourceColumnIndex,
      0
    );

    expect(component.canManageProjects()).toBe(false);
    expect(projectBoardApi.moveCard).not.toHaveBeenCalled();
  });

  it('loads and positions project tasks when a Gantt row is expanded', async () => {
    const taskGroups: TaskGroupDto[] = [{
      id: 'group-1',
      projectId: 'project-1',
      workPackageId: null,
      processType: null,
      timelineSortOrder: 0,
      title: 'Hazırlık',
      subtitle: '',
      createdAtUtc: '2026-01-01T08:00:00Z',
      tasks: [{
        id: 'task-1',
        title: 'Teknik hazırlık',
        assigneeName: 'Ayşe Yılmaz',
        assigneeEmployeeId: null,
        department: 'Bilgi Teknolojileri',
        effortHours: 12,
        depth: 0,
        isMainTask: true,
        dependsOnTaskId: null,
        status: 'InProgress',
        isAiGenerated: false,
        comments: [],
        createdAtUtc: '2026-01-02T08:00:00Z',
        updatedAtUtc: null,
        startDateUtc: '2026-01-10',
        dueDateUtc: '2026-02-15',
        category: null,
        description: null,
        completedAtUtc: null,
        completedBy: null
      }]
    }];
    taskApi.listByProject.mockResolvedValue(taskGroups);
    const component = await createComponent();

    await component.toggleGanttRow('project-1');

    const row = component.ganttRows().find((item) => item.id === 'project-1');
    expect(taskApi.listByProject).toHaveBeenCalledWith('project-1');
    expect(row?.tasksLoaded).toBe(true);
    expect(row?.tasks).toEqual([
      expect.objectContaining({ id: 'task-1', name: 'Teknik hazırlık', scheduled: true, status: 'InProgress' })
    ]);
    expect(component.expandedGanttRows().has('project-1')).toBe(true);
  });

  it('refetches Gantt tasks on every re-expand so archived tasks disappear without a page reload', async () => {
    const withArchivable: TaskGroupDto[] = [{
      id: 'group-1',
      projectId: 'project-1',
      workPackageId: null,
      processType: null,
      timelineSortOrder: 0,
      title: 'Hazırlık',
      subtitle: '',
      createdAtUtc: '2026-01-01T08:00:00Z',
      tasks: [
        {
          id: 'task-1',
          title: 'Kalıcı görev',
          assigneeName: 'Ayşe Yılmaz',
          assigneeEmployeeId: null,
          department: 'Bilgi Teknolojileri',
          effortHours: 12,
          depth: 0,
          isMainTask: true,
          dependsOnTaskId: null,
          status: 'InProgress',
          isAiGenerated: false,
          comments: [],
          createdAtUtc: '2026-01-02T08:00:00Z',
          updatedAtUtc: null,
          startDateUtc: '2026-01-10',
          dueDateUtc: '2026-02-15',
          category: null,
          description: null,
          completedAtUtc: null,
          completedBy: null
        },
        {
          id: 'task-2',
          title: 'Test amaçlı iş paketi',
          assigneeName: 'Ayşe Yılmaz',
          assigneeEmployeeId: null,
          department: 'Bilgi Teknolojileri',
          effortHours: 4,
          depth: 0,
          isMainTask: true,
          dependsOnTaskId: null,
          status: 'Todo',
          isAiGenerated: false,
          comments: [],
          createdAtUtc: '2026-01-02T08:00:00Z',
          updatedAtUtc: null,
          startDateUtc: '2026-01-10',
          dueDateUtc: '2026-02-15',
          category: null,
          description: null,
          completedAtUtc: null,
          completedBy: null
        }
      ]
    }];
    const afterArchiving: TaskGroupDto[] = [{ ...withArchivable[0], tasks: [withArchivable[0].tasks[0]] }];
    taskApi.listByProject
      .mockResolvedValueOnce(withArchivable)
      .mockResolvedValueOnce(afterArchiving);
    const component = await createComponent();

    await component.toggleGanttRow('project-1');
    expect(component.ganttRows().find((item) => item.id === 'project-1')?.tasks).toHaveLength(2);

    await component.toggleGanttRow('project-1');
    await component.toggleGanttRow('project-1');

    expect(taskApi.listByProject).toHaveBeenCalledTimes(2);
    expect(component.ganttRows().find((item) => item.id === 'project-1')?.tasks).toHaveLength(1);
  });
});
