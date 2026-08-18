import 'package:dio/dio.dart';

import '../storage/secure_storage_service.dart';

class AuthInterceptor extends Interceptor {
  AuthInterceptor({required SecureStorageService storage, required Future<void> Function() onUnauthorized})
      : _storage = storage,
        _onUnauthorized = onUnauthorized;

  final SecureStorageService _storage;
  final Future<void> Function() _onUnauthorized;

  static const _loginPath = '/api/auth/login';

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    if (!options.path.contains(_loginPath)) {
      final token = await _storage.readToken();
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    final isLoginRequest = err.requestOptions.path.contains(_loginPath);
    if (!isLoginRequest && err.response?.statusCode == 401) {
      await _onUnauthorized();
    }
    handler.next(err);
  }
}
