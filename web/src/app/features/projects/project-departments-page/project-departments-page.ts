import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../shared/auth/auth.service';
import { Icon, IconName } from '../../../shared/icon/icon';
import { ToastService } from '../../../shared/toast/toast.service';
import { DepartmentDetailDto, DepartmentDto } from '../data/department-api.models';
import { DepartmentApiService } from '../data/department-api.service';
import { EmployeeDto } from '../data/employee-api.models';
import { EmployeeApiService } from '../data/employee-api.service';

type OrganizationTab = 'overview' | 'employees' | 'departments';
type StatusFilter = 'all' | 'active' | 'inactive';
type DeleteCandidate =
  | { kind: 'employee'; id: string; name: string }
  | { kind: 'department'; id: string; name: string };

interface RailItem {
  icon: IconName;
  label: string;
  active?: boolean;
}

const ALL_ROLES = ['Admin', 'ProjectManager', 'Approver', 'Member'] as const;
const OPERATIONAL_ROLES = ['Approver', 'Member'] as const;

interface EmployeeDraft {
  displayName: string;
  email: string;
  password: string;
  title: string;
  departmentId: string;
  roles: Set<string>;
}

function emptyEmployeeDraft(): EmployeeDraft {
  return {
    displayName: '',
    email: '',
    password: '',
    title: '',
    departmentId: '',
    roles: new Set(['Member'])
  };
}

