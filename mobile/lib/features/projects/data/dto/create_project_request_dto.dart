class CreateDepartmentRowDto {
  CreateDepartmentRowDto({
    required this.departmentId,
    required this.title,
    required this.departmentName,
    required this.managerEmployeeId,
    required this.managerName,
    this.startDate,
    this.endDate,
  });

  final String departmentId;
  final String title;
  final String departmentName;
  final String managerEmployeeId;
  final String managerName;
  final DateTime? startDate;
  final DateTime? endDate;

  Map<String, dynamic> toJson() => {
        'departmentId': departmentId,
        'title': title,
        'departmentName': departmentName,
        'managerEmployeeId': managerEmployeeId,
        'managerName': managerName,
        if (startDate != null) 'startDate': startDate!.toIso8601String(),
        if (endDate != null) 'endDate': endDate!.toIso8601String(),
      };
}

class CreateProjectRequestDto {
  CreateProjectRequestDto({
    required this.name,
    required this.description,
    required this.managerEmployeeId,
    required this.managerName,
    this.secondManagerEmployeeId,
    this.secondManagerName,
    required this.unitDepartmentId,
    required this.unit,
    required this.type,
    this.budget,
    this.currency,
    required this.startDate,
    required this.endDate,
    this.templateId,
    required this.enabledComponents,
    required this.templateValues,
    required this.departments,
  });

  final String name;
  final String description;
  final String managerEmployeeId;
  final String managerName;
  final String? secondManagerEmployeeId;
  final String? secondManagerName;
  final String unitDepartmentId;
  final String unit;
  final String type;
  final double? budget;
  final String? currency;
  final DateTime startDate;
  final DateTime endDate;
  final String? templateId;
  final List<String> enabledComponents;
  final Map<String, dynamic> templateValues;
  final List<CreateDepartmentRowDto> departments;

  Map<String, dynamic> toJson() => {
        'name': name,
        'description': description,
        'managerEmployeeId': managerEmployeeId,
        'managerName': managerName,
        if (secondManagerEmployeeId != null) 'secondManagerEmployeeId': secondManagerEmployeeId,
        if (secondManagerName != null) 'secondManagerName': secondManagerName,
        'unitDepartmentId': unitDepartmentId,
        'unit': unit,
        'type': type,
        if (budget != null) 'budget': budget,
        if (currency != null) 'currency': currency,
        'startDate': startDate.toIso8601String(),
        'endDate': endDate.toIso8601String(),
        if (templateId != null) 'templateId': templateId,
        'enabledComponents': enabledComponents,
        'templateValues': templateValues.entries.map((e) => {'fieldId': e.key, 'value': e.value}).toList(),
        'departments': departments.map((d) => d.toJson()).toList(),
      };
}
