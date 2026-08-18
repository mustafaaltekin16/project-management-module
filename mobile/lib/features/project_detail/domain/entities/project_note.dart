class ProjectNote {
  ProjectNote({required this.id, required this.content, required this.createdByName, required this.createdAtUtc});

  final String id;
  final String content;
  final String? createdByName;
  final DateTime? createdAtUtc;

  factory ProjectNote.fromJson(Map<String, dynamic> json) {
    return ProjectNote(
      id: json['id'].toString(),
      content: json['content'] as String? ?? '',
      createdByName: json['createdByName'] as String?,
      createdAtUtc: json['createdAtUtc'] == null ? null : DateTime.tryParse(json['createdAtUtc'] as String),
    );
  }
}
