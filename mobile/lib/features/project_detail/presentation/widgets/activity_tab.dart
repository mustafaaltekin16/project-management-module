import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_indicator.dart';
import '../controllers/activity_controller.dart';
import 'activity_event_tile.dart';

class ActivityTab extends ConsumerWidget {
  const ActivityTab({super.key, required this.projectId});

  final String projectId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final activityAsync = ref.watch(activityFeedProvider(projectId));

    return activityAsync.when(
      loading: () => const LoadingIndicator(),
      error: (e, _) => const ErrorView(message: 'Akış yüklenemedi.'),
      data: (events) {
        if (events.isEmpty) {
          return const EmptyState(message: 'Henüz bir hareket yok.', icon: Icons.timeline);
        }
        return ListView.builder(
          itemCount: events.length,
          itemBuilder: (context, index) => ActivityEventTile(event: events[index]),
        );
      },
    );
  }
}
