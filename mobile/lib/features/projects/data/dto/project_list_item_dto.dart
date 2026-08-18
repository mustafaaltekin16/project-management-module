import '../../domain/entities/project.dart';

class ProjectListItemDto {
  ProjectListItemDto({
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

  factory ProjectListItemDto.fromJson(Map<String, dynamic> json) {
    return ProjectListItemDto(
      id: json['id'].toString(),
      name: json['name'] as String? ?? '',
      managerName: json['managerName'] as String? ?? '',
      unit: json['unit'] as String? ?? '',
      type: projectTypeFromJson(json['type'] as String?),
      status: json['status'] as String? ?? 'Draft',
      progressPercent: (json['progressPercent'] as num?)?.toDouble() ?? 0,
      deviationDays: (json['deviationDays'] as num?)?.toInt() ?? 0,
      budget: (json['budget'] as num?)?.toDouble(),
      currency: json['currency'] as String?,
      startDate: json['startDate'] == null ? null : DateTime.tryParse(json['startDate'] as String),
      endDate: json['endDate'] == null ? null : DateTime.tryParse(json['endDate'] as String),
      boardColumnId: json['boardColumnId'] as String?,
      updatedAtUtc: json['updatedAtUtc'] as String?,
    );
  }

  Project toEntity() => Project(
        id: id,
        name: name,
        managerName: managerName,
        unit: unit,
        type: type,
        status: status,
        progressPercent: progressPercent,
        deviationDays: deviationDays,
        budget: budget,
        currency: currency,
        startDate: startDate,
        endDate: endDate,
        boardColumnId: boardColumnId,
        updatedAtUtc: updatedAtUtc,
      );
}
