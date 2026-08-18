enum ActivityEventType { note, task, document }

class ActivityEvent {
  ActivityEvent({
    required this.type,
    required this.title,
    required this.subtitle,
    required this.timestamp,
  });

  final ActivityEventType type;
  final String title;
  final String subtitle;
  final DateTime timestamp;
}
