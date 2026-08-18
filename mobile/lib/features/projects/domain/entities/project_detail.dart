import 'project.dart';

class ProjectDepartment {
  ProjectDepartment({
    required this.departmentId,
    required this.title,
    required this.departmentName,
    required this.managerName,
    this.startDate,
    this.endDate,
  });

  final String departmentId;
  final String title;
  final String departmentName;
  final String managerName;
  final DateTime? startDate;
  final DateTime? endDate;
}

class ProjectDetail {
  ProjectDetail({
    required this.id,
    required this.name,
    required this.description,
    required this.managerName,
    required this.secondManagerName,
    required this.unit,
    required this.type,
    required this.status,
    required this.progressPercent,
    required this.deviationDays,
    required this.budget,
    required this.currency,
    required this.startDate,
    required this.endDate,
    required this.departments,
    required this.enabledComponents,
  });

  final String id;
  final String name;
  final String description;
  final String managerName;
  final String? secondManagerName;
  final String unit;
  final ProjectType type;
  final String status;
  final double progressPercent;
  final int deviationDays;
  final double? budget;
  final String? currency;
  final DateTime? startDate;
  final DateTime? endDate;
  final List<ProjectDepartment> departments;
  final List<String> enabledComponents;
}
