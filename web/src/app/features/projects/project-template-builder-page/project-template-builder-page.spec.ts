import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ProjectTemplateBuilderPage } from './project-template-builder-page';
import { TemplateApiService } from '../data/template-api.service';

describe('ProjectTemplateBuilderPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectTemplateBuilderPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        {
          provide: TemplateApiService,
          useValue: {
            create: async (request: { name: string }) => ({
              id: 'template-1',
              name: request.name,
              applicableProjectType: 'Simple',
              fields: []
            })
          }
        }
      ]
    }).compileComponents();
  });

  it('selects and configures a toolbox item when it is added', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    component.addTool({ id: 'date', label: 'Tarih Seçici', icon: 'calendar' });

    expect(component.selectedTool()).toBe('date');
    expect(component.selectedField()?.contentType).toBe('date');
    expect(component.addedFields()).toHaveLength(1);
  });

  it('opens and closes preview mode', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    component.showPreview();
    expect(component.previewMode()).toBe(true);
    component.closePreview();
    expect(component.previewMode()).toBe(false);
  });

  it('opens the success dialog after saving a valid template', async () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    component.templateName.set('Basit Proje Standardı');
    component.setProjectType('Basit Proje');
    component.addTool({ id: 'text', label: 'Kısa Metin', icon: 'type' });

    await component.saveTemplate();

    expect(component.saveDialogOpen()).toBe(true);
  });

  it('preserves an existing FeasibilityBased template type when saved without changing the toggle', async () => {
    TestBed.resetTestingModule();
    const createSpy = vi.fn(async (request: { name: string; applicableProjectType: string }) => ({
      id: 'template-1',
      name: request.name,
      applicableProjectType: request.applicableProjectType,
      fields: []
    }));
    await TestBed.configureTestingModule({
      imports: [ProjectTemplateBuilderPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        { provide: TemplateApiService, useValue: { create: createSpy } }
      ]
    }).compileComponents();

    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    (component as unknown as { loadTemplate: (t: unknown) => void }).loadTemplate({
      id: 'template-1',
      name: 'Fizibilite Şablonu',
      applicableProjectType: 'FeasibilityBased',
      fields: []
    });
    component.templateName.set('Fizibilite Şablonu');
    component.addTool({ id: 'text', label: 'Kısa Metin', icon: 'type' });

    await component.saveTemplate();

    expect(createSpy).toHaveBeenCalledWith(
      expect.objectContaining({ applicableProjectType: 'FeasibilityBased' })
    );
  });

  it('edits the behavior of the explicitly selected custom field', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    component.addTool({ id: 'text', label: 'Kısa Metin', icon: 'type' });
    const firstId = component.addedFields()[0].id;
    component.addTool({ id: 'date', label: 'Tarih', icon: 'calendar' });

    component.selectField(firstId);
    component.setFieldHint('İş gerekçesini girin');

    expect(component.addedFields()[0].hint).toBe('İş gerekçesini girin');
    expect(component.addedFields()[1].contentType).toBe('date');
  });

  it('keeps field names static on canvas and read-only in properties', () => {
    const fixture = TestBed.createComponent(ProjectTemplateBuilderPage);
    const component = fixture.componentInstance;
    component.addTool({ id: 'text', label: 'Kısa Metin', icon: 'type' });
    const fieldId = component.addedFields()[0].id;

    fixture.detectChanges();

    const canvasField = fixture.nativeElement.querySelector(
      `.tb-form-node[data-field-id="${fieldId}"]`
    ) as HTMLElement;
    const properties = fixture.nativeElement.querySelector('.tb-properties-card') as HTMLElement;
    const readonlyLabel = properties.querySelector('.tb-readonly-label') as HTMLInputElement;
    expect(canvasField.querySelector('.tb-inline-label-editor')).toBeNull();
    expect(canvasField.textContent).toContain('Kısa Metin Alanı');
    expect(readonlyLabel.value).toBe('Kısa Metin Alanı');
    expect(readonlyLabel.readOnly).toBe(true);
  });

  it('keeps mandatory system fields active and required', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    component.selectField('system-project-name');

    component.setIsActive(false);
    component.setIsRequired(false);

    expect(component.selectedField()?.isActive).toBe(true);
    expect(component.selectedField()?.isRequired).toBe(true);
  });

  it('shows the system identity read-only while its behavior text stays editable', () => {
    const fixture = TestBed.createComponent(ProjectTemplateBuilderPage);
    fixture.componentInstance.selectField('system-project-name');
    fixture.detectChanges();

    const properties = fixture.nativeElement.querySelector('.tb-properties-card') as HTMLElement;
    const identityInput = properties.querySelector('.tb-readonly-label') as HTMLInputElement;
    const hintInput = properties.querySelector('textarea') as HTMLTextAreaElement;
    const typeSelect = properties.querySelector('select') as HTMLSelectElement;
    expect(identityInput.value).toBe('Proje Adı');
    expect(identityInput.readOnly).toBe(true);
    expect(hintInput.disabled).toBe(false);
    expect(typeSelect.disabled).toBe(true);
    expect(properties.textContent).toContain('tek satırlık kısa bir metin');
  });

  it('stores manual list options without blank or duplicate values', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    component.addTool({ id: 'select', label: 'Listeden Seçim', icon: 'checkbox' });

    component.setOptionsText('Yüksek\nDüşük\nYüksek\n\nOrta');

    expect(component.selectedField()?.options).toEqual(['Yüksek', 'Düşük', 'Orta']);
  });

  it('changes the type-specific system schema for simple projects', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;

    component.setProjectType('Basit Proje');

    expect(component.visibleCanvasNodes().some((field) => field.systemKey === 'budget')).toBe(false);
    expect(component.typeSpecificTitle()).toBe('Departmanlar');
    expect(component.typeSpecificCards().map((card) => card.title)).toEqual([
      'Departmanlar', 'Departman Yöneticileri'
    ]);
  });

  it('shows the multi-unit work-package schema', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;

    component.setProjectType('Çok Birimli Proje');
    expect(component.typeSpecificTitle()).toBe('İş Paketleri ve Departmanlar');
    expect(component.typeSpecificCards().map((card) => card.title)).toContain('İş Paketi');
  });

  it('updates the type-specific cards immediately when the selection changes', () => {
    const fixture = TestBed.createComponent(ProjectTemplateBuilderPage);
    const component = fixture.componentInstance;

    component.setProjectType('Çok Birimli Proje');
    fixture.detectChanges();
    expect(component.typeSpecificCards().map((card) => card.title)).toContain('İş Paketi');
    expect(component.typeSpecificCards().map((card) => card.title)).toContain('Sorumlu Departman');

    component.setProjectType('Basit Proje');
    fixture.detectChanges();
    expect(component.typeSpecificCards().map((card) => card.title)).toContain('Departmanlar');
    expect(component.typeSpecificCards().map((card) => card.title)).not.toContain('İş Paketi');
  });

  it('keeps design controls disabled and enables data entry in form preview', () => {
    const fixture = TestBed.createComponent(ProjectTemplateBuilderPage);
    fixture.detectChanges();

    const designInput = fixture.nativeElement.querySelector(
      '.tb-form-canvas article input[type="text"]'
    ) as HTMLInputElement;
    expect(designInput.disabled).toBe(true);

    fixture.componentInstance.showPreview();
    fixture.detectChanges();
    const previewInput = fixture.nativeElement.querySelector(
      '.tb-preview-form input[type="text"]'
    ) as HTMLInputElement;
    expect(previewInput.disabled).toBe(false);
  });

  it('allows dragging from the whole field without directional move actions', () => {
    const fixture = TestBed.createComponent(ProjectTemplateBuilderPage);
    fixture.componentInstance.addTool({ id: 'text', label: 'Kısa Metin', icon: 'type' });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).not.toContain('Yukarı taşı');
    expect(text).not.toContain('Aşağı taşı');
    expect(fixture.nativeElement.querySelector('.tb-order-actions')).toBeNull();
    expect(fixture.nativeElement.querySelector('.tb-form-node.cdk-drag')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[cdkDragHandle]')).toBeNull();
  });

  it('offers field actions instead of preview actions in the three-dot menu', () => {
    const fixture = TestBed.createComponent(ProjectTemplateBuilderPage);
    const component = fixture.componentInstance;
    component.addTool({ id: 'text', label: 'Kısa Metin', icon: 'type' });
    component.toggleFieldMenu(component.selectedFieldId()!);
    fixture.detectChanges();

    let menu = fixture.nativeElement.querySelector('.tb-node-menu') as HTMLElement;
    let actions = Array.from(menu.querySelectorAll('button')) as HTMLButtonElement[];
    expect(actions.map((button) => button.textContent?.trim())).toEqual(['Çoğalt', 'Reset', 'Sil']);
    expect(fixture.nativeElement.querySelector('.tb-destructive-actions')).toBeNull();

    component.toggleFieldMenu('system-manager');
    fixture.detectChanges();
    menu = fixture.nativeElement.querySelector('.tb-node-menu') as HTMLElement;
    actions = Array.from(menu.querySelectorAll('button')) as HTMLButtonElement[];
    expect(actions.map((button) => button.textContent?.trim())).toEqual(['Reset', 'Sil']);
    expect(actions[1].disabled).toBe(true);
  });

  it('clears selection on canvas background and closes the menu on outside click', () => {
    const fixture = TestBed.createComponent(ProjectTemplateBuilderPage);
    const component = fixture.componentInstance;
    component.selectField('system-end-date');
    component.toggleFieldMenu('system-end-date');
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.tb-canvas') as HTMLElement).click();
    fixture.detectChanges();
    expect(component.selectedField()).toBeNull();
    expect(component.fieldMenuId()).toBeNull();

    component.toggleFieldMenu('system-description');
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.tb-template-card') as HTMLElement).click();
    expect(component.fieldMenuId()).toBeNull();
  });

  it('appends toolbox fields to the end regardless of the current selection', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    component.selectField('system-project-name');
    component.addTool({ id: 'date', label: 'Tarih', icon: 'calendar' });

    expect(component.formNodes().at(-1)?.id).toBe(component.selectedFieldId());
    expect(component.formNodes().at(-1)?.contentType).toBe('date');
  });

  it('shows properties that match the selected field type', () => {
    const fixture = TestBed.createComponent(ProjectTemplateBuilderPage);
    const component = fixture.componentInstance;
    component.addTool({ id: 'select', label: 'Listeden Seçim', icon: 'checkbox' });
    fixture.detectChanges();

    let properties = fixture.nativeElement.querySelector('.tb-properties-card') as HTMLElement;
    expect(properties.textContent).toContain('Liste Seçenekleri');
    expect(properties.textContent).toContain('tek bir değer seçtirir');

    component.addTool({ id: 'attachment', label: 'Dosya Eki', icon: 'paperclip' });
    fixture.detectChanges();
    properties = fixture.nativeElement.querySelector('.tb-properties-card') as HTMLElement;
    expect(properties.textContent).toContain('Yardımcı Metin');
    expect(properties.textContent).toContain('forma dosya eklemesini sağlar');
    expect(properties.textContent).not.toContain('Liste Seçenekleri');
  });

  it('keeps the canvas order unchanged while selecting different fields', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    const initialOrder = component.formNodes().map((field) => field.id);

    component.selectField('system-end-date');
    component.selectField('system-unit');
    component.selectField('system-manager');

    expect(component.formNodes().map((field) => field.id)).toEqual(initialOrder);
  });

  it('starts with essential defaults and lets every system field move', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    component.dropField({ previousIndex: 0, currentIndex: 2 } as never);

    expect(component.formNodes().map((field) => field.systemKey)).toContain('attachments');
    expect(component.formNodes().map((field) => field.systemKey)).not.toContain('secondManager');
    expect(component.formNodes()[2].systemKey).toBe('projectName');
  });

  it('provides a functional definition for every toolbox element', () => {
    const component = TestBed.createComponent(ProjectTemplateBuilderPage).componentInstance;
    const tools = component.toolGroups.flatMap((group) => group.items);

    for (const tool of tools) component.addTool(tool);

    expect(component.addedFields()).toHaveLength(tools.length);
    expect(component.addedFields().map((field) => field.contentType)).toEqual(expect.arrayContaining([
      'section', 'table', 'formGroup', 'text', 'textarea', 'number', 'date', 'datetime', 'select',
      'checkbox', 'yesNo', 'employee', 'checklist', 'attachment', 'image', 'signature'
    ]));
  });
});
