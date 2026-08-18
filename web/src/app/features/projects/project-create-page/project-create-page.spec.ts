import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { ProjectCreatePage } from './project-create-page';
import { TemplateDto, TemplateFieldDto } from '../data/template-api.models';
import { ProjectApiService } from '../data/project-api.service';
import { CreateProjectRequest, ProjectDetailDto } from '../data/project-api.models';
import { DepartmentDto } from '../data/department-api.models';
import { EmployeeDto } from '../data/employee-api.models';
import { TaskApiService } from '../data/task-api.service';

class ProjectApiStub {
  readonly requests: CreateProjectRequest[] = [];

  async create(request: CreateProjectRequest): Promise<ProjectDetailDto> {
    this.requests.push(request);
    return {
      id: 'project-1',
      name: request.name,
      unit: request.unit,
      departments: request.departments.map((department, index) => ({
        ...department,
        id: `work-package-${index + 1}`
      }))
    } as ProjectDetailDto;
  }
}

class TaskApiStub {
  async uploadDocument(): Promise<void> {}
}

function field(overrides: Partial<TemplateFieldDto> & Pick<TemplateFieldDto, 'id' | 'label'>): TemplateFieldDto {
  return {
    hint: '',
    contentType: 'text',
    listName: null,
    isRequired: false,
    isActive: true,
    sortOrder: 0,
    kind: 'Custom',
    systemKey: null,
    options: [],
    ...overrides
  };
}

function template(fields: TemplateFieldDto[], type: TemplateDto['applicableProjectType'] = 'Simple'): TemplateDto {
  return { id: `template-${type}`, name: `${type} Şablonu`, applicableProjectType: type, fields };
}

