import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_indicator.dart';
import '../../domain/entities/project.dart';
import '../controllers/projects_list_controller.dart';
import '../widgets/gantt_chart_view.dart';
import '../widgets/kanban_board_view.dart';
import '../widgets/project_list_item_card.dart';

enum _ProjectsView { list, kanban, gantt }

class ProjectsListScreen extends ConsumerStatefulWidget {
  const ProjectsListScreen({super.key});

  @override
  ConsumerState<ProjectsListScreen> createState() => _ProjectsListScreenState();
}

class _ProjectsListScreenState extends ConsumerState<ProjectsListScreen> {
  _ProjectsView _view = _ProjectsView.list;
  String? _status;
  final _searchController = TextEditingController();
  Timer? _searchDebounce;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref
        .read(projectsListControllerProvider.notifier)
        .params
        .query;
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged(String value) {
    setState(() {});
    _searchDebounce?.cancel();
    _searchDebounce = Timer(const Duration(milliseconds: 350), () {
      ref.read(projectsListControllerProvider.notifier).setQuery(value.trim());
    });
  }

  void _resetFilters() {
    _searchDebounce?.cancel();
    _searchController.clear();
    setState(() => _status = null);
    ref.read(projectsListControllerProvider.notifier).resetFilters();
  }

  String get _viewLabel {
    switch (_view) {
      case _ProjectsView.list:
        return 'Liste';
      case _ProjectsView.kanban:
        return 'Kanban';
      case _ProjectsView.gantt:
        return 'Zaman planı';
    }
  }

  IconData get _viewIcon {
    switch (_view) {
      case _ProjectsView.list:
        return Icons.view_agenda_outlined;
      case _ProjectsView.kanban:
        return Icons.view_column_outlined;
      case _ProjectsView.gantt:
        return Icons.timeline_outlined;
    }
  }

