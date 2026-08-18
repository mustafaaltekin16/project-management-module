import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_indicator.dart';
import '../../../feasibility/presentation/widgets/feasibility_tab.dart';
import '../../../projects/domain/entities/project.dart';
import '../../../projects/domain/entities/project_detail.dart';
import '../../../projects/presentation/widgets/project_status_badge.dart';
import '../controllers/project_detail_controller.dart';
import '../widgets/activity_tab.dart';
import '../widgets/description_tab.dart';
import '../widgets/documents_tab.dart';
import '../widgets/tasks_tab.dart';

class ProjectDetailScreen extends ConsumerWidget {
  const ProjectDetailScreen({super.key, required this.projectId});

  final String projectId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detailAsync = ref.watch(projectDetailControllerProvider(projectId));

    return detailAsync.when(
      loading: () => Scaffold(
        appBar: AppBar(title: const Text('Proje detayı')),
        body: const LoadingIndicator(label: 'Proje hazırlanıyor'),
      ),
      error: (e, _) => Scaffold(
        appBar: AppBar(title: const Text('Proje detayı')),
        body: ErrorView(
          message: 'Proje bilgileri yüklenemedi.',
          onRetry: () => ref
              .read(projectDetailControllerProvider(projectId).notifier)
              .refresh(projectId),
        ),
      ),
      data: (detail) {
        final isFeasibilityBased = detail.type == ProjectType.feasibilityBased;
        final tabs = [
          _detailTab(Icons.dashboard_outlined, 'Genel'),
          _detailTab(Icons.task_alt_outlined, 'Görevler'),
          if (isFeasibilityBased)
            _detailTab(Icons.analytics_outlined, 'Fizibilite'),
          _detailTab(Icons.folder_outlined, 'Dosyalar'),
          _detailTab(Icons.history_rounded, 'Aktivite'),
        ];
        final tabViews = [
          DescriptionTab(detail: detail),
          TasksTab(projectId: projectId),
          if (isFeasibilityBased) FeasibilityTab(projectId: projectId),
          DocumentsTab(projectId: projectId),
          ActivityTab(projectId: projectId),
        ];

        return DefaultTabController(
          length: tabs.length,
          child: Scaffold(
            appBar: AppBar(title: const Text('Proje detayı')),
            body: Column(
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(
                    AppSpacing.md,
                    AppSpacing.xs,
                    AppSpacing.md,
                    AppSpacing.sm,
                  ),
                  child: _ProjectSummaryHeader(detail: detail),
                ),
                Material(
                  color: Theme.of(context).colorScheme.surface,
                  child: TabBar(
                    isScrollable: true,
                    tabAlignment: TabAlignment.start,
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppSpacing.xs,
                    ),
                    tabs: tabs,
                  ),
                ),
                Expanded(child: TabBarView(children: tabViews)),
              ],
            ),
          ),
        );
      },
    );
  }

  Tab _detailTab(IconData icon, String label) {
    return Tab(
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [Icon(icon, size: 17), const SizedBox(width: 6), Text(label)],
      ),
    );
  }
}

class _ProjectSummaryHeader extends StatelessWidget {
  const _ProjectSummaryHeader({required this.detail});

  final ProjectDetail detail;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final dateFormat = DateFormat('dd.MM.yyyy');
    final dateRange = detail.startDate == null && detail.endDate == null
        ? 'Tarih planlanmadı'
        : '${detail.startDate == null ? '?' : dateFormat.format(detail.startDate!)} – '
              '${detail.endDate == null ? '?' : dateFormat.format(detail.endDate!)}';
    final isOverdue = detail.deviationDays > 0;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Text(
                    detail.name,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                ProjectStatusBadge(status: detail.status),
              ],
            ),
            const SizedBox(height: AppSpacing.xs),
            Text(
              '${detail.managerName} · ${detail.unit}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(
                context,
              ).textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: AppSpacing.md),
            Row(
              children: [
                Expanded(
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(99),
                    child: LinearProgressIndicator(
                      value: (detail.progressPercent / 100).clamp(0, 1),
                      minHeight: 8,
                    ),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Text(
                  '%${detail.progressPercent.toStringAsFixed(0)}',
                  style: Theme.of(
                    context,
                  ).textTheme.labelLarge?.copyWith(fontWeight: FontWeight.w800),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            Wrap(
              spacing: AppSpacing.sm,
              runSpacing: AppSpacing.xs,
              children: [
                _SummaryMeta(icon: Icons.event_outlined, label: dateRange),
                if (detail.deviationDays != 0)
                  _SummaryMeta(
                    icon: isOverdue
                        ? Icons.warning_amber_rounded
                        : Icons.trending_up_rounded,
                    label: isOverdue
                        ? '${detail.deviationDays} gün gecikme'
                        : '${detail.deviationDays.abs()} gün önde',
                    color: isOverdue ? scheme.error : const Color(0xFF15803D),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _SummaryMeta extends StatelessWidget {
  const _SummaryMeta({required this.icon, required this.label, this.color});

  final IconData icon;
  final String label;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final foreground = color ?? Theme.of(context).colorScheme.onSurfaceVariant;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 15, color: foreground),
        const SizedBox(width: 5),
        Text(
          label,
          style: TextStyle(
            fontSize: 12,
            color: foreground,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}
