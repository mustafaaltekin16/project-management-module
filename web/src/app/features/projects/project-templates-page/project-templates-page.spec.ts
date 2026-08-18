import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../../shared/auth/auth.service';
import { ToastService } from '../../../shared/toast/toast.service';
import { TemplateApiService } from '../data/template-api.service';
import { TemplateDto, TemplateFieldDto } from '../data/template-api.models';
import { ProjectTemplatesPage } from './project-templates-page';

const field = (overrides: Partial<TemplateFieldDto>): TemplateFieldDto => ({
  id: crypto.randomUUID(),
  label: 'Alan',
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
});

const templates: TemplateDto[] = [
  {
    id: 'template-simple',
    name: 'Hızlı Başlangıç',
    applicableProjectType: 'Simple',
    fields: [
      field({ id: 'simple-1', label: 'Proje Sahibi', kind: 'System', sortOrder: 2 }),
      field({ id: 'simple-2', label: 'Müşteri Segmenti', isRequired: true, sortOrder: 1 })
    ]
  },
  {
    id: 'template-multi',
    name: 'Departman Projesi',
    applicableProjectType: 'MultiUnit',
    fields: [
      field({ id: 'multi-1', label: 'Bütçe Onayı', isRequired: true }),
      field({ id: 'multi-2', label: 'Pasif Alan', isActive: false })
    ]
  }
];

describe('ProjectTemplatesPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectTemplatesPage],
      providers: [
        provideRouter([]),
        {
          provide: TemplateApiService,
          useValue: {
            list: async () => templates,
            remove: async () => undefined
          }
        },
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({ userId: 'user-1', displayName: 'Proje Yöneticisi', roles: ['ProjectManager'] }),
            hasAnyRole: () => true,
            logout: () => undefined
          }
        },
        {
          provide: ToastService,
          useValue: {
            success: () => undefined,
            error: () => undefined
          }
        }
      ]
    }).compileComponents();
  });

  it('filters templates by type and field label', () => {
    const component = TestBed.createComponent(ProjectTemplatesPage).componentInstance;
    component.templates.set(templates);

    component.selectType('MultiUnit');
    expect(component.filteredTemplates().map((template) => template.id)).toEqual(['template-multi']);

    component.selectType('All');
    component.searchQuery.set('müşteri');
    expect(component.filteredTemplates().map((template) => template.id)).toEqual(['template-simple']);
  });

  it('calculates library summary values from active template data', () => {
    const component = TestBed.createComponent(ProjectTemplatesPage).componentInstance;
    component.templates.set(templates);

    expect(component.typeCoverageCount()).toBe(2);
    expect(component.customFieldTotal()).toBe(3);
    expect(component.requiredFieldTotal()).toBe(2);
  });

  it('sorts preview fields and excludes inactive fields', () => {
    const component = TestBed.createComponent(ProjectTemplatesPage).componentInstance;
    component.templates.set(templates);

    expect(component.previewFields(templates[0]).map((item) => item.label)).toEqual([
      'Müşteri Segmenti',
      'Proje Sahibi'
    ]);
    expect(component.previewFields(templates[1]).map((item) => item.label)).toEqual(['Bütçe Onayı']);
  });

  it('clears search and type filters together', () => {
    const component = TestBed.createComponent(ProjectTemplatesPage).componentInstance;
    component.searchQuery.set('bütçe');
    component.selectType('MultiUnit');

    component.clearFilters();

    expect(component.searchQuery()).toBe('');
    expect(component.selectedType()).toBe('All');
    expect(component.hasActiveFilters()).toBe(false);
  });
});
