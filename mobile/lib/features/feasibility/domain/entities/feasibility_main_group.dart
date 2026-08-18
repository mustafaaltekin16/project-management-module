import 'feasibility_item.dart';

class FeasibilityMainGroup {
  FeasibilityMainGroup({
    required this.id,
    required this.projectId,
    required this.name,
    required this.totalRequestedAmount,
    required this.totalApprovedAmount,
    required this.items,
  });

  final String id;
  final String projectId;
  final String name;
  final double totalRequestedAmount;
  final double totalApprovedAmount;
  final List<FeasibilityItem> items;

  factory FeasibilityMainGroup.fromJson(Map<String, dynamic> json) {
    return FeasibilityMainGroup(
      id: json['id'].toString(),
      projectId: json['projectId'].toString(),
      name: json['name'] as String? ?? '',
      totalRequestedAmount: (json['totalRequestedAmount'] as num?)?.toDouble() ?? 0,
      totalApprovedAmount: (json['totalApprovedAmount'] as num?)?.toDouble() ?? 0,
      items: (json['items'] as List? ?? []).map((e) => FeasibilityItem.fromJson(e as Map<String, dynamic>)).toList(),
    );
  }
}
