import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import {
  AddDepartmentRequest,
  AddNoteRequest,
  CreateProjectRequest,
  ProjectDetailDto,
  ProjectListItemDto,
  ProjectSearchParams,
  ProjectTimelineDto,
  UpdateNoteRequest,
  UpdateTemplateValuesRequest
} from './project-api.models';

@Injectable({ providedIn: 'root' })
export class ProjectApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/projects`;

  async search(params: ProjectSearchParams = {}): Promise<ProjectListItemDto[]> {
    const query = new URLSearchParams();
    if (params.type) query.set('type', params.type);
    if (params.q) query.set('q', params.q);
    const qs = query.toString();

    const response = await firstValueFrom(
      this.http.get<ApiResponse<ProjectListItemDto[]>>(`${this.baseUrl}${qs ? '?' + qs : ''}`)
    );
    return unwrap(response);
  }

  async getById(id: string): Promise<ProjectDetailDto> {
    const response = await firstValueFrom(this.http.get<ApiResponse<ProjectDetailDto>>(`${this.baseUrl}/${id}`));
    return unwrap(response);
  }

  async getTimeline(id: string): Promise<ProjectTimelineDto> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<ProjectTimelineDto>>(`${this.baseUrl}/${id}/timeline`)
    );
    return unwrap(response);
  }

  async create(request: CreateProjectRequest): Promise<ProjectDetailDto> {
    const response = await firstValueFrom(this.http.post<ApiResponse<ProjectDetailDto>>(this.baseUrl, request));
    return unwrap(response);
  }

  async addDepartment(projectId: string, request: AddDepartmentRequest): Promise<ProjectDetailDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<ProjectDetailDto>>(`${this.baseUrl}/${projectId}/departments`, request)
    );
    return unwrap(response);
  }

  // Backend artık ilerleme/sapmayı TaskService/FeasibilityService'teki güncel veriden kendisi hesaplıyor
  // (bkz. ProjectProgressAppService) — bu yüzden istekte bir gövde yok, sadece "şimdi yeniden hesapla"
  // tetikleyicisi. Asıl güncelleme olay tabanlı (bkz. ProjectProgressInputsChangedEvent) arka planda
  // zaten oluyor; bu çağrı sadece kullanıcının az önceki işleminden hemen sonra ekranda anlık doğru
  // sayıyı görebilmesi için (async event round-trip'ini beklemeden).
  async recomputeProgress(projectId: string): Promise<ProjectDetailDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<ProjectDetailDto>>(`${this.baseUrl}/${projectId}/progress`, {})
    );
    return unwrap(response);
  }

  async updateTemplateValues(projectId: string, request: UpdateTemplateValuesRequest): Promise<ProjectDetailDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<ProjectDetailDto>>(`${this.baseUrl}/${projectId}/template-values`, request)
    );
    return unwrap(response);
  }

  async addNote(projectId: string, request: AddNoteRequest): Promise<ProjectDetailDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<ProjectDetailDto>>(`${this.baseUrl}/${projectId}/notes`, request)
    );
    return unwrap(response);
  }

  async updateNote(projectId: string, noteId: string, request: UpdateNoteRequest): Promise<ProjectDetailDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<ProjectDetailDto>>(`${this.baseUrl}/${projectId}/notes/${noteId}`, request)
    );
    return unwrap(response);
  }

  async cancel(projectId: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<object>>(`${this.baseUrl}/${projectId}/cancel`, {})
    );
    unwrap(response);
  }

  async delete(projectId: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.delete<ApiResponse<object>>(`${this.baseUrl}/${projectId}`)
    );
    unwrap(response);
  }
}
