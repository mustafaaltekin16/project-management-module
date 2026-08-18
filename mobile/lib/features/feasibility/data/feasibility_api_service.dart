import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import '../domain/entities/feasibility_main_group.dart';

class FeasibilityApiService {
  FeasibilityApiService(this._dio);

  final Dio _dio;

  Future<List<FeasibilityMainGroup>> getGroups(String projectId) async {
    final res = await _dio.get('/api/projects/$projectId/feasibility-groups');
    return ApiResponse<List<FeasibilityMainGroup>>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => (json as List).map((e) => FeasibilityMainGroup.fromJson(e as Map<String, dynamic>)).toList(),
    ).unwrap();
  }

  Future<FeasibilityMainGroup> createGroup(String projectId, String name) async {
    final res = await _dio.post('/api/feasibility-groups', data: {
      'projectId': projectId,
      'name': name,
    });
    return ApiResponse<FeasibilityMainGroup>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => FeasibilityMainGroup.fromJson(json as Map<String, dynamic>),
    ).unwrap();
  }

  Future<FeasibilityMainGroup> addItem(
    String mainGroupId, {
    required String unit,
    required String description,
    required double amount,
    required String currency,
  }) async {
    final res = await _dio.post('/api/feasibility-groups/$mainGroupId/items', data: {
      'unit': unit,
      'description': description,
      'amount': amount,
      'currency': currency,
    });
    return ApiResponse<FeasibilityMainGroup>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => FeasibilityMainGroup.fromJson(json as Map<String, dynamic>),
    ).unwrap();
  }

  Future<FeasibilityMainGroup> submitForApproval(
    String mainGroupId,
    String itemId,
    List<String> approverNamesInOrder,
  ) async {
    final res = await _dio.post('/api/feasibility-groups/$mainGroupId/items/$itemId/submit', data: {
      'approverNamesInOrder': approverNamesInOrder,
    });
    return ApiResponse<FeasibilityMainGroup>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => FeasibilityMainGroup.fromJson(json as Map<String, dynamic>),
    ).unwrap();
  }

  Future<FeasibilityMainGroup> decide(
    String mainGroupId,
    String itemId, {
    required String approverName,
    required bool approve,
    String? comment,
  }) async {
    final res = await _dio.post('/api/feasibility-groups/$mainGroupId/items/$itemId/decide', data: {
      'approverName': approverName,
      'approve': approve,
      if (comment != null) 'comment': comment,
    });
    return ApiResponse<FeasibilityMainGroup>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => FeasibilityMainGroup.fromJson(json as Map<String, dynamic>),
    ).unwrap();
  }
}
