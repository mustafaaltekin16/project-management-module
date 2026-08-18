import 'package:dio/dio.dart';

import '../../../core/api/api_response.dart';
import 'dto/login_response_dto.dart';

class LoginApiService {
  LoginApiService(this._dio);

  final Dio _dio;

  Future<LoginResponseDto> login(String email, String password) async {
    final res = await _dio.post('/api/auth/login', data: {
      'email': email,
      'password': password,
    });
    return ApiResponse<LoginResponseDto>.fromJson(
      res.data as Map<String, dynamic>,
      (json) => LoginResponseDto.fromJson(json as Map<String, dynamic>),
    ).unwrap();
  }
}
