import 'package:flutter/material.dart';

import '../../domain/entities/feasibility_main_group.dart';
import 'add_item_dialog.dart';
import 'feasibility_item_tile.dart';

class FeasibilityGroupCard extends StatelessWidget {
  const FeasibilityGroupCard({
    super.key,
    required this.group,
    required this.currentUserName,
    required this.onAddItem,
    required this.onSubmitItem,
    required this.onDecideItem,
  });

  final FeasibilityMainGroup group;
  final String currentUserName;
  final void Function(String unit, String description, double amount, String currency) onAddItem;
  final void Function(String itemId, List<String> approverNamesInOrder) onSubmitItem;
  final void Function(String itemId, String approverName, bool approve, String? comment) onDecideItem;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(group.name, style: Theme.of(context).textTheme.titleMedium)),
                IconButton(
                  icon: const Icon(Icons.add),
                  onPressed: () async {
                    final result = await showAddItemDialog(context);
                    if (result != null) {
                      onAddItem(result.unit, result.description, result.amount, result.currency);
                    }
                  },
                ),
              ],
            ),
            Text(
              'İstenen: ${group.totalRequestedAmount.toStringAsFixed(2)} • Onaylanan: ${group.totalApprovedAmount.toStringAsFixed(2)}',
              style: const TextStyle(color: Colors.grey, fontSize: 12),
            ),
            const SizedBox(height: 8),
            if (group.items.isEmpty) const Text('Henüz kalem eklenmemiş.', style: TextStyle(color: Colors.grey)),
            ...group.items.map((item) => FeasibilityItemTile(
                  item: item,
                  currentUserName: currentUserName,
                  onSubmit: (names) => onSubmitItem(item.id, names),
                  onDecide: (approverName, approve, comment) => onDecideItem(item.id, approverName, approve, comment),
                )),
          ],
        ),
      ),
    );
  }
}
