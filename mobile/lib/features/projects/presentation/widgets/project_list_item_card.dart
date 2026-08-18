import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/app_theme.dart';
import '../../domain/entities/project.dart';
import 'project_status_badge.dart';

class ProjectListItemCard extends StatelessWidget {
  const ProjectListItemCard({
    super.key,
    required this.project,
    required this.onTap,
    this.action,
  });

  final Project project;
  final VoidCallback onTap;
  final Widget? action;

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

  String get _typeLabel {
    switch (project.type) {
      case ProjectType.multiUnit:
        return 'Çoklu birim';
      case ProjectType.feasibilityBased:
        return 'Fizibilite';
      case ProjectType.simple:
        return 'Standart';
    }
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final dateFormat = DateFormat('dd.MM.yyyy');
    final dateText = project.endDate == null
        ? null
        : dateFormat.format(project.endDate!);
    final isOverdue = project.deviationDays > 0;

    return Card(
      margin: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppRadius.lg),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  IconBadge(icon: _typeIcon, size: 44, color: _typeColor),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          project.name,
                          style: Theme.of(context).textTheme.titleMedium,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: AppSpacing.xxs),
                        Text(
                          '${project.managerName} · ${project.unit}',
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: scheme.onSurfaceVariant),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: AppSpacing.xs),
                  if (action != null)
                    action!
                  else
                    const Icon(Icons.chevron_right_rounded),
                ],
              ),
              const SizedBox(height: AppSpacing.md),
              Wrap(
                spacing: AppSpacing.xs,
                runSpacing: AppSpacing.xs,
                children: [
                  ProjectStatusBadge(status: project.status),
                  _MetaPill(icon: Icons.layers_outlined, label: _typeLabel),
                  if (dateText != null)
                    _MetaPill(
                      icon: isOverdue
                          ? Icons.warning_amber_rounded
                          : Icons.event_outlined,
                      label: isOverdue
                          ? '${project.deviationDays} gün gecikme'
                          : dateText,
                      color: isOverdue ? scheme.error : null,
                    ),
                ],
              ),
              const SizedBox(height: AppSpacing.md),
              Row(
                children: [
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(99),
                      child: LinearProgressIndicator(
                        value: (project.progressPercent / 100).clamp(0, 1),
                        minHeight: 7,
                        backgroundColor: scheme.surfaceContainer,
                        color: _typeColor,
                      ),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Text(
                    '%${project.progressPercent.toStringAsFixed(0)}',
                    style: Theme.of(context).textTheme.labelMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _MetaPill extends StatelessWidget {
  const _MetaPill({required this.icon, required this.label, this.color});

  final IconData icon;
  final String label;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final foreground = color ?? scheme.onSurfaceVariant;
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.xs,
        vertical: 5,
      ),
      decoration: BoxDecoration(
        color: color?.withValues(alpha: 0.08) ?? scheme.surfaceContainer,
        borderRadius: BorderRadius.circular(AppRadius.sm),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 13, color: foreground),
          const SizedBox(width: 5),
          Text(
            label,
            style: TextStyle(
              color: foreground,
              fontSize: 11,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
