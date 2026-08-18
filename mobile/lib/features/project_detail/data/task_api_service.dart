import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import '../domain/entities/task_item.dart';

class TaskApiService {
  TaskApiService(this._dio);

  final Dio _dio;

  Future<List<TaskGroup>> getTaskGroups(String projectId) async {
    final res = await _dio.get('/api/projects/$projectId/task-groups');
    return ApiResponse<List<TaskGroup>>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => (json as List).map((e) => TaskGroup.fromJson(e as Map<String, dynamic>)).toList(),
    ).unwrap();
  }

  Future<void> updateStatus(String taskGroupId, String taskId, String status) async {
    await _dio.put('/api/task-groups/$taskGroupId/tasks/$taskId/status', data: {'status': status});
  }
}
