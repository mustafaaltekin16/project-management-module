import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import '../domain/entities/project_document.dart';

class DocumentApiService {
  DocumentApiService(this._dio);

  final Dio _dio;

  Future<List<ProjectDocument>> list(String projectId) async {
    final res = await _dio.get('/api/projects/$projectId/documents');
    return ApiResponse<List<ProjectDocument>>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => (json as List).map((e) => ProjectDocument.fromJson(e as Map<String, dynamic>)).toList(),
    ).unwrap();
  }

  Future<void> upload(String projectId, String filePath, String fileName) async {
    final form = FormData.fromMap({'file': await MultipartFile.fromFile(filePath, filename: fileName)});
    await _dio.post('/api/projects/$projectId/documents', data: form);
  }

  Future<void> delete(String projectId, String documentId) async {
    await _dio.delete('/api/projects/$projectId/documents/$documentId');
  }
}