  @override
  Widget build(BuildContext context) {
    final projectsAsync = ref.watch(projectsListControllerProvider);
    final params = ref.read(projectsListControllerProvider.notifier).params;
    final scheme = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Projeler'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: AppSpacing.sm),
            child: IconButton.filled(
              tooltip: 'Yeni proje oluştur',
              onPressed: () => context.push(RoutePaths.projectCreate),
              icon: const Icon(Icons.add_rounded),
            ),
          ),
        ],
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.md,
              AppSpacing.xs,
              AppSpacing.md,
              0,
            ),
            child: TextField(
              controller: _searchController,
              textInputAction: TextInputAction.search,
              decoration: InputDecoration(
                hintText: 'Proje, yönetici veya birim ara',
                prefixIcon: const Icon(Icons.search_rounded),
                suffixIcon: _searchController.text.isEmpty
                    ? null
                    : IconButton(
                        tooltip: 'Aramayı temizle',
                        onPressed: () {
                          _searchController.clear();
                          _onSearchChanged('');
                        },
                        icon: const Icon(Icons.close_rounded),
                      ),
              ),
              onChanged: _onSearchChanged,
            ),
          ),
          SizedBox(
            height: 56,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.md,
                vertical: AppSpacing.xs,
              ),
              children: [
                FilterChip(
                  label: const Text('Tümü'),
                  selected:
                      params.type == null &&
                      _status == null &&
                      _searchController.text.isEmpty,
                  onSelected: (_) => _resetFilters(),
                ),
                const SizedBox(width: AppSpacing.xs),
                FilterChip(
                  label: const Text('Aktif'),
                  selected: _status == 'Active',
                  onSelected: (selected) =>
                      setState(() => _status = selected ? 'Active' : null),
                ),
                const SizedBox(width: AppSpacing.xs),
                FilterChip(
                  label: const Text('Geciken'),
                  selected: _status == 'Overdue',
                  onSelected: (selected) =>
                      setState(() => _status = selected ? 'Overdue' : null),
                ),
                const SizedBox(width: AppSpacing.xs),
                FilterChip(
                  label: const Text('Çoklu birim'),
                  selected: params.type == ProjectType.multiUnit,
                  onSelected: (selected) => ref
                      .read(projectsListControllerProvider.notifier)
                      .setType(selected ? ProjectType.multiUnit : null),
                ),
                const SizedBox(width: AppSpacing.xs),
                FilterChip(
                  label: const Text('Fizibilite'),
                  selected: params.type == ProjectType.feasibilityBased,
                  onSelected: (selected) => ref
                      .read(projectsListControllerProvider.notifier)
                      .setType(selected ? ProjectType.feasibilityBased : null),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.md,
              AppSpacing.xs,
              AppSpacing.md,
              AppSpacing.sm,
            ),
            child: Row(
              children: [
                Expanded(
                  child: projectsAsync.maybeWhen(
                    data: (projects) {
                      final count = _filteredProjects(projects).length;
                      return Text(
                        '$count proje',
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: scheme.onSurfaceVariant,
                          fontWeight: FontWeight.w600,
                        ),
                      );
                    },
                    orElse: () => const SizedBox.shrink(),
                  ),
                ),
                PopupMenuButton<_ProjectsView>(
                  tooltip: 'Görünümü değiştir',
                  onSelected: (view) => setState(() => _view = view),
                  itemBuilder: (context) => const [
                    PopupMenuItem(
                      value: _ProjectsView.list,
                      child: ListTile(
                        leading: Icon(Icons.view_agenda_outlined),
                        title: Text('Liste'),
                      ),
                    ),
                    PopupMenuItem(
                      value: _ProjectsView.kanban,
                      child: ListTile(
                        leading: Icon(Icons.view_column_outlined),
                        title: Text('Kanban'),
                      ),
                    ),
                    PopupMenuItem(
                      value: _ProjectsView.gantt,
                      child: ListTile(
                        leading: Icon(Icons.timeline_outlined),
                        title: Text('Zaman planı'),
                      ),
                    ),
                  ],
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppSpacing.sm,
                      vertical: AppSpacing.xs,
                    ),
                    decoration: BoxDecoration(
                      color: scheme.surface,
                      border: Border.all(color: scheme.outlineVariant),
                      borderRadius: BorderRadius.circular(AppRadius.sm),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(_viewIcon, size: 18),
                        const SizedBox(width: AppSpacing.xs),
                        Text(
                          _viewLabel,
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                        const SizedBox(width: AppSpacing.xxs),
                        const Icon(Icons.expand_more_rounded, size: 18),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: projectsAsync.when(
              loading: () =>
                  const LoadingIndicator(label: 'Projeler hazırlanıyor'),
              error: (e, _) => ErrorView(
                message: 'Projeler yüklenemedi.',
                onRetry: () =>
                    ref.read(projectsListControllerProvider.notifier).refresh(),
              ),
              data: (projects) {
                final visibleProjects = _filteredProjects(projects);
                if (visibleProjects.isEmpty) {
                  return EmptyState(
                    title: 'Eşleşen proje bulunamadı',
                    message:
                        'Arama metnini veya seçili filtreleri değiştirmeyi deneyin.',
                    icon: Icons.search_off_rounded,
                    actionLabel: 'Filtreleri temizle',
                    onAction: _resetFilters,
                  );
                }
                switch (_view) {
                  case _ProjectsView.list:
                    return RefreshIndicator(
                      onRefresh: () => ref
                          .read(projectsListControllerProvider.notifier)
                          .refresh(),
                      child: ListView.builder(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.fromLTRB(
                          AppSpacing.md,
                          0,
                          AppSpacing.md,
                          AppSpacing.xl,
                        ),
                        itemCount: visibleProjects.length,
                        itemBuilder: (context, index) {
                          final project = visibleProjects[index];
                          return ProjectListItemCard(
                            project: project,
                            onTap: () => context.push(
                              RoutePaths.projectDetail(project.id),
                            ),
                          );
                        },
                      ),
                    );
                  case _ProjectsView.kanban:
                    return KanbanBoardView(
                      projects: visibleProjects,
                      onOpenProject: (id) =>
                          context.push(RoutePaths.projectDetail(id)),
                    );
                  case _ProjectsView.gantt:
                    return GanttChartView(projects: visibleProjects);
                }
              },
            ),
          ),
        ],
      ),
    );
  }

  List<Project> _filteredProjects(List<Project> projects) {
    if (_status == null) {
      return projects;
    }
    if (_status == 'Overdue') {
      return projects.where((project) => project.deviationDays > 0).toList();
    }
    return projects.where((project) => project.status == _status).toList();
  }
}
