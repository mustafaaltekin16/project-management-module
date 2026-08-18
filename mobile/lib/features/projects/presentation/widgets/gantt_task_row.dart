import 'package:flutter/material.dart';

import '../../domain/entities/gantt_task_summary.dart';
import '../controllers/gantt_controller.dart';

class GanttTaskRow extends StatelessWidget {
  const GanttTaskRow({
    super.key,
    required this.task,
    required this.range,
    required this.leftColWidth,
    required this.totalTimelineWidth,
  });

  final GanttTaskSummary task;
  final GanttRange range;
  final double leftColWidth;
  final double totalTimelineWidth;

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
    final hasDates = task.startDate != null && task.dueDate != null;
    return SizedBox(
      height: 32,
      child: Row(
        children: [
          SizedBox(
            width: leftColWidth,
            child: Padding(
              padding: EdgeInsets.only(left: 12.0 + task.depth * 12, right: 4),
              child: Row(
                children: [
                  Container(width: 8, height: 8, decoration: BoxDecoration(color: _statusColor, shape: BoxShape.circle)),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(task.title, style: const TextStyle(fontSize: 12), maxLines: 1, overflow: TextOverflow.ellipsis),
                  ),
                ],
              ),
            ),
          ),
          SizedBox(
            width: totalTimelineWidth,
            child: hasDates
                ? Stack(
                    children: [
                      Positioned(
                        left: range.startPercentOf(task.startDate!) / 100 * totalTimelineWidth,
                        width: (range.widthPercentOf(task.startDate, task.dueDate) / 100 * totalTimelineWidth).clamp(4, totalTimelineWidth),
                        top: 10,
                        child: Container(height: 8, decoration: BoxDecoration(color: _statusColor, borderRadius: BorderRadius.circular(4))),
                      ),
                    ],
                  )
                : const Padding(
                    padding: EdgeInsets.only(left: 8),
                    child: Text('Tarih planlanmadı', style: TextStyle(fontSize: 11, color: Colors.grey)),
                  ),
          ),
        ],
      ),
    );
  }
}
