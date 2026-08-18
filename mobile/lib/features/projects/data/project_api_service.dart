import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import '../domain/entities/gantt_task_summary.dart';
import 'dto/create_project_request_dto.dart';
import 'dto/project_detail_dto.dart';
import 'dto/project_list_item_dto.dart';
import 'dto/project_timeline_dto.dart';

class ProjectApiService {
  ProjectApiService(this._dio);

  final Dio _dio;

  Future<List<ProjectListItemDto>> search({String? type, String? q}) async {
    final res = await _dio.get('/api/projects', queryParameters: {
      if (type != null) 'type': type,
      if (q != null && q.isNotEmpty) 'q': q,
    });
    return ApiResponse<List<ProjectListItemDto>>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => (json as List).map((e) => ProjectListItemDto.fromJson(e as Map<String, dynamic>)).toList(),
    ).unwrap();
  }

  Future<ProjectDetailDto> getById(String id) async {
    final res = await _dio.get('/api/projects/$id');
    return ApiResponse<ProjectDetailDto>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => ProjectDetailDto.fromJson(json as Map<String, dynamic>),
    ).unwrap();
  }

  Future<ProjectTimelineDto> getTimeline(String id) async {
    final res = await _dio.get('/api/projects/$id/timeline');
    return ApiResponse<ProjectTimelineDto>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => ProjectTimelineDto.fromJson(json as Map<String, dynamic>),
    ).unwrap();
  }

  Future<void> delete(String id) async {
    await _dio.delete('/api/projects/$id');
  }

  Future<String> create(CreateProjectRequestDto request) async {
    final res = await _dio.post('/api/projects', data: request.toJson());
    final body = res.data as Map<String, dynamic>;
    final data = body['data'] as Map<String, dynamic>?;
    return (data?['id'] ?? body['id']).toString();
  }

  Future<void> uploadDocument(String projectId, String filePath, String fileName) async {
    final form = FormData.fromMap({
      'file': await MultipartFile.fromFile(filePath, filename: fileName),
    });
    await _dio.post('/api/projects/$projectId/documents', data: form);
  }

  Future<List<GanttTaskSummary>> getTaskGroupsForGantt(String projectId) async {
    final res = await _dio.get('/api/projects/$projectId/task-groups');
    final body = res.data as Map<String, dynamic>;
    final data = (body['data'] as List? ?? []);
    final result = <GanttTaskSummary>[];

    void collect(List<dynamic> tasks, int depth) {
      for (final t in tasks) {
        final map = t as Map<String, dynamic>;
        result.add(GanttTaskSummary(
          id: map['id'].toString(),
          title: map['title'] as String? ?? '',
          startDate: map['startDateUtc'] == null ? null : DateTime.tryParse(map['startDateUtc'] as String),
          dueDate: map['dueDateUtc'] == null ? null : DateTime.tryParse(map['dueDateUtc'] as String),
          status: map['status'] as String? ?? 'Todo',
          depth: depth,
        ));
        final children = map['subTasks'] as List? ?? map['children'] as List? ?? [];
        if (children.isNotEmpty && depth < 3) {
          collect(children, depth + 1);
        }
      }
    }

    for (final group in data) {
      final groupMap = group as Map<String, dynamic>;
      final tasks = groupMap['tasks'] as List? ?? [];
      collect(tasks, 0);
    }
    return result;
  }
}
