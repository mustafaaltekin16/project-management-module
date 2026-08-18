import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../controllers/project_detail_controller.dart';

class TimelinePanel extends ConsumerWidget {
  const TimelinePanel({super.key, required this.projectId});

  final String projectId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final timelineAsync = ref.watch(projectTimelineProvider(projectId));

    return Card(
      margin: const EdgeInsets.all(12),
      child: ExpansionTile(
        title: const Text('Zaman Çizelgesi'),
        initiallyExpanded: false,
        children: [
          timelineAsync.when(
            loading: () => const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            ),
            error: (e, _) => const Padding(
              padding: EdgeInsets.all(16),
              child: Text('Zaman çizelgesi yüklenemedi.'),
            ),
            data: (packages) {
              if (packages.isEmpty) {
                return const Padding(
                  padding: EdgeInsets.all(16),
                  child: Text('İş paketi tanımlı değil.'),
                );
              }
              final dateFormat = DateFormat('dd.MM.yyyy');
              return Column(
                children: packages
                    .map(
                      (wp) => ListTile(
                        leading: Icon(
                          Icons.circle,
                          size: 14,
                          color: switch (wp.state) {
                            'Completed' => Colors.green,
                            'Blocked' => Colors.red,
                            'Active' => Colors.blue,
                            _ => Colors.grey,
                          },
                        ),
                        title: Text(wp.title),
                        subtitle: Text(
                          '${wp.startDate != null ? dateFormat.format(wp.startDate!) : '?'} - '
                          '${wp.endDate != null ? dateFormat.format(wp.endDate!) : '?'}',
                        ),
                        trailing: wp.deviationDays == 0
                            ? null
                            : Text(
                                '${wp.deviationDays > 0 ? '+' : ''}${wp.deviationDays} gün',
                                style: TextStyle(color: wp.deviationDays > 0 ? Colors.red : Colors.green),
                              ),
                      ),
                    )
                    .toList(),
              );
            },
          ),
        ],
      ),
    );
  }
}