@Component({
  selector: 'app-project-departments-page',
  standalone: true,
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './project-departments-page.html',
  styleUrl: './project-departments-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDepartmentsPage implements OnInit {
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly authService = inject(AuthService);
  private readonly departmentApi = inject(DepartmentApiService);
  private readonly employeeApi = inject(EmployeeApiService);

  readonly currentUserName = computed(() => this.authService.currentUser()?.displayName ?? '');
  readonly isAdmin = computed(() => this.authService.hasAnyRole(['Admin']));
  readonly canManage = computed(() => this.authService.hasAnyRole(['Admin', 'ProjectManager']));
  readonly availableRoles = computed<readonly string[]>(() => this.isAdmin() ? ALL_ROLES : OPERATIONAL_ROLES);

  readonly railCompact = signal(false);
  readonly profileOpen = signal(false);
  readonly activeTab = signal<OrganizationTab>('overview');
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly departments = signal<DepartmentDto[]>([]);
  readonly employees = signal<EmployeeDto[]>([]);

  readonly employeeSearch = signal('');
  readonly employeeStatusFilter = signal<StatusFilter>('all');
  readonly employeeDepartmentFilter = signal('');
  readonly departmentSearch = signal('');
  readonly departmentStatusFilter = signal<StatusFilter>('active');

  readonly activeDepartments = computed(() => this.departments().filter((department) => department.isActive !== false));
  readonly activeEmployees = computed(() => this.employees().filter((employee) => employee.isActive !== false));
  readonly selectableEmployees = computed(() =>
    this.activeEmployees().filter((employee) => employee.isSelectable !== false)
  );

  readonly filteredEmployees = computed(() => {
    const query = this.employeeSearch().trim().toLocaleLowerCase('tr-TR');
    const status = this.employeeStatusFilter();
    const departmentId = this.employeeDepartmentFilter();
    return this.employees().filter((employee) => {
      const isActive = employee.isActive !== false;
      const matchesStatus = status === 'all' || (status === 'active' ? isActive : !isActive);
      const matchesDepartment =
        !departmentId ||
        (departmentId === 'unassigned' ? !employee.departmentId : employee.departmentId === departmentId);
      const haystack = `${employee.displayName} ${employee.email} ${employee.title} ${employee.departmentName ?? ''} ${employee.roles.join(' ')}`.toLocaleLowerCase('tr-TR');
      return matchesStatus && matchesDepartment && (!query || haystack.includes(query));
    });
  });

  readonly filteredDepartments = computed(() => {
    const query = this.departmentSearch().trim().toLocaleLowerCase('tr-TR');
    const status = this.departmentStatusFilter();
    return this.departments().filter((department) => {
      const isActive = department.isActive !== false;
      const matchesStatus = status === 'all' || (status === 'active' ? isActive : !isActive);
      const haystack = `${department.name} ${department.headDisplayName ?? ''}`.toLocaleLowerCase('tr-TR');
      return matchesStatus && (!query || haystack.includes(query));
    });
  });

  readonly employeesWithoutDepartment = computed(() =>
    this.activeEmployees().filter((employee) => !employee.departmentId && employee.isSelectable !== false)
  );
  readonly departmentsWithoutHead = computed(() =>
    this.activeDepartments().filter((department) => !department.headEmployeeId)
  );
  readonly projectManagerCount = computed(() =>
    this.activeEmployees().filter((employee) => employee.roles.includes('ProjectManager')).length
  );
  readonly inactiveEmployeeCount = computed(() =>
    this.employees().filter((employee) => employee.isActive === false).length
  );

  readonly selectedDepartmentId = signal<string | null>(null);
  readonly selectedDepartmentDetail = signal<DepartmentDetailDto | null>(null);
  readonly detailLoading = signal(false);
  readonly assignMemberEmployeeId = signal('');
  readonly savingMember = signal(false);
  readonly savingHead = signal(false);

  readonly departmentDialogOpen = signal(false);
  readonly editingDepartmentId = signal<string | null>(null);
  readonly departmentNameDraft = signal('');
  readonly departmentHeadDraft = signal('');
  readonly savingDepartment = signal(false);
  readonly departmentFormError = signal('');

  readonly employeeDialogOpen = signal(false);
  readonly editingEmployeeId = signal<string | null>(null);
  readonly employeeDraft = signal<EmployeeDraft>(emptyEmployeeDraft());
  readonly savingEmployee = signal(false);
  readonly employeeFormError = signal('');

  readonly passwordDialogEmployee = signal<EmployeeDto | null>(null);
  readonly passwordDraft = signal('');
  readonly savingPassword = signal(false);
  readonly deleteCandidate = signal<DeleteCandidate | null>(null);
  readonly deleteConfirmation = signal('');
  readonly deletingRecord = signal(false);
  readonly deleteError = signal('');

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
    { icon: 'building', label: 'Organizasyon', active: true },
    { icon: 'wallet', label: 'Bütçe' },
    { icon: 'apps', label: 'Projeler' },
    { icon: 'dashboard', label: 'Raporlar' }
  ];

  readonly utilityRail: RailItem[] = [
    { icon: 'projects', label: 'Arşiv' },
    { icon: 'feasibility', label: 'Şablonlar' },
    { icon: 'check', label: 'Kontroller' },
    { icon: 'sliders', label: 'Ayarlar' }
  ];

  async ngOnInit(): Promise<void> {
    await this.loadAll();
  }

  selectTab(tab: OrganizationTab): void {
    this.activeTab.set(tab);
  }

  retryLoad(): void {
    this.loadAll();
  }

  showUnassignedEmployees(): void {
    this.employeeDepartmentFilter.set('unassigned');
    this.employeeStatusFilter.set('active');
    this.activeTab.set('employees');
  }

  showDepartmentsWithoutHead(): void {
    this.departmentStatusFilter.set('active');
    this.departmentSearch.set('');
    this.activeTab.set('departments');
  }

  async selectDepartment(id: string): Promise<void> {
    this.selectedDepartmentId.set(id);
    this.assignMemberEmployeeId.set('');
    this.detailLoading.set(true);
    try {
      this.selectedDepartmentDetail.set(await this.departmentApi.getById(id));
    } catch {
      this.toastService.error('Departman detayı yüklenemedi.');
    } finally {
      this.detailLoading.set(false);
    }
  }

  openCreateDepartment(): void {
    this.editingDepartmentId.set(null);
    this.departmentNameDraft.set('');
    this.departmentHeadDraft.set('');
    this.departmentFormError.set('');
    this.departmentDialogOpen.set(true);
  }

  openEditDepartment(department: DepartmentDto): void {
    this.editingDepartmentId.set(department.id);
    this.departmentNameDraft.set(department.name);
    this.departmentHeadDraft.set(department.headEmployeeId ?? '');
    this.departmentFormError.set('');
    this.departmentDialogOpen.set(true);
  }

  closeDepartmentDialog(): void {
    this.departmentDialogOpen.set(false);
    this.departmentFormError.set('');
  }

  async saveDepartment(): Promise<void> {
    const name = this.departmentNameDraft().trim();
    if (!name || this.savingDepartment()) {
      this.departmentFormError.set('Departman adı zorunludur.');
      return;
    }

    this.savingDepartment.set(true);
    try {
      const editingId = this.editingDepartmentId();
      if (editingId) {
        await this.departmentApi.update(editingId, {
          name,
          headEmployeeId: this.departmentHeadDraft() || null,
          updateHead: true
        });
        this.toastService.success('Departman güncellendi.');
      } else {
        await this.departmentApi.create({ name, headEmployeeId: this.departmentHeadDraft() || null });
        this.toastService.success('Departman oluşturuldu.');
      }
      await this.loadAll();
      this.closeDepartmentDialog();
    } catch (error) {
      this.departmentFormError.set(error instanceof Error ? error.message : 'Departman kaydedilemedi.');
    } finally {
      this.savingDepartment.set(false);
    }
  }

  async setDepartmentStatus(department: DepartmentDto): Promise<void> {
    try {
      await this.departmentApi.setStatus(department.id, { isActive: department.isActive === false });
      await this.loadAll();
      this.toastService.success(department.isActive === false ? 'Departman yeniden aktifleştirildi.' : 'Departman arşivlendi.');
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Departman durumu değiştirilemedi.');
    }
  }

  async changeHead(departmentId: string, headEmployeeId: string): Promise<void> {
    if (this.savingHead()) return;
    this.savingHead.set(true);
    try {
      await this.departmentApi.assignHead(departmentId, { headEmployeeId: headEmployeeId || null });
      await this.loadAll();
      await this.selectDepartment(departmentId);
      this.toastService.success('Departman sorumlusu güncellendi.');
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Sorumlu atanamadı.');
    } finally {
      this.savingHead.set(false);
    }
  }

  openCreateEmployee(): void {
    this.editingEmployeeId.set(null);
    this.employeeDraft.set(emptyEmployeeDraft());
    this.employeeFormError.set('');
    this.employeeDialogOpen.set(true);
  }

  openEditEmployee(employee: EmployeeDto): void {
    if (!this.canEditEmployee(employee)) return;
    this.editingEmployeeId.set(employee.id);
    this.employeeDraft.set({
      displayName: employee.displayName,
      email: employee.email,
      password: '',
      title: employee.title,
      departmentId: employee.departmentId ?? '',
      roles: new Set(employee.roles)
    });
    this.employeeFormError.set('');
    this.employeeDialogOpen.set(true);
  }

  closeEmployeeDialog(): void {
    this.employeeDialogOpen.set(false);
    this.employeeFormError.set('');
  }

  updateEmployeeDraft<K extends keyof EmployeeDraft>(field: K, value: EmployeeDraft[K]): void {
    this.employeeDraft.update((draft) => ({ ...draft, [field]: value }));
  }

  toggleEmployeeRole(role: string): void {
    this.employeeDraft.update((draft) => {
      const roles = new Set(draft.roles);
      roles.has(role) ? roles.delete(role) : roles.add(role);
      return {
        ...draft,
        roles,
        departmentId: roles.has('Admin') ? '' : draft.departmentId
      };
    });
  }

  canEditEmployee(employee: EmployeeDto): boolean {
    return this.isAdmin() || !employee.roles.some((role) => role === 'Admin' || role === 'ProjectManager');
  }

  async saveEmployee(): Promise<void> {
    if (this.savingEmployee()) return;
    const draft = this.employeeDraft();
    const editingId = this.editingEmployeeId();
    if (!draft.displayName.trim() || !draft.email.trim()) {
      this.employeeFormError.set('Ad ve e-posta zorunludur.');
      return;
    }
    if (!editingId && draft.password.length < 4) {
      this.employeeFormError.set('Şifre en az 4 karakter olmalıdır.');
      return;
    }
    if (draft.roles.size === 0) {
      this.employeeFormError.set('En az bir rol seçilmelidir.');
      return;
    }

    this.savingEmployee.set(true);
    const request = {
      displayName: draft.displayName.trim(),
      email: draft.email.trim(),
      departmentId: draft.departmentId || null,
      title: draft.title.trim(),
      roles: Array.from(draft.roles)
    };
    try {
      if (editingId) {
        await this.employeeApi.update(editingId, request);
        this.toastService.success('Çalışan bilgileri güncellendi.');
      } else {
        await this.employeeApi.create({ ...request, password: draft.password });
        this.toastService.success('Çalışan hesabı oluşturuldu.');
      }
      await this.loadAll();
      this.closeEmployeeDialog();
    } catch (error) {
      this.employeeFormError.set(error instanceof Error ? error.message : 'Çalışan kaydedilemedi.');
    } finally {
      this.savingEmployee.set(false);
    }
  }

  async setEmployeeStatus(employee: EmployeeDto): Promise<void> {
    if (!this.isAdmin()) return;
    try {
      await this.employeeApi.setStatus(employee.id, { isActive: employee.isActive === false });
      await this.loadAll();
      this.toastService.success(employee.isActive === false ? 'Çalışan aktifleştirildi.' : 'Çalışan pasife alındı.');
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Çalışan durumu değiştirilemedi.');
    }
  }

  openEmployeeDelete(employee: EmployeeDto): void {
    if (!this.isAdmin() || employee.isActive !== false || employee.roles.includes('Admin')) return;
    this.openDeleteDialog({ kind: 'employee', id: employee.id, name: employee.displayName });
  }

  openDepartmentDelete(department: DepartmentDto): void {
    if (!this.isAdmin() || department.isActive !== false) return;
    this.openDeleteDialog({ kind: 'department', id: department.id, name: department.name });
  }

  closeDeleteDialog(): void {
    if (this.deletingRecord()) return;
    this.deleteCandidate.set(null);
    this.deleteConfirmation.set('');
    this.deleteError.set('');
  }

  async confirmPermanentDelete(): Promise<void> {
    const candidate = this.deleteCandidate();
    if (!candidate || this.deleteConfirmation().trim().toLocaleUpperCase('tr-TR') !== 'SİL' || this.deletingRecord()) {
      return;
    }

    this.deletingRecord.set(true);
    this.deleteError.set('');
    try {
      if (candidate.kind === 'employee') {
        await this.employeeApi.delete(candidate.id);
      } else {
        await this.departmentApi.delete(candidate.id);
        if (this.selectedDepartmentId() === candidate.id) {
          this.selectedDepartmentId.set(null);
          this.selectedDepartmentDetail.set(null);
        }
      }
      await this.loadAll();
      this.toastService.success(`${candidate.name} kalıcı olarak silindi.`);
      this.deleteCandidate.set(null);
      this.deleteConfirmation.set('');
    } catch (error) {
      this.deleteError.set(error instanceof Error ? error.message : 'Kayıt kalıcı olarak silinemedi.');
    } finally {
      this.deletingRecord.set(false);
    }
  }

  openPasswordDialog(employee: EmployeeDto): void {
    this.passwordDialogEmployee.set(employee);
    this.passwordDraft.set('');
  }

  closePasswordDialog(): void {
    this.passwordDialogEmployee.set(null);
    this.passwordDraft.set('');
  }

  async savePassword(): Promise<void> {
    const employee = this.passwordDialogEmployee();
    if (!employee || this.passwordDraft().length < 4 || this.savingPassword()) return;
    this.savingPassword.set(true);
    try {
      await this.employeeApi.resetPassword(employee.id, { newPassword: this.passwordDraft() });
      this.toastService.success('Çalışan şifresi güncellendi.');
      this.closePasswordDialog();
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Şifre güncellenemedi.');
    } finally {
      this.savingPassword.set(false);
    }
  }

  async addMemberToSelectedDepartment(): Promise<void> {
    const departmentId = this.selectedDepartmentId();
    const employeeId = this.assignMemberEmployeeId();
    if (!departmentId || !employeeId || this.savingMember()) return;
    this.savingMember.set(true);
    try {
      await this.employeeApi.assignDepartment(employeeId, { departmentId });
      await this.loadAll();
      await this.selectDepartment(departmentId);
      this.toastService.success('Çalışan departmana atandı.');
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Çalışan atanamadı.');
    } finally {
      this.savingMember.set(false);
    }
  }

  async removeMember(employeeId: string): Promise<void> {
    if (this.savingMember()) return;
    const departmentId = this.selectedDepartmentId();
    this.savingMember.set(true);
    try {
      await this.employeeApi.assignDepartment(employeeId, { departmentId: null });
      await this.loadAll();
      if (departmentId) await this.selectDepartment(departmentId);
      this.toastService.success('Çalışan departmandan çıkarıldı.');
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'İşlem başarısız oldu.');
    } finally {
      this.savingMember.set(false);
    }
  }

  initials(name: string): string {
    return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]?.toLocaleUpperCase('tr-TR')).join('');
  }

  roleLabel(role: string): string {
    return role === 'ProjectManager' ? 'Proje Yöneticisi' : role === 'Approver' ? 'Onaycı' : role === 'Member' ? 'Çalışan' : 'Admin';
  }

  goToProjects(): void {
    this.router.navigate(['/projects']);
  }

  goToTemplates(): void {
    this.router.navigate(['/projects/templates']);
  }

  logout(): void {
    this.authService.logout();
  }

  private openDeleteDialog(candidate: DeleteCandidate): void {
    this.deleteCandidate.set(candidate);
    this.deleteConfirmation.set('');
    this.deleteError.set('');
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const selectedDepartmentId = this.selectedDepartmentId();
      const [departments, employees, selectedDetail] = await Promise.all([
        this.departmentApi.list(true),
        this.employeeApi.list({ includeInactive: true }),
        selectedDepartmentId
          ? this.departmentApi.getById(selectedDepartmentId).catch(() => null)
          : Promise.resolve(null)
      ]);
      this.departments.set(departments);
      this.employees.set(employees);
      if (selectedDepartmentId && this.selectedDepartmentId() === selectedDepartmentId) {
        if (selectedDetail) {
          this.selectedDepartmentDetail.set(selectedDetail);
        } else {
          this.selectedDepartmentId.set(null);
          this.selectedDepartmentDetail.set(null);
        }
      }
    } catch (error) {
      this.loadError.set(error instanceof Error ? error.message : 'Organizasyon bilgileri yüklenemedi.');
    } finally {
      this.loading.set(false);
    }
  }
}
