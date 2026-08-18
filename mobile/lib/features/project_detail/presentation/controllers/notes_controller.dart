import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/project_note.dart';
import 'project_detail_providers.dart';

class NotesController extends FamilyAsyncNotifier<List<ProjectNote>, String> {
  @override
  Future<List<ProjectNote>> build(String arg) {
    return ref.read(noteApiServiceProvider).list(arg);
  }

  Future<void> add(String projectId, String content) async {
    await ref.read(noteApiServiceProvider).add(projectId, content);
    state = await AsyncValue.guard(() => ref.read(noteApiServiceProvider).list(projectId));
  }
}

final notesControllerProvider = AsyncNotifierProvider.family<NotesController, List<ProjectNote>, String>(
  NotesController.new,
);
