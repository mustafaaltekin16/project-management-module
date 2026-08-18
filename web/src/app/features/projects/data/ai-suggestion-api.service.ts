import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import { AiSuggestionRequestDto, GenerateSuggestionsRequest } from './ai-suggestion-api.models';

@Injectable({ providedIn: 'root' })
export class AiSuggestionApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/ai-suggestions`;

  async generate(request: GenerateSuggestionsRequest): Promise<AiSuggestionRequestDto> {
    const response = await firstValueFrom(this.http.post<ApiResponse<AiSuggestionRequestDto>>(this.baseUrl, request));
    return unwrap(response);
  }

  async listByProject(projectId: string): Promise<AiSuggestionRequestDto[]> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<AiSuggestionRequestDto[]>>(`${this.baseUrl}/projects/${projectId}`)
    );
    return unwrap(response);
  }

  async approveItem(requestId: string, itemId: string): Promise<AiSuggestionRequestDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<AiSuggestionRequestDto>>(`${this.baseUrl}/${requestId}/items/${itemId}/approve`, {})
    );
    return unwrap(response);
  }

  async rejectItem(requestId: string, itemId: string): Promise<AiSuggestionRequestDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<AiSuggestionRequestDto>>(`${this.baseUrl}/${requestId}/items/${itemId}/reject`, {})
    );
    return unwrap(response);
  }
}
