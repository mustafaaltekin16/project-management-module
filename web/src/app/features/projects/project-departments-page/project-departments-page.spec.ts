import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../../shared/auth/auth.service';
import { ToastService } from '../../../shared/toast/toast.service';
import { DepartmentDto } from '../data/department-api.models';
import { DepartmentApiService } from '../data/department-api.service';
import { EmployeeDto } from '../data/employee-api.models';
import { EmployeeApiService } from '../data/employee-api.service';
import { ProjectDepartmentsPage } from './project-departments-page';

const departments: DepartmentDto[] = [
  {
    id: 'department-1',
    name: 'Bilgi Teknolojileri',
    headEmployeeId: 'employee-1',
    headDisplayName: 'Ayşe Yönetici',
    memberCount: 1,
    isActive: true
  },
  {
    id: 'department-2',
    name: 'Arşiv Birimi',
    headEmployeeId: null,
    headDisplayName: null,
    memberCount: 0,
    isActive: false
  }
];

const employees: EmployeeDto[] = [
  {
    id: 'employee-1',
    displayName: 'Ayşe Yönetici',
    email: 'ayse@example.com',
    departmentId: 'department-1',
    departmentName: 'Bilgi Teknolojileri',
    title: 'Proje Yöneticisi',
    roles: ['ProjectManager'],
    isActive: true,
    isSelectable: true
  },
  {
    id: 'employee-2',
    displayName: 'Mehmet Uzman',
    email: 'mehmet@example.com',
    departmentId: null,
    departmentName: null,
    title: 'Uzman',
    roles: ['Member'],
    isActive: true,
    isSelectable: true
  },
  {
    id: 'employee-3',
    displayName: 'Pasif Çalışan',
    email: 'pasif@example.com',
    departmentId: null,
    departmentName: null,
    title: '',
    roles: ['Member'],
    isActive: false,
    isSelectable: true
  }
];

describe('ProjectDepartmentsPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectDepartmentsPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({ userId: 'admin-1', displayName: 'Admin', roles: ['Admin'] }),
            hasAnyRole: (roles: string[]) => roles.includes('Admin'),
            logout: () => undefined
          }
        },
        {
          provide: DepartmentApiService,
          useValue: {
            list: async () => departments,
            getById: async () => ({ ...departments[0], members: [employees[0]] }),
            create: async () => departments[0],
            update: async () => departments[0],
            assignHead: async () => departments[0],
            setStatus: async () => departments[0],
            delete: async () => undefined
          }
        },
        {
          provide: EmployeeApiService,
          useValue: {
            list: async () => employees,
            create: async () => employees[0],
            update: async () => employees[0],
            assignDepartment: async () => employees[0],
            setStatus: async () => employees[0],
            resetPassword: async () => undefined,
            delete: async () => undefined
          }
        },
        {
          provide: ToastService,
          useValue: { success: () => undefined, error: () => undefined }
        }
      ]
    }).compileComponents();
  });

  it('calculates organization health indicators', () => {
    const component = TestBed.createComponent(ProjectDepartmentsPage).componentInstance;
    component.departments.set(departments);
    component.employees.set(employees);

    expect(component.activeDepartments()).toHaveLength(1);
    expect(component.activeEmployees()).toHaveLength(2);
    expect(component.employeesWithoutDepartment().map((employee) => employee.id)).toEqual(['employee-2']);
    expect(component.projectManagerCount()).toBe(1);
    expect(component.inactiveEmployeeCount()).toBe(1);
  });

  it('filters employees by text, status and department state', () => {
    const component = TestBed.createComponent(ProjectDepartmentsPage).componentInstance;
    component.employees.set(employees);

    component.employeeSearch.set('uzman');
    component.employeeDepartmentFilter.set('unassigned');
    component.employeeStatusFilter.set('active');

    expect(component.filteredEmployees().map((employee) => employee.id)).toEqual(['employee-2']);
  });

  it('opens attention items in their related filtered tabs', () => {
    const component = TestBed.createComponent(ProjectDepartmentsPage).componentInstance;

    component.showUnassignedEmployees();

    expect(component.activeTab()).toBe('employees');
    expect(component.employeeDepartmentFilter()).toBe('unassigned');
    expect(component.employeeStatusFilter()).toBe('active');
  });

  it('excludes system accounts from selectable employees', () => {
    const component = TestBed.createComponent(ProjectDepartmentsPage).componentInstance;
    component.employees.set([
      ...employees,
      {
        ...employees[0],
        id: 'system-admin',
        displayName: 'Admin',
        roles: ['Admin'],
        isSelectable: false
      }
    ]);

    expect(component.selectableEmployees().some((employee) => employee.id === 'system-admin')).toBe(false);
  });

  it('only opens permanent deletion for inactive non-admin employees', () => {
    const component = TestBed.createComponent(ProjectDepartmentsPage).componentInstance;

    component.openEmployeeDelete(employees[0]);
    expect(component.deleteCandidate()).toBeNull();

    component.openEmployeeDelete(employees[2]);
    expect(component.deleteCandidate()).toEqual({
      kind: 'employee',
      id: 'employee-3',
      name: 'Pasif Çalışan'
    });
  });

  it('requires the explicit SİL confirmation before permanent deletion', async () => {
    const component = TestBed.createComponent(ProjectDepartmentsPage).componentInstance;
    const employeeApi = TestBed.inject(EmployeeApiService);
    const deleteSpy = vi.spyOn(employeeApi, 'delete');
    component.openEmployeeDelete(employees[2]);

    component.deleteConfirmation.set('silme');
    await component.confirmPermanentDelete();
    expect(deleteSpy).not.toHaveBeenCalled();

    component.deleteConfirmation.set('SİL');
    await component.confirmPermanentDelete();
    expect(deleteSpy).toHaveBeenCalledWith('employee-3');
  });
});
