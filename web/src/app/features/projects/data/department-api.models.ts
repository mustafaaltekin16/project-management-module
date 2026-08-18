import { EmployeeDto } from './employee-api.models';

export interface DepartmentDto {
  id: string;
  name: string;
  headEmployeeId: string | null;
  headDisplayName: string | null;
  memberCount: number;
  isActive?: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string | null;
}

export interface DepartmentDetailDto {
  id: string;
  name: string;
  headEmployeeId: string | null;
  headDisplayName: string | null;
  isActive?: boolean;
  members: EmployeeDto[];
}

export interface CreateDepartmentRequest {
  name: string;
  headEmployeeId: string | null;
}

export interface AssignDepartmentHeadRequest {
  headEmployeeId: string | null;
}

export interface UpdateDepartmentRequest {
  name: string;
  headEmployeeId?: string | null;
  updateHead?: boolean;
}

export interface SetDepartmentStatusRequest {
  isActive: boolean;
}
