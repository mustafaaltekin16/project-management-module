import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../projects/domain/entities/project_detail.dart';
import '../../../../core/theme/app_theme.dart';
import 'notes_section.dart';
import 'timeline_panel.dart';

class DescriptionTab extends StatelessWidget {
  const DescriptionTab({super.key, required this.detail});

  final ProjectDetail detail;

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('dd.MM.yyyy');
    final scheme = Theme.of(context).colorScheme;
    return ListView(
      padding: const EdgeInsets.only(top: AppSpacing.xs),
      children: [
        TimelinePanel(projectId: detail.id),
        Card(
          margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const IconBadge(icon: Icons.info_outline_rounded, size: 38),
                    const SizedBox(width: AppSpacing.sm),
                    Text(
                      'Proje bilgileri',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                  ],
                ),
                if (detail.description.isNotEmpty) ...[
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    detail.description,
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: scheme.onSurfaceVariant,
                    ),
                  ),
                ],
                const SizedBox(height: AppSpacing.md),
                _InfoRow(label: 'Yönetici', value: detail.managerName),
                if (detail.secondManagerName != null)
                  _InfoRow(
                    label: 'İkinci Yönetici',
                    value: detail.secondManagerName!,
                  ),
                _InfoRow(label: 'Birim', value: detail.unit),
                if (detail.budget != null)
                  _InfoRow(
                    label: 'Bütçe',
                    value:
                        '${detail.budget!.toStringAsFixed(0)} ${detail.currency ?? ''}',
                  ),
                _InfoRow(
                  label: 'Tarihler',
                  value:
                      '${detail.startDate != null ? dateFormat.format(detail.startDate!) : '?'} - ${detail.endDate != null ? dateFormat.format(detail.endDate!) : '?'}',
                ),
                if (detail.departments.isNotEmpty) ...[
                  const SizedBox(height: AppSpacing.md),
                  Divider(color: scheme.outlineVariant),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    'Departmanlar ve iş paketleri',
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  ...detail.departments.map(
                    (d) => ListTile(
                      contentPadding: EdgeInsets.zero,
                      dense: true,
                      leading: const Icon(Icons.apartment_outlined),
                      title: Text(d.title),
                      subtitle: Text('${d.departmentName} · ${d.managerName}'),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
        NotesSection(projectId: detail.id),
        const SizedBox(height: 24),
      ],
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          SizedBox(
            width: 112,
            child: Text(
              label,
              style: TextStyle(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
                fontSize: 13,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }
}
