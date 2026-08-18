import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/activity_event.dart';
import 'documents_controller.dart';
import 'notes_controller.dart';
import 'tasks_controller.dart';

final activityFeedProvider = Provider.family<AsyncValue<List<ActivityEvent>>, String>((ref, projectId) {
  final notes = ref.watch(notesControllerProvider(projectId));
  final tasks = ref.watch(tasksControllerProvider(projectId));
  final documents = ref.watch(documentsControllerProvider(projectId));

  if (notes.isLoading || tasks.isLoading || documents.isLoading) {
    return const AsyncLoading();
  }
  if (notes.hasError) return AsyncError(notes.error!, notes.stackTrace!);
  if (tasks.hasError) return AsyncError(tasks.error!, tasks.stackTrace!);
  if (documents.hasError) return AsyncError(documents.error!, documents.stackTrace!);

  final events = <ActivityEvent>[];
  for (final n in notes.value ?? []) {
    events.add(ActivityEvent(
      type: ActivityEventType.note,
      title: 'Not eklendi',
      subtitle: n.content,
      timestamp: n.createdAtUtc ?? DateTime.fromMillisecondsSinceEpoch(0),
    ));
  }
  for (final g in tasks.value ?? []) {
    for (final t in g.tasks.expand((t) => t.flatten())) {
      events.add(ActivityEvent(
        type: ActivityEventType.task,
        title: 'Görev: ${t.title}',
        subtitle: t.status,
        timestamp: t.startDate ?? DateTime.fromMillisecondsSinceEpoch(0),
      ));
    }
  }
  for (final d in documents.value ?? []) {
    events.add(ActivityEvent(
      type: ActivityEventType.document,
      title: 'Doküman yüklendi',
      subtitle: d.fileName,
      timestamp: d.uploadedAtUtc ?? DateTime.fromMillisecondsSinceEpoch(0),
    ));
  }
  events.sort((a, b) => b.timestamp.compareTo(a.timestamp));
  return AsyncData(events);
});
