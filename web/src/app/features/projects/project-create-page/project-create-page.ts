import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { Router } from '@angular/router';
import { Icon, IconName } from '../../../shared/icon/icon';
import { ToastService } from '../../../shared/toast/toast.service';
import { ProjectApiService } from '../data/project-api.service';
import { TemplateApiService } from '../data/template-api.service';
import { TaskApiService } from '../data/task-api.service';
import { EmployeeApiService } from '../data/employee-api.service';
import { DepartmentApiService } from '../data/department-api.service';
import { BackendProjectType } from '../data/project-api.models';
import { TemplateDto, TemplateFieldDto, TemplateFieldKind } from '../data/template-api.models';
import { EmployeeDto } from '../data/employee-api.models';
import { DepartmentDto } from '../data/department-api.models';
import { AuthService } from '../../../shared/auth/auth.service';

type CreateMode = 'simple' | 'multi';

const MODE_TO_TYPE: Record<CreateMode, BackendProjectType> = {
  simple: 'Simple',
  multi: 'MultiUnit'
};

const CURRENCY_CODES: Record<string, string> = { '₺': 'TRY', '$': 'USD', '€': 'EUR' };

interface RailItem {
  icon: IconName;
  label: string;
  active?: boolean;
}

interface DepartmentRow {
  id: number;
  title: string;
  departmentId: string;
  managerEmployeeId: string;
  startDate: string;
  endDate: string;
}

interface ComponentOption {
  key: string;
  label: string;
  required?: boolean;
  selected: boolean;
}

interface TemplateContractTag {
  id: string;
  label: string;
  isRequired: boolean;
  isCustom: boolean;
}

const COMPONENT_LABELS: Record<string, string> = {
  description: 'Proje Açıklaması',
  tasks: 'Görevler',
  ai: 'AI İş Paketi',
  chat: 'Proje Rehberi',
  documents: 'Dokümanlar',
  flow: 'Akış',
  meeting: 'Online Toplantı'
};

const DEFAULT_ENABLED_COMPONENTS = new Set(['description', 'tasks', 'ai', 'documents', 'flow', 'chat']);

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function monthsFromNowIso(months: number): string {
  const date = new Date();
  date.setMonth(date.getMonth() + months);
  return date.toISOString().slice(0, 10);
}

function schemaField(
  id: string,
  systemKey: string,
  label: string,
  contentType: string,
  hint: string,
  isRequired: boolean,
  sortOrder: number
): TemplateFieldDto {
  return {
    id,
    label,
    hint,
    contentType,
    listName: null,
    isRequired,
    isActive: true,
    sortOrder,
    kind: 'System',
    systemKey,
    options: []
  };
}

function fallbackProjectSchema(mode: CreateMode): TemplateFieldDto[] {
  const fields = [
    schemaField('base-project-name', 'projectName', 'Proje Adı', 'text', 'Projeyi tanımlayan kısa ve ayırt edici ad', true, 0),
    schemaField('base-unit', 'unit', 'Birim', 'text', 'Projeden sorumlu ana birim', true, 1),
    schemaField('base-start-date', 'startDate', 'Başlangıç Tarihi', 'date', 'Planlanan başlangıç tarihi', true, 2),
    schemaField('base-end-date', 'endDate', 'Bitiş Tarihi', 'date', 'Planlanan bitiş tarihi', true, 3),
    schemaField('base-description', 'description', 'Proje Açıklaması', 'textarea', 'Amaç, kapsam ve beklenen sonucu açıklayın', true, 4),
    schemaField('base-attachments', 'attachments', 'Dosya Ekleyin', 'attachment', 'Destekleyici dokümanlar', false, 5),
    schemaField('base-manager', 'manager', 'Proje Yöneticisi', 'employee', 'Projeden sorumlu yöneticiyi seçin', true, 6)
  ];
  if (mode !== 'simple') {
    fields.push(
      schemaField('base-second-manager', 'secondManager', 'İkinci Proje Yöneticisi', 'employee', 'İsteğe bağlı ikinci yönetici', false, 7),
      schemaField('base-budget', 'budget', 'Bütçe', 'currency', 'Planlanan proje bütçesi', true, 8)
    );
  }
  return fields;
}

