import 'package:flutter/material.dart';

Future<List<String>?> showSubmitApprovalDialog(BuildContext context) {
  final rows = <TextEditingController>[TextEditingController()];

  return showDialog<List<String>>(
    context: context,
    builder: (context) => StatefulBuilder(
      builder: (context, setState) => AlertDialog(
        title: const Text('Onaya Gönder'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text('Onaycıları sırayla ekleyin:'),
              const SizedBox(height: 8),
              for (var i = 0; i < rows.length; i++)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    children: [
                      Text('${i + 1}.'),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: rows[i],
                          decoration: const InputDecoration(hintText: 'Onaycı adı', isDense: true),
                        ),
                      ),
                      if (rows.length > 1)
                        IconButton(
                          icon: const Icon(Icons.remove_circle_outline),
                          onPressed: () => setState(() => rows.removeAt(i)),
                        ),
                    ],
                  ),
                ),
              TextButton.icon(
                onPressed: () => setState(() => rows.add(TextEditingController())),
                icon: const Icon(Icons.add),
                label: const Text('Onaycı ekle'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('İptal')),
          FilledButton(
            onPressed: () {
              final names = rows.map((c) => c.text.trim()).where((n) => n.isNotEmpty).toList();
              Navigator.of(context).pop(names);
            },
            child: const Text('Gönder'),
          ),
        ],
      ),
    ),
  );
}
