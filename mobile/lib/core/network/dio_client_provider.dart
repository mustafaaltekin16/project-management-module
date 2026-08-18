import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/login/presentation/controllers/auth_controller.dart';
import '../config/app_config.dart';
import '../storage/secure_storage_service.dart';
import 'auth_interceptor.dart';

final dioProvider = Provider<Dio>((ref) {
  final dio = Dio(BaseOptions(baseUrl: AppConfig.apiBaseUrl));
  dio.interceptors.add(AuthInterceptor(
    storage: ref.read(secureStorageServiceProvider),
    onUnauthorized: () => ref.read(authControllerProvider.notifier).logout(),
  ));
  return dio;
});
