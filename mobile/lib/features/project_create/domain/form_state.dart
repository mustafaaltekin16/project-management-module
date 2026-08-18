import '../../projects/domain/entities/project.dart';

class DepartmentRowFormData {
  DepartmentRowFormData({
    required this.departmentId,
    required this.departmentName,
    this.title = '',
    this.managerEmployeeId,
    this.managerName,
    this.startDate,
    this.endDate,
  });

  final String departmentId;
  final String departmentName;
  String title;
  String? managerEmployeeId;
  String? managerName;
  DateTime? startDate;
  DateTime? endDate;
}

class AttachmentFormData {
  AttachmentFormData({required this.path, required this.fileName});

  final String path;
  final String fileName;
}

class ProjectCreateFormState {
  ProjectCreateFormState({
    this.type = ProjectType.simple,
    this.name = '',
    this.description = '',
    this.startDate,
    this.endDate,
    this.managerEmployeeId,
    this.managerName,
    this.secondManagerEmployeeId,
    this.secondManagerName,
    this.budget,
    this.currency = 'TRY',
    List<DepartmentRowFormData>? departments,
    List<AttachmentFormData>? attachments,
    this.isSubmitting = false,
    List<String>? errors,
  })  : departments = departments ?? [],
        attachments = attachments ?? [],
        errors = errors ?? [];

  final ProjectType type;
  final String name;
  final String description;
  final DateTime? startDate;
  final DateTime? endDate;
  final String? managerEmployeeId;
  final String? managerName;
  final String? secondManagerEmployeeId;
  final String? secondManagerName;
  final double? budget;
  final String currency;
  final List<DepartmentRowFormData> departments;
  final List<AttachmentFormData> attachments;
  final bool isSubmitting;
  final List<String> errors;

  ProjectCreateFormState copyWith({
    ProjectType? type,
    String? name,
    String? description,
    DateTime? startDate,
    DateTime? endDate,
    String? managerEmployeeId,
    String? managerName,
    String? secondManagerEmployeeId,
    String? secondManagerName,
    double? budget,
    String? currency,
    List<DepartmentRowFormData>? departments,
    List<AttachmentFormData>? attachments,
    bool? isSubmitting,
    List<String>? errors,
  }) {
    return ProjectCreateFormState(
      type: type ?? this.type,
      name: name ?? this.name,
      description: description ?? this.description,
      startDate: startDate ?? this.startDate,
      endDate: endDate ?? this.endDate,
      managerEmployeeId: managerEmployeeId ?? this.managerEmployeeId,
      managerName: managerName ?? this.managerName,
      secondManagerEmployeeId: secondManagerEmployeeId ?? this.secondManagerEmployeeId,
      secondManagerName: secondManagerName ?? this.secondManagerName,
      budget: budget ?? this.budget,
      currency: currency ?? this.currency,
      departments: departments ?? this.departments,
      attachments: attachments ?? this.attachments,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      errors: errors ?? this.errors,
    );
  }
}
