import 'package:flutter/material.dart';

class DecideResult {
  DecideResult({required this.approverName, required this.approve, this.comment});

  final String approverName;
  final bool approve;
  final String? comment;
}

Future<DecideResult?> showDecideDialog(BuildContext context, {required String defaultApproverName}) {
  final nameController = TextEditingController(text: defaultApproverName);
  final commentController = TextEditingController();

  return showDialog<DecideResult>(
    context: context,
    builder: (context) => AlertDialog(
      title: const Text('Onay Kararı'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(controller: nameController, decoration: const InputDecoration(labelText: 'Onaycı Adı')),
          const SizedBox(height: 8),
          TextField(
            controller: commentController,
            decoration: const InputDecoration(labelText: 'Yorum (opsiyonel)'),
            maxLines: 2,
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(
            DecideResult(approverName: nameController.text.trim(), approve: false, comment: commentController.text.trim()),
          ),
          child: const Text('Reddet'),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(
            DecideResult(approverName: nameController.text.trim(), approve: true, comment: commentController.text.trim()),
          ),
          child: const Text('Onayla'),
        ),
      ],
    ),
  );
}
