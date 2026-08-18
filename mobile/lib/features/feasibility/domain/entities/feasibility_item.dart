import 'approval_step.dart';

class FeasibilityItem {
  FeasibilityItem({
    required this.id,
    required this.unit,
    required this.description,
    required this.amount,
    required this.currency,
    required this.status,
    required this.steps,
  });

  final String id;
  final String unit;
  final String description;
  final double amount;
  final String currency;
  final String status; // Draft, PendingApproval, Approved, Rejected
  final List<ApprovalStep> steps;

  factory FeasibilityItem.fromJson(Map<String, dynamic> json) {
    return FeasibilityItem(
      id: json['id'].toString(),
      unit: json['unit'] as String? ?? '',
      description: json['description'] as String? ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
      currency: json['currency'] as String? ?? 'TRY',
      status: json['status'] as String? ?? 'Draft',
      steps: (json['steps'] as List? ?? []).map((e) => ApprovalStep.fromJson(e as Map<String, dynamic>)).toList(),
    );
  }
}
