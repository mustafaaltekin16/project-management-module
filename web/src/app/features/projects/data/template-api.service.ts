import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import { CreateTemplateRequest, TemplateDto, UpdateTemplateRequest } from './template-api.models';

@Injectable({ providedIn: 'root' })
export class TemplateApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/project-templates`;

  async list(): Promise<TemplateDto[]> {
    const response = await firstValueFrom(this.http.get<ApiResponse<TemplateDto[]>>(this.baseUrl));
    return unwrap(response);
  }

  async create(request: CreateTemplateRequest): Promise<TemplateDto> {
    const response = await firstValueFrom(this.http.post<ApiResponse<TemplateDto>>(this.baseUrl, request));
    return unwrap(response);
  }

  async getById(id: string): Promise<TemplateDto> {
    const response = await firstValueFrom(this.http.get<ApiResponse<TemplateDto>>(`${this.baseUrl}/${id}`));
    return unwrap(response);
  }

  async update(id: string, request: UpdateTemplateRequest): Promise<TemplateDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<TemplateDto>>(`${this.baseUrl}/${id}`, request)
    );
    return unwrap(response);
  }

  async remove(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiResponse<object>>(`${this.baseUrl}/${id}`));
    unwrap(response);
  }

  async removeField(templateId: string, fieldId: string): Promise<TemplateDto> {
    const response = await firstValueFrom(
      this.http.delete<ApiResponse<TemplateDto>>(`${this.baseUrl}/${templateId}/fields/${fieldId}`)
    );
    return unwrap(response);
  }
}
