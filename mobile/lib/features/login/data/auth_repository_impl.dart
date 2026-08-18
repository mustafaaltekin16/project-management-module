import 'package:jwt_decoder/jwt_decoder.dart';

import '../../../core/storage/secure_storage_service.dart';
import '../domain/auth_repository.dart';
import '../domain/entities/auth_user.dart';
import 'login_api_service.dart';

class AuthRepositoryImpl implements AuthRepository {
  AuthRepositoryImpl(this._api, this._storage);

  final LoginApiService _api;
  final SecureStorageService _storage;

  @override
  Future<AuthUser> login(String email, String password) async {
    final dto = await _api.login(email, password);
    await _storage.saveToken(dto.accessToken);
    return AuthUser(
      userId: dto.employeeId,
      displayName: dto.displayName,
      roles: dto.roles,
      tokenExpiresAtUtc: JwtDecoder.getExpirationDate(dto.accessToken).toUtc(),
    );
  }

  @override
  Future<AuthUser?> restoreSession() async {
    final token = await _storage.readToken();
    if (token == null || JwtDecoder.isExpired(token)) {
      await _storage.deleteToken();
      return null;
    }
    final claims = JwtDecoder.decode(token);
    return AuthUser.fromJwtClaims(claims, token);
  }

  @override
  Future<void> logout() => _storage.deleteToken();
}
