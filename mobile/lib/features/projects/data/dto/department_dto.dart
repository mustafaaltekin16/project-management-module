import '../../domain/entities/department.dart';

class DepartmentDto {
  DepartmentDto({required this.department});

  final Department department;

  factory DepartmentDto.fromJson(Map<String, dynamic> json) {
    return DepartmentDto(
      department: Department(
        id: json['id'].toString(),
        name: json['name'] as String? ?? '',
        headEmployeeId: json['headEmployeeId']?.toString(),
        isActive: json['isActive'] as bool? ?? true,
      ),
    );
  }
}
