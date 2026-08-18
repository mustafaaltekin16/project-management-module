import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import 'dto/employee_dto.dart';

class EmployeeApiService {
  EmployeeApiService(this._dio);

  final Dio _dio;

  Future<List<EmployeeDto>> list() async {
    final res = await _dio.get('/api/employees');
    return ApiResponse<List<EmployeeDto>>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => (json as List).map((e) => EmployeeDto.fromJson(e as Map<String, dynamic>)).toList(),
    ).unwrap();
  }
}
