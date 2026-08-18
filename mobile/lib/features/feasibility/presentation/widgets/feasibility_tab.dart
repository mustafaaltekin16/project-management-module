import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_indicator.dart';
import '../../../login/presentation/controllers/auth_controller.dart';
import '../controllers/feasibility_controller.dart';
import 'feasibility_group_card.dart';

class FeasibilityTab extends ConsumerWidget {
  const FeasibilityTab({super.key, required this.projectId});

  final String projectId;

  Future<void> _showAddGroupDialog(BuildContext context, WidgetRef ref) async {
    final controller = TextEditingController();
    final name = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Fizibilite Grubu Ekle'),
        content: TextField(controller: controller, decoration: const InputDecoration(labelText: 'Grup Adı')),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('İptal')),
          FilledButton(onPressed: () => Navigator.of(context).pop(controller.text.trim()), child: const Text('Ekle')),
        ],
      ),
    );
    if (name != null && name.isNotEmpty) {
      await ref.read(feasibilityControllerProvider(projectId).notifier).createGroup(projectId, name);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final groupsAsync = ref.watch(feasibilityControllerProvider(projectId));
    final currentUserName = ref.watch(authControllerProvider).valueOrNull?.displayName ?? '';
    final controller = ref.read(feasibilityControllerProvider(projectId).notifier);

    return Scaffold(
      body: groupsAsync.when(
        loading: () => const LoadingIndicator(),
        error: (e, _) => ErrorView(
          message: 'Fizibilite verileri yüklenemedi.',
          onRetry: () => controller.refresh(projectId),
        ),
        data: (groups) {
          if (groups.isEmpty) {
            return const EmptyState(message: 'Fizibilite grubu tanımlı değil.', icon: Icons.calculate_outlined);
          }
          return ListView(
            children: groups
                .map((g) => FeasibilityGroupCard(
                      group: g,
                      currentUserName: currentUserName,
                      onAddItem: (unit, description, amount, currency) => controller.addItem(
                        projectId,
                        g.id,
                        unit: unit,
                        description: description,
                        amount: amount,
                        currency: currency,
                      ),
                      onSubmitItem: (itemId, names) => controller.submitForApproval(projectId, g.id, itemId, names),
                      onDecideItem: (itemId, approverName, approve, comment) => controller.decide(
                        projectId,
                        g.id,
                        itemId,
                        approverName: approverName,
                        approve: approve,
                        comment: comment,
                      ),
                    ))
                .toList(),
          );
        },
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showAddGroupDialog(context, ref),
        child: const Icon(Icons.add),
      ),
    );
  }
}