@Component({
  selector: 'app-project-create-page',
  standalone: true,
  imports: [CommonModule, FormsModule, Icon, DragDropModule],
  templateUrl: './project-create-page.html',
  styleUrl: './project-create-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectCreatePage implements OnInit {
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly projectApi = inject(ProjectApiService);
  private readonly templateApi = inject(TemplateApiService);
  private readonly taskApi = inject(TaskApiService);
  private readonly employeeApi = inject(EmployeeApiService);
  private readonly departmentApi = inject(DepartmentApiService);
  private readonly authService = inject(AuthService);
  private readonly hostElement = inject(ElementRef<HTMLElement>);

  readonly currentUserName = computed(() => (this.authService.currentUser()?.displayName ?? ''));
  readonly managerCandidates = signal<EmployeeDto[]>([]);

  readonly railCompact = signal(false);
  readonly profileOpen = signal(false);
  readonly mode = signal<CreateMode>('simple');
  readonly projectName = signal('');
  readonly description = signal('');
  readonly unitDepartmentId = signal('');
  readonly startDate = signal(todayIso());
  readonly endDate = signal(monthsFromNowIso(2));
  // Defaults to whoever is actually logged in — previously hardcoded to a fixed name, so the picker
  // always showed the same person regardless of who was using the app.
  readonly managerEmployeeId = signal('');
  readonly secondManagerEmployeeId = signal('');
  readonly budget = signal('700.000');
  readonly currency = signal('₺');
  readonly attachmentNames = signal<string[]>([]);
  readonly errorMessage = signal('');
  readonly validationAttempted = signal(false);
  readonly validationErrors = signal<Record<string, string>>({});
  readonly validationErrorEntries = computed(() => Object.entries(this.validationErrors()));
  readonly validationSummaryMessages = computed(() =>
    [...new Set(this.validationErrorEntries().map(([, message]) => message))]
  );
  readonly saving = signal(false);
  readonly showSecondManager = signal(false);
  private attachmentFiles: File[] = [];
  private readonly templateFiles = new Map<string, File>();
  private nextDepartmentId = 2;
  private readonly formDirty = signal(false);

  readonly departmentCandidates = signal<DepartmentDto[]>([]);
  readonly allEmployees = signal<EmployeeDto[]>([]);

  readonly templates = signal<TemplateDto[]>([]);
  readonly selectedTemplateId = signal<string | null>(null);
  readonly templateValues = signal<Record<string, string | boolean>>({});

  readonly multiDepartments = signal<DepartmentRow[]>([
    { id: 1, title: '', departmentId: '', managerEmployeeId: '', startDate: '', endDate: '' }
  ]);

  private readonly componentSelections = signal<Record<string, boolean>>({});

  readonly selectedTemplate = computed(() =>
    this.templates().find((tpl) => tpl.id === this.selectedTemplateId()) ?? null
  );

  readonly availableTemplates = computed(() => this.templates());

  readonly projectFormSchema = computed(() => {
    const template = this.selectedTemplate();
    if (!template) {
      return fallbackProjectSchema(this.mode()).filter((field) => this.isSchemaFieldApplicable(field));
    }

    const normalized = template.fields
      .map((field) => ({
        ...field,
        contentType: this.normalizedContentType(field),
        kind: this.normalizedFieldKind(field),
        options: field.options ?? []
      }))
      .sort((left, right) => left.sortOrder - right.sortOrder);
    const hasSystemFields = normalized.some((field) => field.kind === 'System');
    const schema = hasSystemFields ? normalized : [...fallbackProjectSchema(this.mode()), ...normalized];
    const applicableSchema = schema
      .filter((field) => field.isActive !== false && this.isSchemaFieldApplicable(field))
      .map((field) =>
        this.normalizedFieldKind(field) === 'System' && field.systemKey === 'description'
          ? { ...field, isRequired: true }
          : field
      );

    const hasDescription = applicableSchema.some((field) =>
      this.normalizedFieldKind(field) === 'System' && field.systemKey === 'description'
    );
    if (!hasDescription) {
      const defaultDescription = fallbackProjectSchema(this.mode())
        .find((field) => field.systemKey === 'description');
      if (defaultDescription) applicableSchema.push(defaultDescription);
    }

    const hasSecondManager = applicableSchema.some((field) =>
      this.normalizedFieldKind(field) === 'System' && field.systemKey === 'secondManager'
    );
    if (this.mode() === 'multi' && this.showSecondManager() && !hasSecondManager) {
      const defaultSecondManager = fallbackProjectSchema(this.mode())
        .find((field) => field.systemKey === 'secondManager');
      if (defaultSecondManager) applicableSchema.push(defaultSecondManager);
    }

    return applicableSchema.sort((left, right) => left.sortOrder - right.sortOrder);
  });

  readonly activeTemplateFields = computed(() =>
    this.projectFormSchema().filter((field) => field.kind === 'Custom')
  );

  readonly templateFields = computed(() => this.projectFormSchema().map((field) => field.label));

  readonly templateContractTags = computed<TemplateContractTag[]>(() => {
    const template = this.selectedTemplate();
    if (!template) return [];

    const tags = template.fields
      .map((field) => ({
        ...field,
        kind: this.normalizedFieldKind(field),
        contentType: this.normalizedContentType(field)
      }))
      .filter((field) =>
        field.isActive !== false &&
        field.kind !== 'Section' &&
        this.isSchemaFieldApplicable(field)
      )
      .sort((left, right) => left.sortOrder - right.sortOrder)
      .map((field) => ({
        id: field.id,
        label: field.label,
        isRequired: field.isRequired,
        isCustom: field.kind === 'Custom'
      }));

    tags.push(
      { id: 'structure-department', label: 'Departman', isRequired: false, isCustom: false }
    );

    return tags.filter((tag, index, all) =>
      all.findIndex((candidate) =>
        candidate.label.localeCompare(tag.label, 'tr-TR', { sensitivity: 'accent' }) === 0
      ) === index
    );
  });

  readonly components = computed<ComponentOption[]>(() => {
    const selections = this.componentSelections();
    return Object.entries(COMPONENT_LABELS).map(([key, label]) => ({
      key,
      label,
      required: key === 'description',
      selected: selections[key] ?? DEFAULT_ENABLED_COMPONENTS.has(key)
    }));
  });

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
    try {
      this.templates.set(await this.templateApi.list());
      const newTemplateId = (history.state as { newTemplateId?: string } | null)?.newTemplateId;
      if (newTemplateId && this.templates().some((t) => t.id === newTemplateId)) {
        this.onTemplateSelected(newTemplateId, false);
      }
    } catch {
      // Template listing is a convenience, not required to create a project — fail silently.
    }

    try {
      const [departments, employees] = await Promise.all([this.departmentApi.list(), this.employeeApi.list()]);
      this.departmentCandidates.set(departments);
      this.allEmployees.set(employees);
      const managers = employees.filter(
        (employee) =>
          employee.roles.includes('ProjectManager') &&
          employee.isActive !== false &&
          employee.isSelectable !== false
      );
      this.managerCandidates.set(managers);

      const currentUserId = this.authService.currentUser()?.userId ?? '';
      if (managers.some((employee) => employee.id === currentUserId)) {
        this.managerEmployeeId.set(currentUserId);
      }

      const currentEmployee = employees.find((employee) => employee.id === currentUserId);
      if (currentEmployee?.departmentId) {
        this.unitDepartmentId.set(currentEmployee.departmentId);
      }
    } catch {
      // The directory is authoritative for project ownership. Save validation below prevents an
      // unresolved form from being submitted while the rest of the page remains usable.
    }
  }

  setMode(mode: CreateMode): void {
    const modeChanged = this.mode() !== mode;
    this.mode.set(mode);
    this.componentSelections.set({});
    if (modeChanged) {
      this.showSecondManager.set(false);
      this.secondManagerEmployeeId.set('');
      this.markFormDirty();
    }
    this.resetValidation();
  }

  toggleComponent(key: string): void {
    const component = this.components().find((item) => item.key === key);
    if (!component || component.required) return;
    this.componentSelections.update((selections) => ({
      ...selections,
      [key]: !component.selected
    }));
    this.markFormDirty();
  }

  addDepartment(): void {
    this.multiDepartments.update((rows) => [...rows, {
      id: this.nextDepartmentId++,
      title: '',
      departmentId: '',
      managerEmployeeId: '',
      startDate: '',
      endDate: ''
    }]);
    this.markFormDirty();
  }

  removeDepartmentRow(id: number): void {
    this.multiDepartments.update((rows) => {
      const remaining = rows.filter((row) => row.id !== id);
      return remaining.length ? remaining : [{
        id: this.nextDepartmentId++,
        title: '',
        departmentId: '',
        managerEmployeeId: '',
        startDate: '',
        endDate: ''
      }];
    });
    this.markFormDirty();
    this.revalidateAfterChange();
  }

  reorderDepartmentRows(event: CdkDragDrop<DepartmentRow[]>): void {
    this.multiDepartments.update((rows) => {
      const next = [...rows];
      moveItemInArray(next, event.previousIndex, event.currentIndex);
      return next;
    });
    if (event.previousIndex !== event.currentIndex) this.markFormDirty();
  }

  updateMultiDepartmentTitle(id: number, value: string): void {
    this.multiDepartments.update((rows) => rows.map((row) => row.id === id ? { ...row, title: value } : row));
    this.revalidateAfterChange();
  }

  updateDepartmentRow(
    id: number,
    field: 'departmentId' | 'managerEmployeeId' | 'startDate' | 'endDate',
    value: string
  ): void {
    this.multiDepartments.update((rows) => rows.map((row) => {
      if (row.id !== id) return row;
      if (field !== 'departmentId') return { ...row, [field]: value };

      const department = this.departmentCandidates().find((candidate) => candidate.id === value);
      return {
        ...row,
        departmentId: value,
        managerEmployeeId: department?.headEmployeeId ?? ''
      };
    }));
    this.revalidateAfterChange();
  }

  departmentManagerCandidates(departmentId: string): EmployeeDto[] {
    if (!departmentId) return [];
    const headEmployeeId = this.departmentCandidates()
      .find((department) => department.id === departmentId)?.headEmployeeId;
    return this.allEmployees().filter((employee) =>
      employee.departmentId === departmentId || employee.id === headEmployeeId
    );
  }

  openTemplateBuilder(): void {
    this.router.navigate(['/projects/templates/new']);
  }

  onTemplateSelected(templateId: string | null, markDirty = true): void {
    const templateChanged = this.selectedTemplateId() !== templateId;
    const template = this.templates().find((item) => item.id === templateId);
    this.selectedTemplateId.set(template?.id ?? null);
    if (template) {
      this.mode.set(template.applicableProjectType === 'Simple' ? 'simple' : 'multi');
    }
    this.showSecondManager.set(false);
    this.secondManagerEmployeeId.set('');
    this.componentSelections.set({});
    this.templateValues.set({});
    this.templateFiles.clear();
    if (markDirty && templateChanged) this.markFormDirty();
    this.resetValidation();
  }

  updateTemplateValue(fieldId: string, value: string | boolean): void {
    this.templateValues.update((values) => ({ ...values, [fieldId]: value }));
    this.revalidateAfterChange();
  }

  getTemplateValue(fieldId: string): string | boolean {
    return this.templateValues()[fieldId] ?? '';
  }

  onTemplateFileSelected(fieldId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      this.templateFiles.delete(fieldId);
      this.updateTemplateValue(fieldId, '');
      return;
    }
    this.templateFiles.set(fieldId, file);
    this.updateTemplateValue(fieldId, file.name);
  }

  updateProjectName(value: string): void {
    this.projectName.set(value);
    this.revalidateAfterChange();
  }

  updateDescription(value: string): void {
    this.description.set(value);
    this.revalidateAfterChange();
  }

  updateStartDate(value: string): void {
    this.startDate.set(value);
    this.revalidateAfterChange();
  }

  updateEndDate(value: string): void {
    this.endDate.set(value);
    this.revalidateAfterChange();
  }

  updateManagerEmployeeId(value: string): void {
    this.managerEmployeeId.set(value);
    this.revalidateAfterChange();
  }

  updateUnitDepartmentId(value: string): void {
    this.unitDepartmentId.set(value);
    this.revalidateAfterChange();
  }

  addSecondManager(): void {
    if (this.mode() === 'simple' || this.showSecondManager()) return;
    this.showSecondManager.set(true);
    this.markFormDirty();
  }

  removeSecondManager(): void {
    this.showSecondManager.set(false);
    this.secondManagerEmployeeId.set('');
    this.markFormDirty();
  }

  updateSecondManagerEmployeeId(value: string): void {
    this.secondManagerEmployeeId.set(value);
    this.revalidateAfterChange();
  }

  updateBudget(value: string): void {
    this.budget.set(value);
    this.revalidateAfterChange();
  }

  updateCurrency(value: string): void {
    this.currency.set(value);
    this.revalidateAfterChange();
  }

  hasFieldError(key: string): boolean {
    return Boolean(this.validationErrors()[key]);
  }

  fieldError(key: string): string {
    return this.validationErrors()[key] ?? '';
  }

  templateFieldErrorKey(fieldId: string): string {
    return `template.${fieldId}`;
  }

  departmentFieldErrorKey(
    rowId: number,
    field: 'title' | 'departmentId' | 'managerEmployeeId' | 'startDate' | 'endDate'
  ): string {
    return `department.${rowId}.${field}`;
  }

  departmentRowErrors(rowId: number): string[] {
    const prefix = `department.${rowId}.`;
    return [...new Set(
      this.validationErrorEntries()
        .filter(([key]) => key.startsWith(prefix))
        .map(([, message]) => message)
    )];
  }

  isWideTemplateField(field: TemplateFieldDto): boolean {
    return ['textarea', 'checkbox', 'formGroup', 'table', 'attachment', 'image']
      .includes(this.normalizedContentType(field));
  }

  isChecklistItemChecked(fieldId: string, option: string): boolean {
    return this.readChecklistValue(fieldId).includes(option);
  }

  toggleChecklistItem(fieldId: string, option: string, checked: boolean): void {
    const selected = new Set(this.readChecklistValue(fieldId));
    checked ? selected.add(option) : selected.delete(option);
    this.updateTemplateValue(fieldId, JSON.stringify([...selected]));
  }

  getTableCell(fieldId: string, row: number, column: number): string {
    return this.readTableValue(fieldId)[`${row}:${column}`] ?? '';
  }

  updateTableCell(fieldId: string, row: number, column: number, value: string): void {
    const table = this.readTableValue(fieldId);
    table[`${row}:${column}`] = value;
    this.updateTemplateValue(fieldId, JSON.stringify(table));
  }

  getFormGroupValue(fieldId: string, subfield: string): string {
    return this.readTableValue(fieldId)[subfield] ?? '';
  }

  updateFormGroupValue(fieldId: string, subfield: string, value: string): void {
    const group = this.readTableValue(fieldId);
    group[subfield] = value;
    this.updateTemplateValue(fieldId, JSON.stringify(group));
  }

  normalizedContentType(field: TemplateFieldDto): string {
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
    return legacyTypes[field.contentType] ?? field.contentType;
  }

  normalizedFieldKind(field: TemplateFieldDto): TemplateFieldKind {
    return field.kind ?? (this.normalizedContentType(field) === 'section' ? 'Section' : 'Custom');
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.attachmentFiles = Array.from(input.files ?? []);
    this.attachmentNames.set(this.attachmentFiles.map((file) => file.name));
    this.revalidateAfterChange();
  }

  cancel(): void {
    void this.router.navigate(['/projects']);
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

  async save(): Promise<void> {
    this.validationAttempted.set(true);
    this.validationErrors.set(this.buildValidationErrors());
    if (this.validationErrorEntries().length) {
      this.errorMessage.set('');
      this.focusFirstValidationError();
      return;
    }

    const selectedDepartmentRows = this.selectedDepartmentRows();
    const resolvedUnitDepartmentId = selectedDepartmentRows[0].departmentId;

    this.errorMessage.set('');
    this.saving.set(true);

    try {
      const departments = selectedDepartmentRows.map((row) => ({
        departmentId: row.departmentId,
        title: this.mode() === 'simple' ? '' : row.title.trim(),
        departmentName: this.departmentCandidates()
          .find((department) => department.id === row.departmentId)?.name ?? '',
        managerEmployeeId: row.managerEmployeeId,
        managerName: this.allEmployees()
          .find((employee) => employee.id === row.managerEmployeeId)?.displayName ?? '',
        startDate: this.mode() === 'simple' ? null : row.startDate,
        endDate: this.mode() === 'simple' ? null : row.endDate
      }));

      const selectedManager = this.managerCandidates()
        .find((employee) => employee.id === this.managerEmployeeId());
      const selectedSecondManager = this.managerCandidates()
        .find((employee) => employee.id === this.secondManagerEmployeeId());
      const selectedUnit = this.departmentCandidates()
        .find((department) => department.id === resolvedUnitDepartmentId);

      const project = await this.projectApi.create({
        name: this.projectName(),
        description: this.description(),
        managerEmployeeId: this.managerEmployeeId(),
        managerName: selectedManager?.displayName ?? '',
        secondManagerEmployeeId: selectedSecondManager?.id ?? null,
        secondManagerName: selectedSecondManager?.displayName ?? null,
        unitDepartmentId: resolvedUnitDepartmentId,
        unit: selectedUnit?.name ?? '',
        // Şablonun kendi tipi (örn. FeasibilityBased) modun iki seçeneğinden (Basit/Çok Birimli)
        // daha kesin bir kaynaktır — şablon seçiliyse onun tipi korunur, aksi halde mod esas alınır.
        type: this.selectedTemplate()?.applicableProjectType ?? MODE_TO_TYPE[this.mode()],
        budget: this.mode() === 'simple' ? 0 : Number(this.budget().replace(/[^0-9]/g, '')) || 0,
        currency: CURRENCY_CODES[this.currency()] ?? 'TRY',
        startDate: this.startDate(),
        endDate: this.endDate(),
        templateId: this.selectedTemplateId(),
        enabledComponents: this.components().filter((c) => c.selected).map((c) => c.key),
        templateValues: this.activeTemplateFields()
          .map((field) => ({
            fieldId: field.id,
            value: String(this.templateValues()[field.id] ?? '')
          })),
        departments
      });

      const failedUploads: string[] = [];
      for (const file of [...this.attachmentFiles, ...this.templateFiles.values()]) {
        try {
          await this.taskApi.uploadDocument(project.id, file, { uploadedBy: this.currentUserName() });
        } catch {
          // A single attachment failing to upload shouldn't block project creation from succeeding —
          // but the user still needs to know which files didn't make it.
          failedUploads.push(file.name);
        }
      }

      this.toastService.success('Proje oluşturuldu.');
      if (failedUploads.length) {
        this.toastService.error(`Şu dosyalar yüklenemedi: ${failedUploads.join(', ')}`);
      }
      this.router.navigate(['/projects', project.id]);
    } catch (error) {
      this.errorMessage.set(error instanceof Error ? error.message : 'Proje oluşturulamadı.');
    } finally {
      this.saving.set(false);
    }
  }

  private isSchemaFieldApplicable(field: TemplateFieldDto): boolean {
    if (this.normalizedFieldKind(field) !== 'System') return true;
    if (field.systemKey === 'unit') return false;
    if (field.systemKey === 'budget') return this.mode() !== 'simple';
    if (field.systemKey === 'secondManager') return this.mode() !== 'simple' && this.showSecondManager();
    return true;
  }

  private readChecklistValue(fieldId: string): string[] {
    const value = this.templateValues()[fieldId];
    if (typeof value !== 'string' || !value) return [];
    try {
      const parsed = JSON.parse(value);
      return Array.isArray(parsed)
        ? parsed.filter((item): item is string => typeof item === 'string')
        : [];
    } catch {
      return [];
    }
  }

  private readTableValue(fieldId: string): Record<string, string> {
    const value = this.templateValues()[fieldId];
    if (typeof value !== 'string' || !value) return {};
    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
        ? parsed as Record<string, string>
        : {};
    } catch {
      return {};
    }
  }

  private selectedDepartmentRows(): DepartmentRow[] {
    return this.multiDepartments().filter((row) =>
      this.mode() === 'simple'
        ? Boolean(row.departmentId || row.managerEmployeeId)
        : Boolean(row.title.trim() || row.departmentId || row.managerEmployeeId || row.startDate || row.endDate)
    );
  }

  private buildValidationErrors(): Record<string, string> {
    const errors: Record<string, string> = {};
    const add = (key: string, message: string): void => {
      if (!errors[key]) errors[key] = message;
    };

    if (!this.projectName().trim()) add('projectName', 'Proje adı girilmelidir.');
    if (!this.startDate()) add('startDate', 'Başlangıç tarihi seçilmelidir.');
    if (!this.endDate()) add('endDate', 'Bitiş tarihi seçilmelidir.');
    if (this.startDate() && this.endDate() && this.endDate() < this.startDate()) {
      add('endDate', 'Bitiş tarihi başlangıç tarihinden önce olamaz.');
    }
    if (!this.managerEmployeeId()) add('managerEmployeeId', 'Proje yöneticisi seçilmelidir.');

    const descriptionRequired = this.projectFormSchema().some((field) =>
      this.normalizedFieldKind(field) === 'System' &&
      field.systemKey === 'description' &&
      field.isRequired
    );
    if (descriptionRequired && !this.description().trim()) {
      add('description', 'Proje açıklaması girilmelidir.');
    }

    if (this.mode() === 'multi') {
      const numericBudget = Number(this.budget().replace(/[^0-9]/g, ''));
      if (!this.budget().trim() || !Number.isFinite(numericBudget) || numericBudget <= 0) {
        add('budget', 'Bütçe sıfırdan büyük bir tutar olmalıdır.');
      }
    }

    const selectedRows = this.selectedDepartmentRows();
    const rowsToValidate = selectedRows.length ? selectedRows : this.multiDepartments().slice(0, 1);
    for (const row of rowsToValidate) {
      if (this.mode() === 'multi' && !row.title.trim()) {
        add(this.departmentFieldErrorKey(row.id, 'title'), 'İş paketi başlığı girilmelidir.');
      }
      if (!row.departmentId) {
        add(this.departmentFieldErrorKey(row.id, 'departmentId'), 'Departman seçilmelidir.');
      }
      if (!row.managerEmployeeId) {
        add(this.departmentFieldErrorKey(row.id, 'managerEmployeeId'), 'Departman yöneticisi seçilmelidir.');
      }
      if (this.mode() !== 'multi') continue;

      if (!row.startDate) {
        add(this.departmentFieldErrorKey(row.id, 'startDate'), 'İş paketi başlangıç tarihi seçilmelidir.');
      }
      if (!row.endDate) {
        add(this.departmentFieldErrorKey(row.id, 'endDate'), 'İş paketi bitiş tarihi seçilmelidir.');
      }
      if (row.startDate && row.endDate && row.endDate < row.startDate) {
        add(this.departmentFieldErrorKey(row.id, 'endDate'), 'İş paketi bitiş tarihi başlangıçtan önce olamaz.');
      }
      if (row.startDate && this.startDate() && row.startDate < this.startDate()) {
        add(this.departmentFieldErrorKey(row.id, 'startDate'), 'İş paketi proje başlangıcından önce başlayamaz.');
      }
      if (row.endDate && this.endDate() && row.endDate > this.endDate()) {
        add(this.departmentFieldErrorKey(row.id, 'endDate'), 'İş paketi proje bitişinden sonra bitemez.');
      }
    }

    for (const field of this.activeTemplateFields()) {
      if (!field.isRequired || this.normalizedContentType(field) === 'section') continue;
      const key = this.templateFieldErrorKey(field.id);
      const value = this.templateValues()[field.id];
      const contentType = this.normalizedContentType(field);
      if (contentType === 'checklist' && this.readChecklistValue(field.id).length === 0) {
        add(key, `${field.label} alanından en az bir seçim yapılmalıdır.`);
      } else if (
        ['table', 'formGroup'].includes(contentType) &&
        !Object.values(this.readTableValue(field.id)).some((cell) => cell.trim())
      ) {
        add(key, `${field.label} alanı doldurulmalıdır.`);
      } else if (
        value === undefined ||
        value === null ||
        value === '' ||
        (contentType === 'checkbox' && value !== true)
      ) {
        add(key, `${field.label} alanı zorunludur.`);
      }
    }

    return errors;
  }

  private revalidateAfterChange(): void {
    this.markFormDirty();
    this.errorMessage.set('');
    if (this.validationAttempted()) {
      this.validationErrors.set(this.buildValidationErrors());
    }
  }

  private markFormDirty(): void {
    this.formDirty.set(true);
  }

  private resetValidation(): void {
    this.validationAttempted.set(false);
    this.validationErrors.set({});
    this.errorMessage.set('');
  }

  private focusFirstValidationError(): void {
    const firstKey = this.validationErrorEntries()[0]?.[0];
    if (!firstKey) return;

    setTimeout(() => {
      const host = this.hostElement.nativeElement as HTMLElement;
      const container = host.querySelector(
        `[data-validation-key="${firstKey}"]`
      ) as HTMLElement | null;
      if (!container) return;
      if (typeof container.scrollIntoView === 'function') {
        container.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
      const focusTarget = container.matches('input, select, textarea, button')
        ? container
        : container.querySelector('input, select, textarea, button') as HTMLElement | null;
      focusTarget?.focus({ preventScroll: true });
    });
  }
}
