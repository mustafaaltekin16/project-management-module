import 'package:flutter/material.dart';

class AddItemResult {
  AddItemResult({required this.unit, required this.description, required this.amount, required this.currency});

  final String unit;
  final String description;
  final double amount;
  final String currency;
}

Future<AddItemResult?> showAddItemDialog(BuildContext context) {
  final unitController = TextEditingController();
  final descriptionController = TextEditingController();
  final amountController = TextEditingController();
  String currency = 'TRY';

  return showDialog<AddItemResult>(
    context: context,
    builder: (context) => StatefulBuilder(
      builder: (context, setState) => AlertDialog(
        title: const Text('Fizibilite Kalemi Ekle'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(controller: unitController, decoration: const InputDecoration(labelText: 'Birim')),
            const SizedBox(height: 8),
            TextField(controller: descriptionController, decoration: const InputDecoration(labelText: 'Açıklama')),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: amountController,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(labelText: 'Tutar'),
                  ),
                ),
                const SizedBox(width: 8),
                DropdownButton<String>(
                  value: currency,
                  items: const [
                    DropdownMenuItem(value: 'TRY', child: Text('₺')),
                    DropdownMenuItem(value: 'USD', child: Text('\$')),
                    DropdownMenuItem(value: 'EUR', child: Text('€')),
                  ],
                  onChanged: (v) => setState(() => currency = v ?? 'TRY'),
                ),
              ],
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('İptal')),
          FilledButton(
            onPressed: () {
              final amount = double.tryParse(amountController.text) ?? 0;
              if (unitController.text.trim().isEmpty || descriptionController.text.trim().isEmpty || amount <= 0) {
                return;
              }
              Navigator.of(context).pop(AddItemResult(
                unit: unitController.text.trim(),
                description: descriptionController.text.trim(),
                amount: amount,
                currency: currency,
              ));
            },
            child: const Text('Ekle'),
          ),
        ],
      ),
    ),
  );
}
