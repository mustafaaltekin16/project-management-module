export type SuggestionItemDecision = 'Pending' | 'Approved' | 'Rejected';

export interface AiSuggestionActivityDto {
  id: string;
  title: string;
  effortHours: number | null;
}

export interface WorkPackageSuggestionItemDto {
  id: string;
  title: string;
  department: string;
  effortHours: number;
  sourceDocument: string | null;
  decision: SuggestionItemDecision;
  description: string | null;
  sequenceNote: string | null;
  insertAfterTaskTitle: string | null;
  sequenceRank: number | null;
  isAtProjectStart: boolean;
  activities: AiSuggestionActivityDto[];
}

export interface AiSuggestionRequestDto {
  id: string;
  projectId: string;
  projectType: string;
  extraInstructions: string | null;
  providerUsed: string;
  createdAtUtc: string;
  selectedDocumentNames: string[];
  items: WorkPackageSuggestionItemDto[];
  usedRealDocumentContext: boolean;
  // Sadece bu üretim çağrısının ANLIK sonucunu yansıtır — modelin yanıtındaki önerilerin yarısından azı
  // ayrıştırılabildiğinde true olur (bkz. backend AiSuggestionAppService.GenerateAndParseSuggestionsAsync).
  // Geçmiş isteklerin listelenmesinde her zaman false'tur.
  possiblyIncomplete: boolean;
}

export interface GenerateSuggestionsRequest {
  projectId: string;
  extraInstructions: string | null;
  selectedDocumentIds: string[];
}
