import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/api/api-response';
import { unwrap } from '../../../shared/api/unwrap';
import {
  AddCommentRequest,
  ArchiveTaskResult,
  ArchivedTaskDto,
  ChangeTaskStatusRequest,
  ConfigureTaskGroupTimelineRequest,
  CopyTaskResult,
  CreateTaskGroupRequest,
  CreateTaskRequest,
  MyTaskDto,
  ProjectDocumentDto,
  ReassignTaskRequest,
  RestoreTaskResult,
  TaskGroupDto,
  UpdateTaskRequest,
  UpdateTaskGroupRequest
} from './task-api.models';

@Injectable({ providedIn: 'root' })
export class TaskApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api`;

  async listByProject(projectId: string): Promise<TaskGroupDto[]> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<TaskGroupDto[]>>(`${this.apiUrl}/projects/${projectId}/task-groups`)
    );
    return unwrap(response);
  }

  async createGroup(request: CreateTaskGroupRequest): Promise<TaskGroupDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<TaskGroupDto>>(`${this.apiUrl}/task-groups`, request)
    );
    return unwrap(response);
  }

  async renameGroup(groupId: string, request: UpdateTaskGroupRequest): Promise<TaskGroupDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<TaskGroupDto>>(`${this.apiUrl}/task-groups/${groupId}`, request)
    );
    return unwrap(response);
  }

  async configureTimeline(
    groupId: string,
    request: ConfigureTaskGroupTimelineRequest
  ): Promise<TaskGroupDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<TaskGroupDto>>(`${this.apiUrl}/task-groups/${groupId}/timeline`, request)
    );
    return unwrap(response);
  }

  async addTask(groupId: string, request: CreateTaskRequest): Promise<TaskGroupDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<TaskGroupDto>>(`${this.apiUrl}/task-groups/${groupId}/tasks`, request)
    );
    return unwrap(response);
  }

  async changeStatus(groupId: string, taskId: string, request: ChangeTaskStatusRequest): Promise<TaskGroupDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<TaskGroupDto>>(`${this.apiUrl}/task-groups/${groupId}/tasks/${taskId}/status`, request)
    );
    return unwrap(response);
  }

  async updateTask(groupId: string, taskId: string, request: UpdateTaskRequest): Promise<TaskGroupDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<TaskGroupDto>>(`${this.apiUrl}/task-groups/${groupId}/tasks/${taskId}`, request)
    );
    return unwrap(response);
  }

  async archiveTask(groupId: string, taskId: string): Promise<ArchiveTaskResult> {
    const response = await firstValueFrom(
      this.http.delete<ApiResponse<ArchiveTaskResult>>(`${this.apiUrl}/task-groups/${groupId}/tasks/${taskId}`)
    );
    return unwrap(response);
  }

  async listArchivedTasks(projectId: string): Promise<ArchivedTaskDto[]> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<ArchivedTaskDto[]>>(`${this.apiUrl}/projects/${projectId}/archived-tasks`)
    );
    return unwrap(response);
  }

  async restoreTask(groupId: string, taskId: string): Promise<RestoreTaskResult> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<RestoreTaskResult>>(`${this.apiUrl}/task-groups/${groupId}/tasks/${taskId}/restore`, {})
    );
    return unwrap(response);
  }

  async copyTask(groupId: string, taskId: string): Promise<CopyTaskResult> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<CopyTaskResult>>(`${this.apiUrl}/task-groups/${groupId}/tasks/${taskId}/copy`, {})
    );
    return unwrap(response);
  }

  async reassignTask(groupId: string, taskId: string, request: ReassignTaskRequest): Promise<TaskGroupDto> {
    const response = await firstValueFrom(
      this.http.put<ApiResponse<TaskGroupDto>>(`${this.apiUrl}/task-groups/${groupId}/tasks/${taskId}/assignee`, request)
    );
    return unwrap(response);
  }

  async addComment(groupId: string, taskId: string, request: AddCommentRequest): Promise<TaskGroupDto> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<TaskGroupDto>>(`${this.apiUrl}/task-groups/${groupId}/tasks/${taskId}/comments`, request)
    );
    return unwrap(response);
  }

  async listMine(): Promise<MyTaskDto[]> {
    const response = await firstValueFrom(this.http.get<ApiResponse<MyTaskDto[]>>(`${this.apiUrl}/tasks/mine`));
    return unwrap(response);
  }

  async listDocuments(projectId: string): Promise<ProjectDocumentDto[]> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<ProjectDocumentDto[]>>(`${this.apiUrl}/projects/${projectId}/documents`)
    );
    return unwrap(response);
  }

  async uploadDocument(
    projectId: string,
    file: File,
    options?: { noteId?: string | null; uploadedBy?: string | null }
  ): Promise<ProjectDocumentDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    if (options?.noteId) {
      formData.append('noteId', options.noteId);
    }
    if (options?.uploadedBy) {
      formData.append('uploadedBy', options.uploadedBy);
    }
    const response = await firstValueFrom(
      this.http.post<ApiResponse<ProjectDocumentDto>>(`${this.apiUrl}/projects/${projectId}/documents`, formData)
    );
    return unwrap(response);
  }

  async downloadDocument(projectId: string, documentId: string, fileName: string): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`${this.apiUrl}/projects/${projectId}/documents/${documentId}/content`, { responseType: 'blob' })
    );
    const url = window.URL.createObjectURL(blob);
    const link = window.document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  async deleteDocument(projectId: string, documentId: string): Promise<void> {
    await firstValueFrom(
      this.http.delete<void>(`${this.apiUrl}/projects/${projectId}/documents/${documentId}`)
    );
  }
}
