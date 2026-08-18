import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import {
  MoveProjectBoardCardRequest,
  ProjectBoardColumnDto,
  SaveProjectBoardColumnRequest
} from './project-board-api.models';

@Injectable({ providedIn: 'root' })
export class ProjectBoardApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/project-board`;

  async listColumns(): Promise<ProjectBoardColumnDto[]> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<ProjectBoardColumnDto[]>>(`${this.baseUrl}/columns`)
    );
    return unwrap(response);
  }

  async createColumn(request: SaveProjectBoardColumnRequest): Promise<ProjectBoardColumnDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<ProjectBoardColumnDto>>(`${this.baseUrl}/columns`, request)
    );
    return unwrap(response);
  }

  async updateColumn(id: string, request: SaveProjectBoardColumnRequest): Promise<ProjectBoardColumnDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<ProjectBoardColumnDto>>(`${this.baseUrl}/columns/${id}`, request)
    );
    return unwrap(response);
  }

  async reorderColumns(columnIds: string[]): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<object>>(`${this.baseUrl}/columns/reorder`, { columnIds })
    );
    unwrap(response);
  }

  async archiveColumn(id: string, targetColumnId: string | null): Promise<void> {
    const query = targetColumnId ? `?targetColumnId=${encodeURIComponent(targetColumnId)}` : '';
    const response = await firstValueFrom(
      this.http.delete<ApiResponse<object>>(`${this.baseUrl}/columns/${id}${query}`)
    );
    unwrap(response);
  }

  async moveCard(projectId: string, request: MoveProjectBoardCardRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<object>>(`${this.baseUrl}/projects/${projectId}/placement`, request)
    );
    unwrap(response);
  }
}
