export interface ProjectBoardColumnDto {
  id: string;
  name: string;
  color: string;
  sortOrder: number;
  updatedAtUtc: string;
  isProtected: boolean;
}

export interface SaveProjectBoardColumnRequest {
  name: string;
  color: string;
}

export interface MoveProjectBoardCardRequest {
  columnId: string | null;
  beforeProjectId: string | null;
  afterProjectId: string | null;
  expectedUpdatedAtUtc: string;
}
