class LoginResponseDto {
  LoginResponseDto({
    required this.accessToken,
    required this.employeeId,
    required this.displayName,
    required this.roles,
  });

  final String accessToken;
  final String employeeId;
  final String displayName;
  final List<String> roles;

  factory LoginResponseDto.fromJson(Map<String, dynamic> json) {
    return LoginResponseDto(
      accessToken: json['accessToken'] as String,
      employeeId: json['employeeId'] as String,
      displayName: json['displayName'] as String,
      roles: (json['roles'] as List? ?? []).map((e) => e.toString()).toList(),
    );
  }
}
