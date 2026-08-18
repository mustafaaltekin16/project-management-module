import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../domain/entities/activity_event.dart';

class ActivityEventTile extends StatelessWidget {
  const ActivityEventTile({super.key, required this.event});

  final ActivityEvent event;

  IconData get _icon {
    switch (event.type) {
      case ActivityEventType.note:
        return Icons.sticky_note_2_outlined;
      case ActivityEventType.task:
        return Icons.task_alt;
      case ActivityEventType.document:
        return Icons.insert_drive_file_outlined;
    }
  }

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('dd.MM.yyyy HH:mm');
    return ListTile(
      leading: Icon(_icon),
      title: Text(event.title),
      subtitle: Text(event.subtitle, maxLines: 2, overflow: TextOverflow.ellipsis),
      trailing: Text(dateFormat.format(event.timestamp), style: const TextStyle(fontSize: 11, color: Colors.grey)),
    );
  }
}
