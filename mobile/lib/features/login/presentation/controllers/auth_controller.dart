import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/dio_client_provider.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../data/auth_repository_impl.dart';
import '../../data/login_api_service.dart';
import '../../domain/auth_repository.dart';
import '../../domain/entities/auth_user.dart';

final loginApiServiceProvider = Provider<LoginApiService>((ref) {
  return LoginApiService(ref.watch(dioProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepositoryImpl(
    ref.watch(loginApiServiceProvider),
    ref.watch(secureStorageServiceProvider),
  );
});

class AuthController extends AsyncNotifier<AuthUser?> {
  @override
  Future<AuthUser?> build() {
    return ref.read(authRepositoryProvider).restoreSession();
  }

  Future<void> login(String email, String password) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => ref.read(authRepositoryProvider).login(email, password),
    );
  }

  Future<void> logout() async {
    await ref.read(authRepositoryProvider).logout();
    state = const AsyncData(null);
  }
}

final authControllerProvider = AsyncNotifierProvider<AuthController, AuthUser?>(
  AuthController.new,
);
