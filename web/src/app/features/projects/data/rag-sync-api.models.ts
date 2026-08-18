export interface RagSyncResultDto {
  confirmedIndexedFileNames: string[];
  attemptedFileNames: string[];
  fullySynced: boolean;
}
