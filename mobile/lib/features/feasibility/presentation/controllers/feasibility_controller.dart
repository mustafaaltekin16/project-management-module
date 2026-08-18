import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/dio_client_provider.dart';
import '../../data/feasibility_api_service.dart';
import '../../domain/entities/feasibility_main_group.dart';

final feasibilityApiServiceProvider = Provider((ref) => FeasibilityApiService(ref.watch(dioProvider)));

class FeasibilityController extends FamilyAsyncNotifier<List<FeasibilityMainGroup>, String> {
  @override
  Future<List<FeasibilityMainGroup>> build(String arg) {
    return ref.read(feasibilityApiServiceProvider).getGroups(arg);
  }

  Future<void> refresh(String projectId) async {
    state = await AsyncValue.guard(() => ref.read(feasibilityApiServiceProvider).getGroups(projectId));
  }

  Future<void> createGroup(String projectId, String name) async {
    await ref.read(feasibilityApiServiceProvider).createGroup(projectId, name);
    await refresh(projectId);
  }

  Future<void> addItem(
    String projectId,
    String mainGroupId, {
    required String unit,
    required String description,
    required double amount,
    required String currency,
  }) async {
    await ref.read(feasibilityApiServiceProvider).addItem(
          mainGroupId,
          unit: unit,
          description: description,
          amount: amount,
          currency: currency,
        );
    await refresh(projectId);
  }

  Future<void> submitForApproval(
    String projectId,
    String mainGroupId,
    String itemId,
    List<String> approverNamesInOrder,
  ) async {
    await ref.read(feasibilityApiServiceProvider).submitForApproval(mainGroupId, itemId, approverNamesInOrder);
    await refresh(projectId);
  }

  Future<void> decide(
    String projectId,
    String mainGroupId,
    String itemId, {
    required String approverName,
    required bool approve,
    String? comment,
  }) async {
    await ref.read(feasibilityApiServiceProvider).decide(
          mainGroupId,
          itemId,
          approverName: approverName,
          approve: approve,
          comment: comment,
        );
    await refresh(projectId);
  }
}

final feasibilityControllerProvider =
    AsyncNotifierProvider.family<FeasibilityController, List<FeasibilityMainGroup>, String>(
  FeasibilityController.new,
);
