import 'package:flutter/material.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../projects/domain/entities/project.dart';
import '../../../projects/presentation/widgets/project_status_badge.dart';

class MiniProjectTile extends StatelessWidget {
  const MiniProjectTile({
    super.key,
    required this.project,
    required this.onTap,
    this.showDivider = true,
  });

  final Project project;
  final VoidCallback onTap;
  final bool showDivider;

  Color get _typeColor {
    switch (project.type) {
      case ProjectType.multiUnit:
        return ProjectTypeColors.multiUnit;
      case ProjectType.feasibilityBased:
        return ProjectTypeColors.feasibilityBased;
      case ProjectType.simple:
        return ProjectTypeColors.simple;
    }
  }

  IconData get _typeIcon {
    switch (project.type) {
      case ProjectType.multiUnit:
        return Icons.account_tree_outlined;
      case ProjectType.feasibilityBased:
        return Icons.analytics_outlined;
      case ProjectType.simple:
        return Icons.folder_outlined;
    }
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Column(
      children: [
        InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(AppRadius.md),
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
            child: Row(
              children: [
                IconBadge(icon: _typeIcon, size: 42, color: _typeColor),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        project.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: AppSpacing.xxs),
                      Text(
                        '${project.managerName} · %${project.progressPercent.toStringAsFixed(0)}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: scheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: AppSpacing.xs),
                ProjectStatusBadge(status: project.status),
                const SizedBox(width: AppSpacing.xxs),
                Icon(
                  Icons.chevron_right_rounded,
                  color: scheme.onSurfaceVariant,
                ),
              ],
            ),
          ),
        ),
        if (showDivider)
          Divider(height: 1, indent: 54, color: scheme.outlineVariant),
      ],
    );
  }
}
