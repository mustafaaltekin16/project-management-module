import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../controllers/project_create_controller.dart';

class AttachmentPicker extends ConsumerWidget {
  const AttachmentPicker({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final formState = ref.watch(projectCreateControllerProvider);
    final controller = ref.read(projectCreateControllerProvider.notifier);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (var i = 0; i < formState.attachments.length; i++)
          ListTile(
            dense: true,
            leading: const Icon(Icons.attach_file),
            title: Text(formState.attachments[i].fileName),
            trailing: IconButton(
              icon: const Icon(Icons.close),
              onPressed: () => controller.removeAttachment(i),
            ),
          ),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            onPressed: () async {
              final picked = await openFile();
              if (picked != null) {
                controller.addAttachment(picked.path, picked.name);
              }
            },
            icon: const Icon(Icons.add),
            label: const Text('Dosya ekle'),
          ),
        ),
      ],
    );
  }
}
