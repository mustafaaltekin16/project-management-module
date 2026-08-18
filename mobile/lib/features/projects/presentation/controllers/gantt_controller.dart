import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../domain/entities/gantt_task_summary.dart';
import '../../domain/entities/project.dart';
import 'projects_providers.dart';

class GanttRange {
  GanttRange({required this.start, required this.months});

  final DateTime start; // first day of first month, local
  final int months;

  DateTime get end => DateTime(start.year, start.month + months, 1);

  int get totalDays => end.difference(start).inDays;

  List<String> get monthLabels {
    final formatter = DateFormat('MMMM yyyy', 'tr_TR');
    return List.generate(months, (i) {
      final month = DateTime(start.year, start.month + i, 1);
      return formatter.format(month);
    });
  }

  double startPercentOf(DateTime date) {
    final clamped = date.isBefore(start) ? start : date;
    return clamped.difference(start).inDays / totalDays * 100;
  }

  double widthPercentOf(DateTime? startDate, DateTime? endDate) {
    final s = startDate ?? start;
    final e = endDate ?? end;
    final clampedStart = s.isBefore(start) ? start : s;
    final clampedEnd = e.isAfter(end) ? end : e;
    final days = clampedEnd.difference(clampedStart).inDays;
    return (days <= 0 ? 1 : days) / totalDays * 100;
  }
}

GanttRange computeGanttRange(List<Project> projects) {
  final now = DateTime.now();
  DateTime? minStart;
  DateTime? maxEnd;
  for (final p in projects) {
    if (p.startDate != null && (minStart == null || p.startDate!.isBefore(minStart))) {
      minStart = p.startDate;
    }
    if (p.endDate != null && (maxEnd == null || p.endDate!.isAfter(maxEnd))) {
      maxEnd = p.endDate;
    }
  }
  minStart ??= DateTime(now.year, now.month, 1);
  maxEnd ??= DateTime(now.year, now.month + 3, 1);

  final start = DateTime(minStart.year, minStart.month, 1);
  var months = (maxEnd.year - start.year) * 12 + (maxEnd.month - start.month) + 1;
  if (months < 1) months = 1;
  return GanttRange(start: start, months: months);
}

class GanttRowData {
  GanttRowData({required this.project, required this.startPercent, required this.widthPercent, required this.color});

  final Project project;
  final double startPercent;
  final double widthPercent;
  final int color;
}

const ganttBarColors = <int>[
  0xFF3F51B5,
  0xFF009688,
  0xFFFF9800,
  0xFF9C27B0,
  0xFF03A9F4,
  0xFFE91E63,
  0xFF4CAF50,
  0xFF795548,
];

List<GanttRowData> buildGanttRows(List<Project> projects, GanttRange range) {
  return List.generate(projects.length, (i) {
    final p = projects[i];
    return GanttRowData(
      project: p,
      startPercent: range.startPercentOf(p.startDate ?? range.start),
      widthPercent: range.widthPercentOf(p.startDate, p.endDate),
      color: ganttBarColors[i % ganttBarColors.length],
    );
  });
}

final expandedGanttProjectsProvider = StateProvider<Set<String>>((ref) => {});

final ganttTasksProvider = FutureProvider.family<List<GanttTaskSummary>, String>((ref, projectId) {
  return ref.read(projectRepositoryProvider).getTaskGroupsForGantt(projectId);
});
