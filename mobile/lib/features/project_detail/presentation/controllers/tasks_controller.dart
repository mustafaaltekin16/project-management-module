import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/task_item.dart';
import 'project_detail_providers.dart';

class TasksController extends FamilyAsyncNotifier<List<TaskGroup>, String> {
  @override
  Future<List<TaskGroup>> build(String arg) {
    return ref.read(taskApiServiceProvider).getTaskGroups(arg);
  }

  Future<void> updateStatus(String projectId, String taskGroupId, String taskId, String status) async {
    await ref.read(taskApiServiceProvider).updateStatus(taskGroupId, taskId, status);
    state = await AsyncValue.guard(() => ref.read(taskApiServiceProvider).getTaskGroups(projectId));
  }
}

final tasksControllerProvider = AsyncNotifierProvider.family<TasksController, List<TaskGroup>, String>(
  TasksController.new,
);
