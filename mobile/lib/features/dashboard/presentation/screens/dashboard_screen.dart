import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_indicator.dart';
import '../../../../core/widgets/section_header.dart';
import '../../../login/presentation/controllers/auth_controller.dart';
import '../../../projects/domain/entities/project.dart';
import '../../../projects/presentation/controllers/projects_list_controller.dart';
import '../widgets/mini_project_tile.dart';
import '../widgets/stat_tile.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final projectsAsync = ref.watch(projectsListControllerProvider);
    final user = ref.watch(authControllerProvider).valueOrNull;

    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: projectsAsync.when(
          loading: () => const LoadingIndicator(label: 'Panon hazırlanıyor'),
          error: (e, _) => ErrorView(
            message: 'Pano verileri yüklenemedi.',
            onRetry: () =>
                ref.read(projectsListControllerProvider.notifier).refresh(),
          ),
          data: (projects) {
            final active = projects.where((p) => p.status == 'Active').length;
            final completed = projects
                .where((p) => p.status == 'Completed')
                .length;
            final completionRate = projects.isEmpty
                ? 0
                : (completed / projects.length * 100).round();
            final myProjects = user == null
                ? const <Project>[]
                : projects
                      .where((p) => p.managerName == user.displayName)
                      .toList();
            final recent = [...projects]
              ..sort((a, b) {
                final aDate = a.updatedAtUtc == null
                    ? null
                    : DateTime.tryParse(a.updatedAtUtc!);
                final bDate = b.updatedAtUtc == null
                    ? null
                    : DateTime.tryParse(b.updatedAtUtc!);
                if (aDate == null && bDate == null) return 0;
                if (aDate == null) return 1;
                if (bDate == null) return -1;
                return bDate.compareTo(aDate);
              });
            final focusProjects = (myProjects.isNotEmpty ? myProjects : recent)
                .take(5)
                .toList();

            return RefreshIndicator(
              onRefresh: () =>
                  ref.read(projectsListControllerProvider.notifier).refresh(),
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(
                  AppSpacing.lg,
                  AppSpacing.sm,
                  AppSpacing.lg,
                  AppSpacing.xxl,
                ),
                children: [
                  _DashboardHeader(
                    displayName: user?.displayName ?? '',
                    onProfileTap: () => context.go(RoutePaths.profile),
                  ),
                  const SizedBox(height: AppSpacing.xl),
                  _QuickStartCard(
                    activeCount: active,
                    onCreate: () => context.push(RoutePaths.projectCreate),
                    onOpenProjects: () => context.go(RoutePaths.projects),
                  ),
                  const SizedBox(height: AppSpacing.xl),
                  const SectionHeader(
                    title: 'Portföy görünümü',
                    subtitle: 'Projelerinin güncel sağlık durumu',
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  GridView.count(
                    crossAxisCount: 2,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    mainAxisSpacing: AppSpacing.sm,
                    crossAxisSpacing: AppSpacing.sm,
                    childAspectRatio: 1.28,
                    children: [
                      StatTile(
                        icon: Icons.folder_copy_outlined,
                        label: 'Toplam proje',
                        value: projects.length,
                        color: ProjectTypeColors.simple,
                        onTap: () => context.go(RoutePaths.projects),
                      ),
                      StatTile(
                        icon: Icons.play_circle_outline_rounded,
                        label: 'Aktif',
                        value: active,
                        color: const Color(0xFF2563EB),
                        onTap: () => context.go(RoutePaths.projects),
                      ),
                      StatTile(
                        icon: Icons.task_alt_rounded,
                        label: 'Tamamlanan',
                        value: completed,
                        color: const Color(0xFF15803D),
                        onTap: () => context.go(RoutePaths.projects),
                      ),
                      StatTile(
                        icon: Icons.donut_large_rounded,
                        label: 'Tamamlanma oranı',
                        value: completionRate,
                        suffix: '%',
                        color: ProjectTypeColors.feasibilityBased,
                        onTap: () => context.go(RoutePaths.projects),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.xxl),
                  SectionHeader(
                    title: myProjects.isNotEmpty
                        ? 'Senin projelerin'
                        : 'Son güncellenenler',
                    subtitle: myProjects.isNotEmpty
                        ? 'Yönettiğin projelere hızlı eriş'
                        : 'En son hareket gören projeler',
                    actionLabel: 'Tümünü gör',
                    onAction: () => context.go(RoutePaths.projects),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  if (focusProjects.isEmpty)
                    _NoProjectsCard(
                      onCreate: () => context.push(RoutePaths.projectCreate),
                    )
                  else
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: AppSpacing.md,
                          vertical: AppSpacing.xxs,
                        ),
                        child: Column(
                          children: [
                            for (var i = 0; i < focusProjects.length; i++)
                              MiniProjectTile(
                                project: focusProjects[i],
                                showDivider: i < focusProjects.length - 1,
                                onTap: () => context.push(
                                  RoutePaths.projectDetail(focusProjects[i].id),
                                ),
                              ),
                          ],
                        ),
                      ),
                    ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

class _DashboardHeader extends StatelessWidget {
  const _DashboardHeader({
    required this.displayName,
    required this.onProfileTap,
  });

  final String displayName;
  final VoidCallback onProfileTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final firstName = displayName.trim().split(' ').firstOrNull ?? '';
    return Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Hoş geldin${firstName.isEmpty ? '' : ', $firstName'}',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: AppSpacing.xxs),
              Text(
                'Bugünün önceliklerine birlikte bakalım.',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: scheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(width: AppSpacing.sm),
        Semantics(
          button: true,
          label: 'Profili aç',
          child: InkWell(
            onTap: onProfileTap,
            borderRadius: BorderRadius.circular(AppRadius.md),
            child: Container(
              width: 48,
              height: 48,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                gradient: AppGradients.primary,
                borderRadius: BorderRadius.circular(AppRadius.md),
              ),
              child: Text(
                displayName.isNotEmpty ? displayName[0].toUpperCase() : '?',
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w800,
                  fontSize: 18,
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _QuickStartCard extends StatelessWidget {
  const _QuickStartCard({
    required this.activeCount,
    required this.onCreate,
    required this.onOpenProjects,
  });

  final int activeCount;
  final VoidCallback onCreate;
  final VoidCallback onOpenProjects;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.lg),
      decoration: BoxDecoration(
        gradient: AppGradients.primary,
        borderRadius: BorderRadius.circular(AppRadius.xl),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.sm,
              vertical: 6,
            ),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.15),
              borderRadius: BorderRadius.circular(AppRadius.sm),
            ),
            child: Text(
              '$activeCount aktif proje',
              style: const TextStyle(
                color: Colors.white,
                fontSize: 12,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          const Text(
            'Projelerini net ve\nodaklı yönet.',
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              height: 1.15,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          Row(
            children: [
              Expanded(
                child: FilledButton.icon(
                  onPressed: onCreate,
                  icon: const Icon(Icons.add_rounded),
                  label: const Text('Yeni proje'),
                  style: FilledButton.styleFrom(
                    backgroundColor: Colors.white,
                    foregroundColor: const Color(0xFF4B48C5),
                  ),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              IconButton.filledTonal(
                onPressed: onOpenProjects,
                tooltip: 'Projeleri aç',
                style: IconButton.styleFrom(
                  backgroundColor: Colors.white.withValues(alpha: 0.15),
                  foregroundColor: Colors.white,
                  minimumSize: const Size(52, 52),
                ),
                icon: const Icon(Icons.arrow_forward_rounded),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _NoProjectsCard extends StatelessWidget {
  const _NoProjectsCard({required this.onCreate});

  final VoidCallback onCreate;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Row(
          children: [
            const IconBadge(icon: Icons.rocket_launch_outlined),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'İlk projeni oluştur',
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                  const SizedBox(height: AppSpacing.xxs),
                  Text(
                    'Ekibini ve takvimini tek yerde yönetmeye başla.',
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: scheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
            IconButton(
              onPressed: onCreate,
              tooltip: 'Proje oluştur',
              icon: const Icon(Icons.add_rounded),
            ),
          ],
        ),
      ),
    );
  }
}
