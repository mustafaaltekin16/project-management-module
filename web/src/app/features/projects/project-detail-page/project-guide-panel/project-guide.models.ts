export type ProjectGuideTaskStatus = 'Todo' | 'InProgress' | 'Done';

export interface ProjectGuideTask {
  title: string;
  description: string | null;
  status: ProjectGuideTaskStatus;
  assignee: string;
  department: string | null;
  effortHours: number | null;
  dueDateUtc: string | null;
  groupTitle: string;
  isMainTask: boolean;
}

export interface ProjectGuideNote {
  author: string;
  text: string;
  date: string;
}

export interface ProjectGuideDocument {
  name: string;
  kind: string;
  size: string;
  uploadedBy: string | null;
}

export interface ProjectGuideContext {
  projectId: string;
  projectName: string;
  description: string;
  notes: ProjectGuideNote[];
  documents: ProjectGuideDocument[];
  tasks: ProjectGuideTask[];
}

export interface ProjectGuideReply {
  text: string;
  sources: string[];
  usedRealDocumentContext: boolean;
}

