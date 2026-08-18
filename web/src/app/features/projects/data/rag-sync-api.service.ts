import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import { RagSyncResultDto } from './rag-sync-api.models';

@Injectable({ providedIn: 'root' })
export class RagSyncApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/rag-sync`;

  async syncDocument(projectId: string, documentId: string): Promise<RagSyncResultDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<RagSyncResultDto>>(`${this.baseUrl}/projects/${projectId}/documents/${documentId}`, {})
    );
    return unwrap(response);
  }
}
