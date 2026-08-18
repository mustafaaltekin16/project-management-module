import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/project.dart';
import 'projects_providers.dart';

class ProjectSearchParams {
  const ProjectSearchParams({this.query = '', this.type});

  final String query;
  final ProjectType? type;

  ProjectSearchParams copyWith({
    String? query,
    ProjectType? type,
    bool clearType = false,
  }) {
    return ProjectSearchParams(
      query: query ?? this.query,
      type: clearType ? null : (type ?? this.type),
    );
  }
}

class ProjectsListController extends AsyncNotifier<List<Project>> {
  ProjectSearchParams _params = const ProjectSearchParams();
  ProjectSearchParams get params => _params;

  @override
  Future<List<Project>> build() => _fetch();

  Future<List<Project>> _fetch() {
    return ref
        .read(projectRepositoryProvider)
        .search(
          type: _params.type == null ? null : projectTypeToJson(_params.type!),
          q: _params.query,
        );
  }

  Future<void> setQuery(String query) async {
    _params = _params.copyWith(query: query);
    state = const AsyncLoading();
    state = await AsyncValue.guard(_fetch);
  }

  Future<void> setType(ProjectType? type) async {
    _params = _params.copyWith(type: type, clearType: type == null);
    state = const AsyncLoading();
    state = await AsyncValue.guard(_fetch);
  }

  Future<void> resetFilters() async {
    _params = const ProjectSearchParams();
    state = const AsyncLoading();
    state = await AsyncValue.guard(_fetch);
  }

  Future<void> refresh() async {
    state = await AsyncValue.guard(_fetch);
  }
}

final projectsListControllerProvider =
    AsyncNotifierProvider<ProjectsListController, List<Project>>(
      ProjectsListController.new,
    );
