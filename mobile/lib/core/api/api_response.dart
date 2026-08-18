class ApiException implements Exception {
  ApiException(this.message, {this.validationErrors});

  final String message;
  final List<String>? validationErrors;

  @override
  String toString() => message;
}

class ApiResponse<T> {
  ApiResponse({required this.success, this.data, this.error, this.validationErrors});

  final bool success;
  final T? data;
  final String? error;
  final List<String>? validationErrors;

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) {
    return ApiResponse<T>(
      success: json['success'] as bool? ?? false,
      data: json['data'] == null ? null : fromJsonT(json['data']),
      error: json['error'] as String?,
      validationErrors: (json['validationErrors'] as List?)?.map((e) => e.toString()).toList(),
    );
  }

  T unwrap() {
    if (!success || data == null) {
      throw ApiException(error ?? 'Bilinmeyen bir API hatası oluştu.', validationErrors: validationErrors);
    }
    return data as T;
  }
}
