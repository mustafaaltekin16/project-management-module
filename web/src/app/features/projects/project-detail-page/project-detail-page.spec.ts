import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TaskApiService } from '../data/task-api.service';
import { ProjectDetailPage } from './project-detail-page';

describe('ProjectDetailPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectDetailPage],
      providers: [provideHttpClient(), provideRouter([])]
    }).compileComponents();
  });

  it('formats checklist and table template values for the project detail', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());

    const checklist = component.formatTemplateValue({
      contentType: 'checklist',
      value: JSON.stringify(['Güvenlik', 'Bütçe'])
    } as never);
    const table = component.formatTemplateValue({
      contentType: 'table',
      value: JSON.stringify({ '0:0': 'Kalem A', '0:1': '125.000 ₺' })
    } as never);

    expect(checklist).toBe('Güvenlik • Bütçe');
    expect(table).toBe('Kalem A | 125.000 ₺');
  });

  it('shows the AI work package tab only when its project component is enabled', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());

    component.project.update((project) => ({
      ...project,
      enabledComponents: ['description', 'tasks', 'documents', 'flow']
    }));
    expect(component.tabs().map((tab) => tab.id)).not.toContain('ai');

    component.project.update((project) => ({
      ...project,
      enabledComponents: [...project.enabledComponents, 'ai']
    }));
    expect(component.tabs().map((tab) => tab.id)).toContain('ai');
  });

  it('shows the project guide only when the chat component was selected during creation', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());

    component.project.update((project) => ({
      ...project,
      enabledComponents: ['description', 'tasks', 'documents', 'flow']
    }));
    expect(component.tabs().map((tab) => tab.id)).not.toContain('assistant');

    component.project.update((project) => ({
      ...project,
      enabledComponents: [...project.enabledComponents, 'chat']
    }));
    expect(component.tabs().find((tab) => tab.id === 'assistant')?.label).toBe('Proje Rehberi');
  });

  it('orders selected work tabs as tasks, AI work package, project guide and documents', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());

    component.project.update((project) => ({
      ...project,
      enabledComponents: ['description', 'tasks', 'ai', 'chat', 'documents']
    }));

    expect(component.tabs().map((tab) => tab.id)).toEqual([
      'overview', 'tasks', 'ai', 'assistant', 'documents'
    ]);
  });

  it('allows the project summary and timeline panels to collapse independently', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());
    component.summaryCollapsed.set(false);
    component.timelineCollapsed.set(false);

    component.toggleSummaryPanel();

    expect(component.summaryCollapsed()).toBe(true);
    expect(component.timelineCollapsed()).toBe(false);

    component.toggleTimelinePanel();

    expect(component.summaryCollapsed()).toBe(true);
    expect(component.timelineCollapsed()).toBe(true);
  });

  it('formats composite form element values with their subfield labels', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());

    const value = component.formatTemplateValue({
      contentType: 'formGroup',
      value: JSON.stringify({ 'Sözleşme No': 'C-1047', Tedarikçi: 'Örnek Ltd' })
    } as never);

    expect(value).toBe('Sözleşme No: C-1047 | Tedarikçi: Örnek Ltd');
  });

  it('combines project work-package data into a timeline group', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());
    component.timeline.set({
      projectId: 'project-1',
      startDate: '2024-03-06',
      endDate: '2024-06-06',
      isPartial: false,
      warnings: [],
      workPackages: [{
        id: 'work-package-1',
        title: 'Teknik Müdürlük Alımı (Ana Grup)',
        departmentId: 'department-1',
        departmentName: 'Teknik Müdürlük',
        managerEmployeeId: 'manager-1',
        managerName: 'Ahmet Görür',
        startDate: '2024-04-10',
        endDate: '2024-04-20',
        deviationDays: 0,
        state: 'Active',
        processes: []
      }]
    } as never);

    const timeline = component.timelineGroups();

    expect(timeline).toHaveLength(1);
    expect(timeline[0].title).toBe('Teknik Müdürlük Alımı (Ana Grup)');
    expect(timeline[0].date).toBe('10.04.2024 12:00');
    expect(timeline[0].state).toBe('active');
  });

  it('closes task, status and assignee popovers when the user clicks elsewhere', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());
    component.openTaskMenuId.set('task-1');
    component.openStatusMenuId.set('task-1');
    component.reassignTaskContext.set({ groupId: 'group-1', taskId: 'task-1' });

    component.closeTaskPopoversOnOutsideClick({
      target: { closest: () => null }
    } as unknown as MouseEvent);

    expect(component.openTaskMenuId()).toBeNull();
    expect(component.openStatusMenuId()).toBeNull();
    expect(component.reassignTaskContext()).toBeNull();
  });

  it('keeps system activity separate from user comment counts', () => {
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());
    const task = {
      commentEntries: [
        { id: '1', author: 'Mustafa', text: 'Saha ekibi bilgilendirildi.', createdAtUtc: '2026-07-31T10:00:00Z' },
        { id: '2', author: 'Mustafa', text: 'Görev durumu Bekliyor durumundan Devam Ediyor durumuna getirildi.', createdAtUtc: '2026-07-31T10:05:00Z' }
      ]
    } as never;

    expect(component.taskUserCommentCount(task)).toBe(1);
  });

  it('removes a document from the page after the delete request succeeds', async () => {
    const taskApi = TestBed.inject(TaskApiService);
    const deleteDocument = vi.spyOn(taskApi, 'deleteDocument').mockResolvedValue();
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());
    const document = {
      id: 'document-1',
      noteId: null,
      uploadedBy: 'Mustafa',
      name: 'Faaliyet Raporu.docx',
      kind: 'word',
      size: '40 Kb',
      sizeBytes: 40960,
      createdAtUtc: '2026-08-03T12:00:00Z'
    } as never;
    component.documents.set([document]);
    component.documentDeleteConfirmation.set({ document });

    await component.confirmDeleteDocument();

    expect(deleteDocument).toHaveBeenCalledWith(expect.any(String), 'document-1');
    expect(component.documents()).toEqual([]);
    expect(component.documentDeleteConfirmation()).toBeNull();
  });

  it('reconciles a stale document card when the document is already absent on the server', async () => {
    const taskApi = TestBed.inject(TaskApiService);
    vi.spyOn(taskApi, 'deleteDocument').mockRejectedValue(new Error('Not Found'));
    vi.spyOn(taskApi, 'listDocuments').mockResolvedValue([]);
    const component = TestBed.runInInjectionContext(() => new ProjectDetailPage());
    const document = {
      id: 'document-1',
      noteId: null,
      uploadedBy: 'Mustafa',
      name: 'Faaliyet Raporu.docx',
      kind: 'word',
      size: '40 Kb',
      sizeBytes: 40960,
      createdAtUtc: '2026-08-03T12:00:00Z'
    } as never;
    component.documents.set([document]);
    component.documentDeleteConfirmation.set({ document });

    await component.confirmDeleteDocument();

    expect(component.documents()).toEqual([]);
    expect(component.documentDeleteConfirmation()).toBeNull();
  });
});
