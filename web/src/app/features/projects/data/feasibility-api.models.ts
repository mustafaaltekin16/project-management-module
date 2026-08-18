export type ApprovalDecision = 'Pending' | 'Approved' | 'Rejected';
export type FeasibilityItemStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'Rejected';

export interface ApprovalStepDto {
  id: string;
  approverName: string;
  order: number;
  decision: ApprovalDecision;
  comment: string | null;
  decidedAtUtc: string | null;
}

export interface FeasibilityItemDto {
  id: string;
  unit: string;
  description: string;
  amount: number;
  currency: string;
  status: FeasibilityItemStatus;
  steps: ApprovalStepDto[];
}

export interface FeasibilityMainGroupDto {
  id: string;
  projectId: string;
  workPackageId: string | null;
  timelineSortOrder: number;
  name: string;
  totalRequestedAmount: number;
  totalApprovedAmount: number;
  items: FeasibilityItemDto[];
}

export interface CreateMainGroupRequest {
  projectId: string;
  name: string;
  workPackageId?: string | null;
  timelineSortOrder?: number;
}

export interface ConfigureMainGroupTimelineRequest {
  workPackageId: string | null;
  timelineSortOrder: number;
}

export interface AddFeasibilityItemRequest {
  unit: string;
  description: string;
  amount: number;
  currency: string;
}

export interface SubmitForApprovalRequest {
  approverNamesInOrder: string[];
}

export interface DecideApprovalRequest {
  approverName: string;
  approve: boolean;
  comment: string | null;
}
