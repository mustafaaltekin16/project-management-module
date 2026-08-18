import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import '../domain/entities/project_note.dart';

class NoteApiService {
  NoteApiService(this._dio);

  final Dio _dio;

  Future<List<ProjectNote>> list(String projectId) async {
    final res = await _dio.get('/api/projects/$projectId/notes');
    return ApiResponse<List<ProjectNote>>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => (json as List).map((e) => ProjectNote.fromJson(e as Map<String, dynamic>)).toList(),
    ).unwrap();
  }

  Future<void> add(String projectId, String content) async {
    await _dio.post('/api/projects/$projectId/notes', data: {'content': content});
  }
}
