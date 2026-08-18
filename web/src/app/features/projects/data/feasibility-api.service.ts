import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import {
  AddFeasibilityItemRequest,
  ConfigureMainGroupTimelineRequest,
  CreateMainGroupRequest,
  DecideApprovalRequest,
  FeasibilityMainGroupDto,
  SubmitForApprovalRequest
} from './feasibility-api.models';

@Injectable({ providedIn: 'root' })
export class FeasibilityApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api`;

  async listByProject(projectId: string): Promise<FeasibilityMainGroupDto[]> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<FeasibilityMainGroupDto[]>>(`${this.apiUrl}/projects/${projectId}/feasibility-groups`)
    );
    return unwrap(response);
  }

  async createMainGroup(request: CreateMainGroupRequest): Promise<FeasibilityMainGroupDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<FeasibilityMainGroupDto>>(`${this.apiUrl}/feasibility-groups`, request)
    );
    return unwrap(response);
  }

  async configureTimeline(
    mainGroupId: string,
    request: ConfigureMainGroupTimelineRequest
  ): Promise<FeasibilityMainGroupDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<FeasibilityMainGroupDto>>(
        `${this.apiUrl}/feasibility-groups/${mainGroupId}/timeline`,
        request
      )
    );
    return unwrap(response);
  }

  async addItem(mainGroupId: string, request: AddFeasibilityItemRequest): Promise<FeasibilityMainGroupDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<FeasibilityMainGroupDto>>(`${this.apiUrl}/feasibility-groups/${mainGroupId}/items`, request)
    );
    return unwrap(response);
  }

  async submitForApproval(mainGroupId: string, itemId: string, request: SubmitForApprovalRequest): Promise<FeasibilityMainGroupDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<FeasibilityMainGroupDto>>(
        `${this.apiUrl}/feasibility-groups/${mainGroupId}/items/${itemId}/submit`,
        request
      )
    );
    return unwrap(response);
  }

  async decide(mainGroupId: string, itemId: string, request: DecideApprovalRequest): Promise<FeasibilityMainGroupDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<FeasibilityMainGroupDto>>(
        `${this.apiUrl}/feasibility-groups/${mainGroupId}/items/${itemId}/decide`,
        request
      )
    );
    return unwrap(response);
  }
}
