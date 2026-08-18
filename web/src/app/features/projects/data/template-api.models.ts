import { BackendProjectType } from './project-api.models';

export type TemplateFieldKind = 'System' | 'Section' | 'Custom';

export interface TemplateFieldDto {
  id: string;
  label: string;
  hint: string;
  contentType: string;
  listName: string | null;
  isRequired: boolean;
  isActive: boolean;
  sortOrder: number;
  kind: TemplateFieldKind;
  systemKey: string | null;
  options: string[];
}

export interface TemplateDto {
  id: string;
  name: string;
  applicableProjectType: BackendProjectType;
  fields: TemplateFieldDto[];
}

export interface CreateTemplateFieldRequest {
  label: string;
  hint: string;
  contentType: string;
  listName: string | null;
  isRequired: boolean;
  isActive: boolean;
  kind: TemplateFieldKind;
  systemKey: string | null;
  options: string[];
}

export interface CreateTemplateRequest {
  name: string;
  applicableProjectType: BackendProjectType;
  fields: CreateTemplateFieldRequest[];
}

export type UpdateTemplateRequest = CreateTemplateRequest;
