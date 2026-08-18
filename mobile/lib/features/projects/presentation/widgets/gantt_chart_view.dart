import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/widgets/empty_state.dart';
import '../../domain/entities/project.dart';
import '../controllers/gantt_controller.dart';
import 'gantt_task_row.dart';

const _leftColWidth = 150.0;
const _monthWidth = 120.0;

class GanttChartView extends ConsumerWidget {
  const GanttChartView({super.key, required this.projects});

  final List<Project> projects;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (projects.isEmpty) {
      return const EmptyState(message: 'Gösterilecek proje yok.');
    }

    final range = computeGanttRange(projects);
    final rows = buildGanttRows(projects, range);
    final totalTimelineWidth = range.months * _monthWidth;
    final expanded = ref.watch(expandedGanttProjectsProvider);

    return SingleChildScrollView(
      scrollDirection: Axis.vertical,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: SizedBox(
          width: _leftColWidth + totalTimelineWidth,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              SizedBox(
                height: 40,
                child: Row(
                  children: [
                    const SizedBox(width: _leftColWidth),
                    ...range.monthLabels.map(
                      (label) => SizedBox(
                        width: _monthWidth,
                        child: Text(
                          label,
                          style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 12),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const Divider(height: 1),
              for (final row in rows) ...[
                InkWell(
                  onTap: () {
                    final next = {...expanded};
                    if (next.contains(row.project.id)) {
                      next.remove(row.project.id);
                    } else {
                      next.add(row.project.id);
                    }
                    ref.read(expandedGanttProjectsProvider.notifier).state = next;
                  },
                  child: SizedBox(
                    height: 44,
                    child: Row(
                      children: [
                        SizedBox(
                          width: _leftColWidth,
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 8),
                            child: Row(
                              children: [
                                Icon(
                                  expanded.contains(row.project.id) ? Icons.expand_more : Icons.chevron_right,
                                  size: 18,
                                ),
                                Expanded(
                                  child: Text(
                                    row.project.name,
                                    style: const TextStyle(fontSize: 13),
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        SizedBox(
                          width: totalTimelineWidth,
                          height: 44,
                          child: Stack(
                            children: [
                              Positioned(
                                left: row.startPercent / 100 * totalTimelineWidth,
                                width: (row.widthPercent / 100 * totalTimelineWidth).clamp(6, totalTimelineWidth),
                                top: 14,
                                child: Container(
                                  height: 16,
                                  decoration: BoxDecoration(
                                    color: Color(row.color),
                                    borderRadius: BorderRadius.circular(4),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                if (expanded.contains(row.project.id))
                  Consumer(
                    builder: (context, ref, _) {
                      final tasksAsync = ref.watch(ganttTasksProvider(row.project.id));
                      return tasksAsync.when(
                        loading: () => const Padding(
                          padding: EdgeInsets.symmetric(vertical: 8),
                          child: SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          ),
                        ),
                        error: (e, _) => const Padding(
                          padding: EdgeInsets.only(left: 24),
                          child: Text('Görevler yüklenemedi', style: TextStyle(fontSize: 11, color: Colors.red)),
                        ),
                        data: (tasks) {
                          if (tasks.isEmpty) {
                            return const Padding(
                              padding: EdgeInsets.only(left: 24, top: 4, bottom: 4),
                              child: Text('Görev yok', style: TextStyle(fontSize: 11, color: Colors.grey)),
                            );
                          }
                          return Column(
                            children: tasks
                                .map((t) => GanttTaskRow(
                                      task: t,
                                      range: range,
                                      leftColWidth: _leftColWidth,
                                      totalTimelineWidth: totalTimelineWidth,
                                    ))
                                .toList(),
                          );
                        },
                      );
                    },
                  ),
                const Divider(height: 1),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