describe('ProjectCreatePage', () => {
  let projectApi: ProjectApiStub;
  let taskApi: TaskApiStub;

  beforeEach(async () => {
    projectApi = new ProjectApiStub();
    taskApi = new TaskApiStub();
    await TestBed.configureTestingModule({
      imports: [ProjectCreatePage],
      providers: [
        provideRouter([{ path: 'projects/:id', component: ProjectCreatePage }]),
        provideHttpClient(),
        { provide: ProjectApiService, useValue: projectApi },
        { provide: TaskApiService, useValue: taskApi }
      ]
    }).compileComponents();
  });

  it('switches form content for multi-unit projects', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    component.setMode('multi');

    expect(component.mode()).toBe('multi');
    expect(component.templateFields()).toContain('Bütçe');
    expect(component.templateFields()).not.toContain('Birim');
    expect(component.components().find((item) => item.key === 'flow')?.selected).toBe(true);
  });

  it('keeps legacy feasibility templates compatible with the multi-unit form', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const feasibilityTemplate = template([
      field({
        id: 'project-name',
        label: 'Proje Adı',
        kind: 'System',
        systemKey: 'projectName',
        isRequired: true
      }),
      field({
        id: 'budget',
        label: 'Bütçe',
        contentType: 'currency',
        kind: 'System',
        systemKey: 'budget',
        isRequired: true,
        sortOrder: 1
      })
    ], 'FeasibilityBased');
    component.templates.set([feasibilityTemplate]);

    component.onTemplateSelected(feasibilityTemplate.id);

    expect(component.mode()).toBe('multi');
    expect(component.templateFields()).toContain('Bütçe');
    expect(component.templateFields()).not.toContain('Birim');
    expect(component.templateContractTags().map((tag) => tag.label)).not.toContain('Proje Türü');
    expect(component.components().find((item) => item.key === 'flow')?.selected).toBe(true);
  });

  it('lists every template regardless of the currently selected project mode', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const simpleTemplate = template([field({ id: 'a', label: 'A' })], 'Simple');
    const multiTemplate = template([field({ id: 'b', label: 'B' })], 'MultiUnit');
    component.templates.set([simpleTemplate, multiTemplate]);

    component.setMode('simple');
    expect(component.availableTemplates()).toEqual([simpleTemplate, multiTemplate]);

    component.setMode('multi');
    expect(component.availableTemplates()).toEqual([simpleTemplate, multiTemplate]);
  });

  it('adds an empty department row', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const initialCount = component.multiDepartments().length;

    component.addDepartment();

    expect(component.multiDepartments().length).toBe(initialCount + 1);
    expect(component.multiDepartments().at(-1)?.departmentId).toBe('');
  });

  it('shows all missing fields near save and marks their controls after the first save attempt', async () => {
    const fixture = TestBed.createComponent(ProjectCreatePage);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.validationErrorEntries()).toEqual([]);

    await component.save();
    fixture.detectChanges();

    expect(component.validationErrors()).toMatchObject({
      projectName: 'Proje adı girilmelidir.',
      description: 'Proje açıklaması girilmelidir.',
      managerEmployeeId: 'Proje yöneticisi seçilmelidir.'
    });
    expect(component.hasFieldError(component.departmentFieldErrorKey(1, 'departmentId'))).toBe(true);
    expect(component.hasFieldError(component.departmentFieldErrorKey(1, 'managerEmployeeId'))).toBe(true);
    expect(fixture.nativeElement.querySelector('[data-validation-key="projectName"].has-error')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.pc-validation-summary')?.textContent).toContain('alanı kontrol edin');
    expect(projectApi.requests).toHaveLength(0);
  });

  it('clears a field error as soon as that field is corrected', async () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;

    await component.save();
    expect(component.hasFieldError('projectName')).toBe(true);

    component.updateProjectName('Yeni proje');

    expect(component.hasFieldError('projectName')).toBe(false);
  });

  it('marks an end date that is earlier than the project start date', async () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    component.updateStartDate('2026-08-10');
    component.updateEndDate('2026-08-09');

    await component.save();

    expect(component.fieldError('endDate')).toBe('Bitiş tarihi başlangıç tarihinden önce olamaz.');
  });

  it('validates every required multi-unit work-package field and its date range', async () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    component.setMode('multi');
    component.updateStartDate('2026-08-01');
    component.updateEndDate('2026-08-31');
    component.updateDepartmentRow(1, 'startDate', '2026-07-31');
    component.updateDepartmentRow(1, 'endDate', '2026-09-01');

    await component.save();

    expect(component.hasFieldError(component.departmentFieldErrorKey(1, 'title'))).toBe(true);
    expect(component.hasFieldError(component.departmentFieldErrorKey(1, 'departmentId'))).toBe(true);
    expect(component.fieldError(component.departmentFieldErrorKey(1, 'startDate')))
      .toBe('İş paketi proje başlangıcından önce başlayamaz.');
    expect(component.fieldError(component.departmentFieldErrorKey(1, 'endDate')))
      .toBe('İş paketi proje bitişinden sonra bitemez.');
  });

  it('submits authoritative employee and department ids for a multi-unit project', async () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const manager: EmployeeDto = {
      id: 'employee-1',
      displayName: 'Selim Akar',
      email: 'selim@example.com',
      departmentId: 'department-1',
      departmentName: 'BT Müdürlüğü',
      title: 'Proje Yöneticisi',
      roles: ['ProjectManager']
    };
    const department: DepartmentDto = {
      id: 'department-1',
      name: 'BT Müdürlüğü',
      headEmployeeId: manager.id,
      headDisplayName: manager.displayName,
      memberCount: 1
    };
    component.departmentCandidates.set([department]);
    component.allEmployees.set([manager]);
    component.managerCandidates.set([manager]);
    component.managerEmployeeId.set(manager.id);
    component.unitDepartmentId.set(department.id);
    component.projectName.set('Dizin referanslı proje');
    component.description.set('Çok birimli proje açıklaması');
    component.setMode('multi');
    component.updateMultiDepartmentTitle(1, 'BT İş Paketi');
    component.updateDepartmentRow(1, 'departmentId', department.id);
    component.updateDepartmentRow(1, 'startDate', component.startDate());
    component.updateDepartmentRow(1, 'endDate', component.endDate());

    await component.save();

    expect(projectApi.requests).toHaveLength(1);
    expect(projectApi.requests[0].managerEmployeeId).toBe(manager.id);
    expect(projectApi.requests[0].unitDepartmentId).toBe(department.id);
    expect(projectApi.requests[0].departments).toEqual([expect.objectContaining({
      departmentId: department.id,
      managerEmployeeId: manager.id,
      title: 'BT İş Paketi',
      startDate: component.startDate(),
      endDate: component.endDate()
    })]);
    expect(projectApi.requests[0].type).toBe('MultiUnit');
  });

  it('preserves a FeasibilityBased template type instead of downgrading it to MultiUnit', async () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const manager: EmployeeDto = {
      id: 'employee-1',
      displayName: 'Selim Akar',
      email: 'selim@example.com',
      departmentId: 'department-1',
      departmentName: 'BT Müdürlüğü',
      title: 'Proje Yöneticisi',
      roles: ['ProjectManager']
    };
    const department: DepartmentDto = {
      id: 'department-1',
      name: 'BT Müdürlüğü',
      headEmployeeId: manager.id,
      headDisplayName: manager.displayName,
      memberCount: 1
    };
    component.departmentCandidates.set([department]);
    component.allEmployees.set([manager]);
    component.managerCandidates.set([manager]);
    component.managerEmployeeId.set(manager.id);
    component.unitDepartmentId.set(department.id);
    component.projectName.set('Fizibilite Projesi');
    component.description.set('Açıklama');
    const feasibilityTemplate = template([
      field({
        id: 'project-name', label: 'Proje Adı', kind: 'System', systemKey: 'projectName', isRequired: true
      }),
      field({
        id: 'budget', label: 'Bütçe', contentType: 'currency', kind: 'System', systemKey: 'budget',
        isRequired: true, sortOrder: 1
      })
    ], 'FeasibilityBased');
    component.templates.set([feasibilityTemplate]);
    component.onTemplateSelected(feasibilityTemplate.id);
    component.updateMultiDepartmentTitle(1, 'İş Paketi');
    component.updateDepartmentRow(1, 'departmentId', department.id);
    component.updateDepartmentRow(1, 'startDate', component.startDate());
    component.updateDepartmentRow(1, 'endDate', component.endDate());

    await component.save();

    expect(projectApi.requests).toHaveLength(1);
    expect(projectApi.requests[0].type).toBe('FeasibilityBased');
  });

  it('submits simple-project departments with project-level dates', async () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const manager: EmployeeDto = {
      id: 'employee-1',
      displayName: 'Ahmet Görür',
      email: 'ahmet@example.com',
      departmentId: 'department-1',
      departmentName: 'BT Departmanı',
      title: 'Proje Yöneticisi',
      roles: ['ProjectManager']
    };
    const department: DepartmentDto = {
      id: 'department-1',
      name: 'BT Departmanı',
      headEmployeeId: manager.id,
      headDisplayName: manager.displayName,
      memberCount: 1
    };
    component.departmentCandidates.set([department]);
    component.allEmployees.set([manager]);
    component.managerCandidates.set([manager]);
    component.managerEmployeeId.set(manager.id);
    component.projectName.set('Basit proje');
    component.description.set('Basit proje açıklaması');
    component.updateDepartmentRow(1, 'departmentId', department.id);

    await component.save();

    expect(projectApi.requests).toHaveLength(1);
    expect(projectApi.requests[0].type).toBe('Simple');
    expect(projectApi.requests[0].unitDepartmentId).toBe(department.id);
    expect(projectApi.requests[0].budget).toBe(0);
    expect(projectApi.requests[0].departments).toEqual([expect.objectContaining({
      title: '',
      departmentId: department.id,
      managerEmployeeId: manager.id,
      startDate: null,
      endDate: null
    })]);
  });

  it('keeps the project description component selected and required', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;

    component.toggleComponent('description');

    expect(component.components().find((item) => item.key === 'description')?.selected).toBe(true);
    expect(component.components().find((item) => item.key === 'description')?.required).toBe(true);
    expect(component.projectFormSchema().find((item) => item.systemKey === 'description')?.isRequired).toBe(true);
  });

  it('makes an optional template description required on the project form', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const selected = template([
      field({
        id: 'optional-description',
        label: 'Proje Açıklaması',
        contentType: 'textarea',
        kind: 'System',
        systemKey: 'description',
        isRequired: false
      })
    ]);
    component.templates.set([selected]);

    component.onTemplateSelected(selected.id);

    expect(component.projectFormSchema().find((item) => item.systemKey === 'description')?.isRequired).toBe(true);
  });

  it('shows the optional second manager only after the add button is used', () => {
    const fixture = TestBed.createComponent(ProjectCreatePage);
    const component = fixture.componentInstance;
    component.setMode('multi');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[name="secondManagerEmployeeId"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('.pc-manager-add')).not.toBeNull();

    component.addSecondManager();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[name="secondManagerEmployeeId"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.pc-manager-add')).toBeNull();
  });

  it('only shows department row delete buttons when multiple rows exist', () => {
    const fixture = TestBed.createComponent(ProjectCreatePage);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.pc-row-remove')).toHaveLength(0);

    component.addDepartment();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.pc-row-remove')).toHaveLength(2);
  });

  it('navigates back to the projects list when the cancel button is used', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component.cancel();

    expect(router.navigate).toHaveBeenCalledWith(['/projects']);
  });

  it('labels the template-free project structure as the default template', () => {
    const fixture = TestBed.createComponent(ProjectCreatePage);
    fixture.detectChanges();

    const optionElements = fixture.nativeElement
      .querySelectorAll('.pc-template-picker option') as NodeListOf<HTMLOptionElement>;
    const options = Array.from(optionElements).map((option) => option.textContent?.trim());

    expect(options).toContain('Varsayılan Şablon');
    expect(options).not.toContain('Şablon Yok');
  });

  it('does not show template properties when no template is selected', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;

    expect(component.templateContractTags()).toEqual([]);
  });

  it('derives template properties from active template fields and project structure', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const selected = template([
      field({
        id: 'name',
        label: 'Proje Adı',
        kind: 'System',
        systemKey: 'projectName',
        isRequired: true
      }),
      field({ id: 'risk', label: 'Risk Seviyesi', contentType: 'select', sortOrder: 1 }),
      field({ id: 'inactive', label: 'Gizli Alan', isActive: false, sortOrder: 2 })
    ], 'MultiUnit');
    component.templates.set([selected]);

    component.onTemplateSelected(selected.id);

    const labels = component.templateContractTags().map((tag) => tag.label);
    expect(labels).toContain('Proje Adı');
    expect(labels).toContain('Risk Seviyesi');
    expect(labels).toContain('Departman');
    expect(labels).not.toContain('Proje Türü');
    expect(labels).not.toContain('Gizli Alan');
  });

  it('shows all project components with the first five selected by default', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;
    const selected = template([
      field({
        id: 'description',
        label: 'Proje Açıklaması',
        contentType: 'textarea',
        kind: 'System',
        systemKey: 'description'
      }),
      field({ id: 'approval', label: 'Onay Kontrolü', contentType: 'checklist', sortOrder: 1 }),
      field({ id: 'participant', label: 'Katılımcı', contentType: 'employee', sortOrder: 2 }),
      field({ id: 'meeting-date', label: 'Toplantı Tarihi', contentType: 'datetime', sortOrder: 3 })
    ]);
    component.templates.set([selected]);

    component.onTemplateSelected(selected.id);

    const components = component.components();
    expect(components.map((item) => item.key)).toEqual([
      'description', 'tasks', 'ai', 'chat', 'documents', 'flow', 'meeting'
    ]);
    expect(components.filter((item) => item.selected).map((item) => item.key)).toEqual([
      'description', 'tasks', 'ai', 'chat', 'documents', 'flow'
    ]);
    expect(components.filter((item) => item.required).map((item) => item.key)).toEqual(['description']);
    expect(components.find((item) => item.key === 'chat')?.label).toBe('Proje Rehberi');
  });

  it('shows the complete collaboration component set for multi-unit projects', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;

    component.setMode('multi');

    expect(component.components().map((item) => item.key)).toEqual([
      'description', 'tasks', 'ai', 'chat', 'documents', 'flow', 'meeting'
    ]);
    expect(component.components().find((item) => item.key === 'ai')?.selected).toBe(true);
    expect(component.components().find((item) => item.key === 'chat')?.selected).toBe(true);
    expect(component.components().find((item) => item.key === 'meeting')?.selected).toBe(false);
  });

  it('stores checklist selections as a template value', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;

    component.toggleChecklistItem('checklist-1', 'Güvenlik kontrolü', true);

    expect(component.isChecklistItemChecked('checklist-1', 'Güvenlik kontrolü')).toBe(true);
  });

  it('stores independently editable table cells', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;

    component.updateTableCell('table-1', 0, 1, 'Onaylandı');

    expect(component.getTableCell('table-1', 0, 1)).toBe('Onaylandı');
    expect(component.getTableCell('table-1', 1, 1)).toBe('');
  });

  it('stores composite form element values by subfield', () => {
    const component = TestBed.createComponent(ProjectCreatePage).componentInstance;

    component.updateFormGroupValue('group-1', 'Sözleşme No', 'C-1047');
    component.updateFormGroupValue('group-1', 'Tedarikçi', 'Örnek Ltd');

    expect(component.getFormGroupValue('group-1', 'Sözleşme No')).toBe('C-1047');
    expect(component.getFormGroupValue('group-1', 'Tedarikçi')).toBe('Örnek Ltd');
  });
});
