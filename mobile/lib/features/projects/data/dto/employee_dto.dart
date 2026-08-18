import '../../domain/entities/employee.dart';

class EmployeeDto {
  EmployeeDto({required this.employee});

  final Employee employee;

  factory EmployeeDto.fromJson(Map<String, dynamic> json) {
    return EmployeeDto(
      employee: Employee(
        id: json['id'].toString(),
        fullName: json['fullName'] as String? ?? json['name'] as String? ?? '',
        roles: (json['roles'] as List? ?? []).map((e) => e.toString()).toList(),
        isActive: json['isActive'] as bool? ?? true,
      ),
    );
  }
}
