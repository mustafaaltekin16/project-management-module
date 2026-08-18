import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_indicator.dart';
import '../controllers/tasks_controller.dart';
import 'task_tile.dart';

class TasksTab extends ConsumerWidget {
  const TasksTab({super.key, required this.projectId});

  final String projectId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final groupsAsync = ref.watch(tasksControllerProvider(projectId));

    return groupsAsync.when(
      loading: () => const LoadingIndicator(),
      error: (e, _) => ErrorView(
        message: 'Görevler yüklenemedi.',
        onRetry: () => ref.invalidate(tasksControllerProvider(projectId)),
      ),
      data: (groups) {
        final allTasks = groups.expand((g) => g.tasks.expand((t) => t.flatten())).toList();
        if (allTasks.isEmpty) {
          return const EmptyState(message: 'Görev tanımlı değil.', icon: Icons.task_alt);
        }
        return ListView(
          children: [
            for (final group in groups) ...[
              if (groups.length > 1)
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
                  child: Text(group.title, style: Theme.of(context).textTheme.titleSmall),
                ),
              for (final rootTask in group.tasks)
                for (final task in rootTask.flatten())
                  TaskTile(
                    task: task,
                    onStatusChange: (newStatus) => ref
                        .read(tasksControllerProvider(projectId).notifier)
                        .updateStatus(projectId, group.id, task.id, newStatus),
                  ),
            ],
            const SizedBox(height: 16),
          ],
        );
      },
    );
  }
}
