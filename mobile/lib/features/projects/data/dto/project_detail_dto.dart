import '../../domain/entities/project.dart';
import '../../domain/entities/project_detail.dart';

class ProjectDetailDto {
  ProjectDetailDto({required this.detail});

  final ProjectDetail detail;

  factory ProjectDetailDto.fromJson(Map<String, dynamic> json) {
    final departments = (json['departments'] as List? ?? [])
        .map((e) => ProjectDepartment(
              departmentId: e['departmentId'].toString(),
              title: e['title'] as String? ?? '',
              departmentName: e['departmentName'] as String? ?? '',
              managerName: e['managerName'] as String? ?? '',
              startDate: e['startDate'] == null ? null : DateTime.tryParse(e['startDate'] as String),
              endDate: e['endDate'] == null ? null : DateTime.tryParse(e['endDate'] as String),
            ))
        .toList();

    return ProjectDetailDto(
      detail: ProjectDetail(
        id: json['id'].toString(),
        name: json['name'] as String? ?? '',
        description: json['description'] as String? ?? '',
        managerName: json['managerName'] as String? ?? '',
        secondManagerName: json['secondManagerName'] as String?,
        unit: json['unit'] as String? ?? '',
        type: projectTypeFromJson(json['type'] as String?),
        status: json['status'] as String? ?? 'Draft',
        progressPercent: (json['progressPercent'] as num?)?.toDouble() ?? 0,
        deviationDays: (json['deviationDays'] as num?)?.toInt() ?? 0,
        budget: (json['budget'] as num?)?.toDouble(),
        currency: json['currency'] as String?,
        startDate: json['startDate'] == null ? null : DateTime.tryParse(json['startDate'] as String),
        endDate: json['endDate'] == null ? null : DateTime.tryParse(json['endDate'] as String),
        departments: departments,
        enabledComponents: (json['enabledComponents'] as List? ?? []).map((e) => e.toString()).toList(),
      ),
    );
  }
}
