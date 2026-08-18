enum ProjectType { simple, multiUnit, feasibilityBased }

ProjectType projectTypeFromJson(String? value) {
  switch (value) {
    case 'MultiUnit':
      return ProjectType.multiUnit;
    case 'FeasibilityBased':
      return ProjectType.feasibilityBased;
    default:
      return ProjectType.simple;
  }
}

String projectTypeToJson(ProjectType type) {
  switch (type) {
    case ProjectType.multiUnit:
      return 'MultiUnit';
    case ProjectType.feasibilityBased:
      return 'FeasibilityBased';
    case ProjectType.simple:
      return 'Simple';
  }
}

class Project {
  Project({
    required this.id,
    required this.name,
    required this.managerName,
    required this.unit,
    required this.type,
    required this.status,
    required this.progressPercent,
    required this.deviationDays,
    required this.budget,
    required this.currency,
    required this.startDate,
    required this.endDate,
    required this.boardColumnId,
    required this.updatedAtUtc,
  });

  final String id;
  final String name;
  final String managerName;
  final String unit;
  final ProjectType type;
  final String status;
  final double progressPercent;
  final int deviationDays;
  final double? budget;
  final String? currency;
  final DateTime? startDate;
  final DateTime? endDate;
  final String? boardColumnId;
  final String? updatedAtUtc;
}
