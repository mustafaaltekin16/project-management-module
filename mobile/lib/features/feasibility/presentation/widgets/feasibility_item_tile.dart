import 'package:flutter/material.dart';

import '../../domain/entities/feasibility_item.dart';
import 'decide_dialog.dart';
import 'submit_approval_dialog.dart';

class FeasibilityItemTile extends StatelessWidget {
  const FeasibilityItemTile({
    super.key,
    required this.item,
    required this.currentUserName,
    required this.onSubmit,
    required this.onDecide,
  });

  final FeasibilityItem item;
  final String currentUserName;
  final void Function(List<String> approverNamesInOrder) onSubmit;
  final void Function(String approverName, bool approve, String? comment) onDecide;

  Color _statusColor(BuildContext context) {
    switch (item.status) {
      case 'Approved':
        return Colors.green;
      case 'Rejected':
        return Colors.red;
      case 'PendingApproval':
        return Colors.orange;
      default:
        return Colors.grey;
    }
  }

  String _statusLabel() {
    switch (item.status) {
      case 'Approved':
        return 'Onaylandı';
      case 'Rejected':
        return 'Reddedildi';
      case 'PendingApproval':
        return 'Onay Bekliyor';
      default:
        return 'Taslak';
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _statusColor(context);
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(10),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(item.description, style: const TextStyle(fontWeight: FontWeight.w600))),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(color: color.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(12)),
                  child: Text(_statusLabel(), style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600)),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text('${item.unit} • ${item.amount.toStringAsFixed(2)} ${item.currency}',
                style: const TextStyle(color: Colors.grey)),
            if (item.steps.isNotEmpty) ...[
              const SizedBox(height: 8),
              ...item.steps.map((s) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 2),
                    child: Row(
                      children: [
                        Icon(
                          switch (s.decision) {
                            'Approved' => Icons.check_circle,
                            'Rejected' => Icons.cancel,
                            _ => Icons.radio_button_unchecked,
                          },
                          size: 16,
                          color: switch (s.decision) {
                            'Approved' => Colors.green,
                            'Rejected' => Colors.red,
                            _ => Colors.grey,
                          },
                        ),
                        const SizedBox(width: 6),
                        Text('${s.order}. ${s.approverName}'),
                        if (s.comment != null && s.comment!.isNotEmpty) ...[
                          const SizedBox(width: 6),
                          Expanded(child: Text('"${s.comment}"', style: const TextStyle(fontStyle: FontStyle.italic, fontSize: 12))),
                        ],
                      ],
                    ),
                  )),
            ],
            const SizedBox(height: 8),
            if (item.status == 'Draft')
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: () async {
                    final names = await showSubmitApprovalDialog(context);
                    if (names != null && names.isNotEmpty) onSubmit(names);
                  },
                  child: const Text('Onaya Gönder'),
                ),
              ),
            if (item.status == 'PendingApproval')
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: () async {
                    final result = await showDecideDialog(context, defaultApproverName: currentUserName);
                    if (result != null && result.approverName.isNotEmpty) {
                      onDecide(result.approverName, result.approve, result.comment);
                    }
                  },
                  child: const Text('Karar Ver'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
