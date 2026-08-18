import 'package:jwt_decoder/jwt_decoder.dart';

class AuthUser {
  AuthUser({
    required this.userId,
    required this.displayName,
    required this.roles,
    required this.tokenExpiresAtUtc,
  });

  final String userId;
  final String displayName;
  final List<String> roles;
  final DateTime tokenExpiresAtUtc;

  bool hasAnyRole(List<String> allowed) => roles.any(allowed.contains);

  factory AuthUser.fromJwtClaims(Map<String, dynamic> claims, String token) {
    final rawRole = claims['role'];
    final roles = rawRole is List
        ? rawRole.map((e) => e.toString()).toList()
        : rawRole == null
            ? <String>[]
            : <String>[rawRole.toString()];
    return AuthUser(
      userId: claims['sub']?.toString() ?? '',
      displayName: claims['name']?.toString() ?? '',
      roles: roles,
      tokenExpiresAtUtc: JwtDecoder.getExpirationDate(token).toUtc(),
    );
  }
}
