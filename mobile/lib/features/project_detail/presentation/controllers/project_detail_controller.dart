import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../projects/domain/entities/project_detail.dart';
import '../../../projects/domain/entities/timeline_work_package.dart';
import '../../../projects/presentation/controllers/projects_providers.dart';

class ProjectDetailController extends FamilyAsyncNotifier<ProjectDetail, String> {
  @override
  Future<ProjectDetail> build(String arg) {
    return ref.read(projectRepositoryProvider).getById(arg);
  }

  Future<void> refresh(String projectId) async {
    state = await AsyncValue.guard(() => ref.read(projectRepositoryProvider).getById(projectId));
  }
}

final projectDetailControllerProvider =
    AsyncNotifierProvider.family<ProjectDetailController, ProjectDetail, String>(
  ProjectDetailController.new,
);

final projectTimelineProvider = FutureProvider.family<List<TimelineWorkPackage>, String>((ref, projectId) {
  return ref.read(projectRepositoryProvider).getTimeline(projectId);
});
