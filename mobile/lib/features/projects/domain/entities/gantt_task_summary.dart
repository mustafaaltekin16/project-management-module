class GanttTaskSummary {
  GanttTaskSummary({
    required this.id,
    required this.title,
    required this.startDate,
    required this.dueDate,
    required this.status,
    required this.depth,
  });

  final String id;
  final String title;
  final DateTime? startDate;
  final DateTime? dueDate;
  final String status; // Todo, InProgress, Done
  final int depth;
}
