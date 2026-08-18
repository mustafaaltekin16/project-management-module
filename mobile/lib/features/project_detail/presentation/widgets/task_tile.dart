import 'package:flutter/material.dart';

import '../../domain/entities/task_item.dart';
import 'task_status_menu.dart';

class TaskTile extends StatelessWidget {
  const TaskTile({super.key, required this.task, required this.onStatusChange});

  final TaskItem task;
  final void Function(String newStatus) onStatusChange;

  Color get _statusColor {
    switch (task.status) {
      case 'InProgress':
        return Colors.blue;
      case 'Done':
        return Colors.green;
      default:
        return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(left: 16.0 * task.depth),
      child: ListTile(
        dense: true,
        leading: Icon(Icons.circle, size: 12, color: _statusColor),
        title: Text(
          task.title,
          style: TextStyle(decoration: task.status == 'Done' ? TextDecoration.lineThrough : null),
        ),
        subtitle: task.assigneeName != null ? Text(task.assigneeName!) : null,
        trailing: IconButton(
          icon: const Icon(Icons.more_vert),
          onPressed: () async {
            final newStatus = await showTaskStatusMenu(context, task.status);
            if (newStatus != null && newStatus != task.status) {
              onStatusChange(newStatus);
            }
          },
        ),
      ),
    );
  }
}
