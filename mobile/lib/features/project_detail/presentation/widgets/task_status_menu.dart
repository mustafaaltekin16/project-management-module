import 'package:flutter/material.dart';

Future<String?> showTaskStatusMenu(BuildContext context, String currentStatus) {
  const statuses = {'Todo': 'Yapılacak', 'InProgress': 'Devam Ediyor', 'Done': 'Tamamlandı'};
  return showModalBottomSheet<String>(
    context: context,
    builder: (context) => SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: statuses.entries
            .map((e) => ListTile(
                  leading: Icon(e.key == currentStatus ? Icons.radio_button_checked : Icons.radio_button_unchecked),
                  title: Text(e.value),
                  onTap: () => Navigator.of(context).pop(e.key),
                ))
            .toList(),
      ),
    ),
  );
}
