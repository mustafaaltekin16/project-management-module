export interface EmployeeDto {
  id: string;
  displayName: string;
  email: string;
  departmentId: string | null;
  departmentName: string | null;
  title: string;
  roles: string[];
  isActive?: boolean;
  isSelectable?: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string | null;
}

export interface EmployeeListFilter {
  role?: string;
  q?: string;
  departmentId?: string;
  includeInactive?: boolean;
}

export interface AssignEmployeeDepartmentRequest {
  departmentId: string | null;
}

export interface CreateEmployeeRequest {
  displayName: string;
  email: string;
  password: string;
  departmentId: string | null;
  title: string;
  roles: string[];
}

export interface UpdateEmployeeRequest {
  displayName: string;
  email: string;
  departmentId: string | null;
  title: string;
  roles: string[];
}

export interface SetEmployeeStatusRequest {
  isActive: boolean;
}

export interface ResetEmployeePasswordRequest {
  newPassword: string;
}
