import 'package:flutter/material.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../projects/domain/entities/project.dart';

class ModeSelector extends StatelessWidget {
  const ModeSelector({super.key, required this.value, required this.onChanged});

  final ProjectType value;
  final void Function(ProjectType) onChanged;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        _ModeOption(
          icon: Icons.folder_outlined,
          title: 'Standart proje',
          description: 'Tek ekip veya departman için yalın proje akışı',
          color: ProjectTypeColors.simple,
          selected: value == ProjectType.simple,
          onTap: () => onChanged(ProjectType.simple),
        ),
        const SizedBox(height: AppSpacing.xs),
        _ModeOption(
          icon: Icons.account_tree_outlined,
          title: 'Çoklu birim',
          description: 'Birden fazla departman ve iş paketi içeren projeler',
          color: ProjectTypeColors.multiUnit,
          selected: value == ProjectType.multiUnit,
          onTap: () => onChanged(ProjectType.multiUnit),
        ),
      ],
    );
  }
}

class _ModeOption extends StatelessWidget {
  const _ModeOption({
    required this.icon,
    required this.title,
    required this.description,
    required this.color,
    required this.selected,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String description;
  final Color color;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      button: true,
      selected: selected,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppRadius.md),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          padding: const EdgeInsets.all(AppSpacing.sm),
          decoration: BoxDecoration(
            color: selected
                ? color.withValues(alpha: 0.08)
                : scheme.surfaceContainerLowest,
            borderRadius: BorderRadius.circular(AppRadius.md),
            border: Border.all(
              color: selected ? color : scheme.outlineVariant,
              width: selected ? 1.5 : 1,
            ),
          ),
          child: Row(
            children: [
              IconBadge(icon: icon, size: 42, color: color),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xxs),
                    Text(
                      description,
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: scheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: AppSpacing.xs),
              Icon(
                selected ? Icons.check_circle_rounded : Icons.circle_outlined,
                color: selected ? color : scheme.outline,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
