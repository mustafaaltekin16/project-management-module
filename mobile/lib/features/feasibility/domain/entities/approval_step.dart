class ApprovalStep {
  ApprovalStep({
    required this.id,
    required this.approverName,
    required this.order,
    required this.decision,
    required this.comment,
    required this.decidedAtUtc,
  });

  final String id;
  final String approverName;
  final int order;
  final String decision; // Pending, Approved, Rejected
  final String? comment;
  final DateTime? decidedAtUtc;

  factory ApprovalStep.fromJson(Map<String, dynamic> json) {
    return ApprovalStep(
      id: json['id'].toString(),
      approverName: json['approverName'] as String? ?? '',
      order: (json['order'] as num?)?.toInt() ?? 0,
      decision: json['decision'] as String? ?? 'Pending',
      comment: json['comment'] as String?,
      decidedAtUtc: json['decidedAtUtc'] == null ? null : DateTime.tryParse(json['decidedAtUtc'] as String),
    );
  }
}
