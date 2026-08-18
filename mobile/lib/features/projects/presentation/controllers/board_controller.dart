import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/board_column.dart';
import 'projects_list_controller.dart';
import 'projects_providers.dart';

class BoardController extends AsyncNotifier<List<BoardColumn>> {
  @override
  Future<List<BoardColumn>> build() {
    return ref.read(projectRepositoryProvider).getBoardColumns();
  }

  Future<void> movePlacement(String projectId, String targetColumnId, String expectedUpdatedAtUtc) async {
    await ref.read(projectRepositoryProvider).movePlacement(projectId, targetColumnId, expectedUpdatedAtUtc);
    state = await AsyncValue.guard(() => ref.read(projectRepositoryProvider).getBoardColumns());
    await ref.read(projectsListControllerProvider.notifier).refresh();
  }
}

final boardControllerProvider = AsyncNotifierProvider<BoardController, List<BoardColumn>>(
  BoardController.new,
);
