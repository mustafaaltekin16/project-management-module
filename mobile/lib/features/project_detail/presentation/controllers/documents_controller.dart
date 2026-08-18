import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/project_document.dart';
import 'project_detail_providers.dart';

class DocumentsController extends FamilyAsyncNotifier<List<ProjectDocument>, String> {
  @override
  Future<List<ProjectDocument>> build(String arg) {
    return ref.read(documentApiServiceProvider).list(arg);
  }

  Future<void> upload(String projectId, String filePath, String fileName) async {
    await ref.read(documentApiServiceProvider).upload(projectId, filePath, fileName);
    state = await AsyncValue.guard(() => ref.read(documentApiServiceProvider).list(projectId));
  }

  Future<void> delete(String projectId, String documentId) async {
    await ref.read(documentApiServiceProvider).delete(projectId, documentId);
    state = await AsyncValue.guard(() => ref.read(documentApiServiceProvider).list(projectId));
  }
}

final documentsControllerProvider =
    AsyncNotifierProvider.family<DocumentsController, List<ProjectDocument>, String>(
  DocumentsController.new,
);
