import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_indicator.dart';
import '../../domain/entities/board_column.dart';
import '../../domain/entities/project.dart';
import '../controllers/board_controller.dart';
import 'project_list_item_card.dart';

class KanbanBoardView extends ConsumerStatefulWidget {
  const KanbanBoardView({
    super.key,
    required this.projects,
    required this.onOpenProject,
  });

  final List<Project> projects;
  final void Function(String projectId) onOpenProject;

  @override
  ConsumerState<KanbanBoardView> createState() => _KanbanBoardViewState();
}

class _KanbanBoardViewState extends ConsumerState<KanbanBoardView>
    with SingleTickerProviderStateMixin {
  TabController? _tabController;
  int _columnCount = 0;

  void _ensureTabController(int count) {
    if (_tabController == null || _columnCount != count) {
      _tabController?.dispose();
      _tabController = TabController(length: count, vsync: this);
      _columnCount = count;
    }
  }

  @override
  void dispose() {
    _tabController?.dispose();
    super.dispose();
  }

  Future<void> _showMoveSheet(
    BuildContext context,
    Project project,
    List<BoardColumn> columns,
  ) async {
    final target = await showModalBottomSheet<String>(
      context: context,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Padding(
              padding: EdgeInsets.all(16),
              child: Text(
                'Şuraya taşı',
                style: TextStyle(fontWeight: FontWeight.bold),
              ),
            ),
            ...columns
                .where((c) => c.id != project.boardColumnId)
                .map(
                  (c) => ListTile(
                    title: Text(c.name),
                    onTap: () => Navigator.of(context).pop(c.id),
                  ),
                ),
          ],
        ),
      ),
    );
    if (target != null) {
      await ref
          .read(boardControllerProvider.notifier)
          .movePlacement(
            project.id,
            target,
            project.updatedAtUtc ?? DateTime.now().toIso8601String(),
          );
    }
  }

  @override
  Widget build(BuildContext context) {
    final boardAsync = ref.watch(boardControllerProvider);

    return boardAsync.when(
      loading: () => const LoadingIndicator(),
      error: (e, _) => ErrorView(
        message: 'Pano yüklenemedi.',
        onRetry: () => ref.invalidate(boardControllerProvider),
      ),
      data: (unsortedColumns) {
        if (unsortedColumns.isEmpty) {
          return const EmptyState(message: 'Pano sütunu tanımlı değil.');
        }
        final columns = [...unsortedColumns]
          ..sort((a, b) => a.sortOrder.compareTo(b.sortOrder));
        _ensureTabController(columns.length);

        final projectsByColumn = <String, List<Project>>{};
        for (final p in widget.projects) {
          if (p.boardColumnId == null) continue;
          projectsByColumn.putIfAbsent(p.boardColumnId!, () => []).add(p);
        }

        return Column(
          children: [
            TabBar(
              controller: _tabController,
              isScrollable: true,
              tabs: columns
                  .map(
                    (c) => Tab(
                      text:
                          '${c.name} (${projectsByColumn[c.id]?.length ?? 0})',
                    ),
                  )
                  .toList(),
            ),
            Expanded(
              child: TabBarView(
                controller: _tabController,
                children: columns.map((column) {
                  final columnProjects =
                      projectsByColumn[column.id] ?? const <Project>[];
                  if (columnProjects.isEmpty) {
                    return const EmptyState(message: 'Bu sütunda proje yok.');
                  }
                  return ListView.builder(
                    itemCount: columnProjects.length,
                    itemBuilder: (context, index) {
                      final project = columnProjects[index];
                      return Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        child: ProjectListItemCard(
                          project: project,
                          onTap: () => widget.onOpenProject(project.id),
                          action: IconButton(
                            tooltip: 'Projeyi taşı',
                            icon: const Icon(Icons.more_horiz_rounded),
                            onPressed: () =>
                                _showMoveSheet(context, project, columns),
                          ),
                        ),
                      );
                    },
                  );
                }).toList(),
              ),
            ),
          ],
        );
      },
    );
  }
}
