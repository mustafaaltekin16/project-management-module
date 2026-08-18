import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../controllers/notes_controller.dart';

class NotesSection extends ConsumerStatefulWidget {
  const NotesSection({super.key, required this.projectId});

  final String projectId;

  @override
  ConsumerState<NotesSection> createState() => _NotesSectionState();
}

class _NotesSectionState extends ConsumerState<NotesSection> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final notesAsync = ref.watch(notesControllerProvider(widget.projectId));
    final dateFormat = DateFormat('dd.MM.yyyy HH:mm');

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Notlar', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            notesAsync.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => const Text('Notlar yüklenemedi.'),
              data: (notes) {
                if (notes.isEmpty) {
                  return const Text('Henüz not eklenmemiş.', style: TextStyle(color: Colors.grey));
                }
                return Column(
                  children: notes
                      .map((n) => Padding(
                            padding: const EdgeInsets.symmetric(vertical: 4),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(n.content),
                                Text(
                                  '${n.createdByName ?? ''} • ${n.createdAtUtc != null ? dateFormat.format(n.createdAtUtc!) : ''}',
                                  style: const TextStyle(fontSize: 11, color: Colors.grey),
                                ),
                              ],
                            ),
                          ))
                      .toList(),
                );
              },
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _controller,
                    decoration: const InputDecoration(hintText: 'Not ekle...', isDense: true),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.send),
                  onPressed: () {
                    if (_controller.text.trim().isEmpty) return;
                    ref.read(notesControllerProvider(widget.projectId).notifier).add(widget.projectId, _controller.text.trim());
                    _controller.clear();
                  },
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
