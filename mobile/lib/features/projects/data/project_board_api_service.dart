import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import 'dto/board_column_dto.dart';

class ProjectBoardApiService {
  ProjectBoardApiService(this._dio);

  final Dio _dio;

  Future<List<BoardColumnDto>> getColumns() async {
    final res = await _dio.get('/api/project-board/columns');
    return ApiResponse<List<BoardColumnDto>>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => (json as List).map((e) => BoardColumnDto.fromJson(e as Map<String, dynamic>)).toList(),
    ).unwrap();
  }

  Future<void> movePlacement(String projectId, String targetColumnId, String expectedUpdatedAtUtc) async {
    await _dio.put('/api/project-board/projects/$projectId/placement', data: {
      'columnId': targetColumnId,
      'beforeProjectId': null,
      'afterProjectId': null,
      'expectedUpdatedAtUtc': expectedUpdatedAtUtc,
    });
  }
}
