class TimelineWorkPackage {
  TimelineWorkPackage({
    required this.id,
    required this.title,
    required this.startDate,
    required this.endDate,
    required this.deviationDays,
    required this.state,
  });

  final String id;
  final String title;
  final DateTime? startDate;
  final DateTime? endDate;
  final int deviationDays;
  final String state; // Pending, Active, Completed, Blocked
}
