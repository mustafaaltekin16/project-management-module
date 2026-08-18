import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import { ProjectGuideReply } from '../project-detail-page/project-guide-panel/project-guide.models';
import { AskProjectGuideResponseDto } from './project-chat-api.models';

@Injectable({ providedIn: 'root' })
export class ProjectChatApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/ai-chat`;

  async ask(projectId: string, question: string): Promise<ProjectGuideReply> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<AskProjectGuideResponseDto>>(`${this.baseUrl}/ask`, { projectId, question })
    );
    const dto = unwrap(response);
    return { text: dto.answer, sources: dto.sources, usedRealDocumentContext: dto.usedRealDocumentContext };
  }
}
