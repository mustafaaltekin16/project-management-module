import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import {
  AssignDepartmentHeadRequest,
  CreateDepartmentRequest,
  DepartmentDetailDto,
  DepartmentDto,
  SetDepartmentStatusRequest,
  UpdateDepartmentRequest
} from './department-api.models';

@Injectable({ providedIn: 'root' })
export class DepartmentApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/departments`;

  async list(includeInactive = false): Promise<DepartmentDto[]> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<DepartmentDto[]>>(
        `${this.baseUrl}${includeInactive ? '?includeInactive=true' : ''}`
      )
    );
    return unwrap(response);
  }

  async getById(id: string): Promise<DepartmentDetailDto> {
    const response = await firstValueFrom(this.http.get<ApiResponse<DepartmentDetailDto>>(`${this.baseUrl}/${id}`));
    return unwrap(response);
  }

  async create(request: CreateDepartmentRequest): Promise<DepartmentDto> {
    const response = await firstValueFrom(this.http.post<ApiResponse<DepartmentDto>>(this.baseUrl, request));
    return unwrap(response);
  }

  async assignHead(id: string, request: AssignDepartmentHeadRequest): Promise<DepartmentDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<DepartmentDto>>(`${this.baseUrl}/${id}/head`, request)
    );
    return unwrap(response);
  }

  async update(id: string, request: UpdateDepartmentRequest): Promise<DepartmentDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<DepartmentDto>>(`${this.baseUrl}/${id}`, request)
    );
    return unwrap(response);
  }

  async setStatus(id: string, request: SetDepartmentStatusRequest): Promise<DepartmentDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<DepartmentDto>>(`${this.baseUrl}/${id}/status`, request)
    );
    return unwrap(response);
  }

  async delete(id: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.delete<ApiResponse<object>>(`${this.baseUrl}/${id}`)
    );
    unwrap(response);
  }
}
