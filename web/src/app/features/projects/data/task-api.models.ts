export type KanbanStatus = 'Todo' | 'InProgress' | 'Done';
export type TaskProcessType = 'Feasibility' | 'PriceComparison' | 'Approval' | 'Procurement';
export type DocumentKind = 'Word' | 'PowerPoint' | 'Excel' | 'Pdf' | 'File' | 'Image' | 'Video';

export interface TaskCommentDto {
  id: string;
  author: string;
  text: string;
  createdAtUtc: string;
}

export interface TaskItemDto {
  id: string;
  title: string;
  assigneeName: string;
  assigneeEmployeeId: string | null;
  department: string | null;
  effortHours: number | null;
  depth: number;
  isMainTask: boolean;
  dependsOnTaskId: string | null;
  status: KanbanStatus;
  isAiGenerated: boolean;
  comments: TaskCommentDto[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
  startDateUtc: string | null;
  dueDateUtc: string | null;
  category: string | null;
  description: string | null;
  completedAtUtc: string | null;
  completedBy: string | null;
}

export interface TaskGroupDto {
  id: string;
  projectId: string;
  workPackageId: string | null;
  processType: TaskProcessType | null;
  timelineSortOrder: number;
  title: string;
  subtitle: string;
  tasks: TaskItemDto[];
  createdAtUtc: string;
}

export interface CreateTaskGroupRequest {
  projectId: string;
  title: string;
  subtitle: string;
  workPackageId?: string | null;
  processType?: TaskProcessType | null;
  timelineSortOrder?: number;
}

export interface ConfigureTaskGroupTimelineRequest {
  workPackageId: string | null;
  processType: TaskProcessType | null;
  timelineSortOrder: number;
}

export interface UpdateTaskGroupRequest {
  title: string;
  subtitle: string;
}

export interface CreateTaskRequest {
  title: string;
  assigneeName: string;
  department: string | null;
  effortHours: number | null;
  isMainTask: boolean;
  dependsOnTaskId: string | null;
  assigneeEmployeeId: string | null;
  startDateUtc: string | null;
  dueDateUtc: string | null;
  category: string | null;
  description: string | null;
}

export interface ChangeTaskStatusRequest {
  status: KanbanStatus;
}

export interface UpdateTaskRequest {
  title: string;
  assigneeName: string;
  assigneeEmployeeId: string | null;
  department: string | null;
  effortHours: number | null;
  startDateUtc: string | null;
  dueDateUtc: string | null;
  category: string | null;
  description: string | null;
}

export interface ArchiveTaskResult {
  group: TaskGroupDto;
  archivedTaskCount: number;
}

export interface ArchivedTaskDto {
  taskId: string;
  groupId: string;
  title: string;
  isMainTask: boolean;
  isAiGenerated: boolean;
  assigneeName: string;
  archivedSubtaskCount: number;
  archivedAtUtc: string;
}

export interface RestoreTaskResult {
  group: TaskGroupDto;
  restoredTaskCount: number;
}

export interface CopyTaskResult {
  group: TaskGroupDto;
  copiedTaskCount: number;
}

export interface ReassignTaskRequest {
  assigneeEmployeeId: string;
  assigneeName: string;
  department: string | null;
  changedByName: string;
}

export interface AddCommentRequest {
  author: string;
  text: string;
}

export interface MyTaskDto {
  taskId: string;
  projectId: string;
  groupId: string;
  title: string;
  status: KanbanStatus;
  isAiGenerated: boolean;
}

export interface ProjectDocumentDto {
  id: string;
  projectId: string;
  noteId: string | null;
  uploadedBy: string | null;
  name: string;
  kind: DocumentKind;
  sizeBytes: number;
  contentType: string;
  createdAtUtc: string;
}
