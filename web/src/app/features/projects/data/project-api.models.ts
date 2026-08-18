export type BackendProjectType = 'Simple' | 'MultiUnit' | 'FeasibilityBased';
export type ProjectStatus = 'Draft' | 'Active' | 'Completed' | 'Cancelled';

export interface DepartmentAssignmentDto {
  id: string;
  departmentId?: string | null;
  title: string;
  departmentName: string;
  managerEmployeeId?: string | null;
  managerName: string;
  startDate: string | null;
  endDate: string | null;
}

export interface ProjectListItemDto {
  id: string;
  managerEmployeeId?: string | null;
  unitDepartmentId?: string | null;
  name: string;
  managerName: string;
  unit: string;
  progressPercent: number;
  deviationDays: number;
  budget: number;
  currency: string;
  type: BackendProjectType;
  status: ProjectStatus;
  startDate: string;
  endDate: string;
  updatedAtUtc: string;
  boardColumnId: string | null;
  boardPosition: number;
}

export interface ProjectNoteDto {
  id: string;
  author: string;
  text: string;
  createdAtUtc: string;
}

export interface ProjectTemplateFieldValueDto {
  templateFieldId: string;
  label: string;
  hint: string;
  contentType: string;
  listName: string | null;
  isRequired: boolean;
  options: string[];
  value: string | null;
  sortOrder: number;
}

export interface ProjectDetailDto {
  id: string;
  name: string;
  description: string;
  managerEmployeeId?: string | null;
  managerName: string;
  secondManagerEmployeeId?: string | null;
  secondManagerName: string | null;
  unitDepartmentId?: string | null;
  unit: string;
  type: BackendProjectType;
  status: ProjectStatus;
  budget: number;
  currency: string;
  progressPercent: number;
  deviationDays: number;
  startDate: string;
  endDate: string;
  templateId: string | null;
  templateName: string | null;
  enabledComponents: string[];
  templateValues: ProjectTemplateFieldValueDto[];
  departments: DepartmentAssignmentDto[];
  notes: ProjectNoteDto[];
}

export type ProjectTimelineState = 'Pending' | 'Active' | 'Completed' | 'Blocked';
export type ProjectTimelineProcessType =
  | 'Feasibility'
  | 'PriceComparison'
  | 'Approval'
  | 'Procurement';

export interface ProjectTimelineProcessDto {
  type: ProjectTimelineProcessType;
  label: string;
  ownerEmployeeId: string | null;
  ownerName: string;
  state: ProjectTimelineState;
  plannedStartDate: string | null;
  plannedEndDate: string | null;
}

export interface ProjectTimelineWorkPackageDto {
  id: string;
  title: string;
  departmentId: string | null;
  departmentName: string;
  managerEmployeeId: string | null;
  managerName: string;
  startDate: string;
  endDate: string;
  deviationDays: number;
  state: ProjectTimelineState;
  processes: ProjectTimelineProcessDto[];
}

export interface ProjectTimelineDto {
  projectId: string;
  startDate: string;
  endDate: string;
  workPackages: ProjectTimelineWorkPackageDto[];
  isPartial: boolean;
  warnings: string[];
}

export interface AddNoteRequest {
  author: string;
  text: string;
}

export interface UpdateNoteRequest {
  author: string;
  text: string;
}

export interface AddDepartmentRequest {
  departmentId: string;
  title: string;
  departmentName: string;
  managerEmployeeId: string;
  managerName: string;
  startDate: string | null;
  endDate: string | null;
}

export interface CreateProjectRequest {
  name: string;
  description: string;
  managerEmployeeId: string;
  managerName: string;
  secondManagerEmployeeId: string | null;
  secondManagerName: string | null;
  unitDepartmentId: string;
  unit: string;
  type: BackendProjectType;
  budget: number;
  currency: string;
  startDate: string;
  endDate: string;
  templateId: string | null;
  enabledComponents: string[];
  templateValues: Array<{ fieldId: string; value: string | null }>;
  departments: AddDepartmentRequest[];
}


export interface UpdateTemplateValuesRequest {
  values: Array<{ fieldId: string; value: string | null }>;
}

export interface ProjectSearchParams {
  type?: BackendProjectType;
  q?: string;
}
