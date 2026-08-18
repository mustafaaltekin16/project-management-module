import '../../domain/entities/timeline_work_package.dart';

class ProjectTimelineDto {
  ProjectTimelineDto({required this.workPackages});

  final List<TimelineWorkPackage> workPackages;

  factory ProjectTimelineDto.fromJson(Map<String, dynamic> json) {
    final items = (json['workPackages'] as List? ?? [])
        .map((e) => TimelineWorkPackage(
              id: e['id'].toString(),
              title: e['title'] as String? ?? '',
              startDate: e['startDate'] == null ? null : DateTime.tryParse(e['startDate'] as String),
              endDate: e['endDate'] == null ? null : DateTime.tryParse(e['endDate'] as String),
              deviationDays: (e['deviationDays'] as num?)?.toInt() ?? 0,
              state: e['state'] as String? ?? 'Pending',
            ))
        .toList();
    return ProjectTimelineDto(workPackages: items);
  }
}
