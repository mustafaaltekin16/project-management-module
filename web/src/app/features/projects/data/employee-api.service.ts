import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import {
  AssignEmployeeDepartmentRequest,
  CreateEmployeeRequest,
  EmployeeDto,
  EmployeeListFilter,
  ResetEmployeePasswordRequest,
  SetEmployeeStatusRequest,
  UpdateEmployeeRequest
} from './employee-api.models';

@Injectable({ providedIn: 'root' })
export class EmployeeApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/employees`;

  async create(request: CreateEmployeeRequest): Promise<EmployeeDto> {
    const response = await firstValueFrom(this.http.post<ApiResponse<EmployeeDto>>(this.baseUrl, request));
    return unwrap(response);
  }

  async list(filter: EmployeeListFilter = {}): Promise<EmployeeDto[]> {
    const query = new URLSearchParams();
    if (filter.role) query.set('role', filter.role);
    if (filter.q) query.set('q', filter.q);
    if (filter.departmentId) query.set('departmentId', filter.departmentId);
    if (filter.includeInactive) query.set('includeInactive', 'true');
    const qs = query.toString();

    const response = await firstValueFrom(
      this.http.get<ApiResponse<EmployeeDto[]>>(`${this.baseUrl}${qs ? '?' + qs : ''}`)
    );
    return unwrap(response);
  }

  async assignDepartment(employeeId: string, request: AssignEmployeeDepartmentRequest): Promise<EmployeeDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<EmployeeDto>>(`${this.baseUrl}/${employeeId}/department`, request)
    );
    return unwrap(response);
  }

  async update(employeeId: string, request: UpdateEmployeeRequest): Promise<EmployeeDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<EmployeeDto>>(`${this.baseUrl}/${employeeId}`, request)
    );
    return unwrap(response);
  }

  async setStatus(employeeId: string, request: SetEmployeeStatusRequest): Promise<EmployeeDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<EmployeeDto>>(`${this.baseUrl}/${employeeId}/status`, request)
    );
    return unwrap(response);
  }

  async resetPassword(employeeId: string, request: ResetEmployeePasswordRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<object>>(`${this.baseUrl}/${employeeId}/password`, request)
    );
    unwrap(response);
  }

  async delete(employeeId: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.delete<ApiResponse<object>>(`${this.baseUrl}/${employeeId}`)
    );
    unwrap(response);
  }
}
