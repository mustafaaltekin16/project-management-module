import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../domain/entities/project_document.dart';

class DocumentTile extends StatelessWidget {
  const DocumentTile({super.key, required this.document, required this.onDelete});

  final ProjectDocument document;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('dd.MM.yyyy HH:mm');
    return ListTile(
      leading: const Icon(Icons.insert_drive_file_outlined),
      title: Text(document.fileName),
      subtitle: Text(
        '${document.uploadedByName ?? ''} ${document.uploadedAtUtc != null ? '• ${dateFormat.format(document.uploadedAtUtc!)}' : ''}',
      ),
      trailing: IconButton(icon: const Icon(Icons.delete_outline), onPressed: onDelete),
    );
  }
}
