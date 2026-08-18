import { ApiResponse } from './api-response';

export function unwrap<T>(response: ApiResponse<T>): T {
  if (!response.success || response.data === null) {
    throw new Error(response.error ?? 'Bilinmeyen bir API hatası oluştu.');
  }
  return response.data;
}
