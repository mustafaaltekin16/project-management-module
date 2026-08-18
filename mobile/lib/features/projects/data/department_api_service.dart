import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import 'dto/department_dto.dart';

class DepartmentApiService {
  DepartmentApiService(this._dio);

  final Dio _dio;

  Future<List<DepartmentDto>> list() async {
    final res = await _dio.get('/api/departments');
    return ApiResponse<List<DepartmentDto>>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => (json as List).map((e) => DepartmentDto.fromJson(e as Map<String, dynamic>)).toList(),
    ).unwrap();
  }
}
