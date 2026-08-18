class ProjectDocument {
  ProjectDocument({
    required this.id,
    required this.fileName,
    required this.uploadedByName,
    required this.uploadedAtUtc,
  });

  final String id;
  final String fileName;
  final String? uploadedByName;
  final DateTime? uploadedAtUtc;

  factory ProjectDocument.fromJson(Map<String, dynamic> json) {
    return ProjectDocument(
      id: json['id'].toString(),
      fileName: json['fileName'] as String? ?? json['name'] as String? ?? 'Dosya',
      uploadedByName: json['uploadedByName'] as String?,
      uploadedAtUtc: json['uploadedAtUtc'] == null ? null : DateTime.tryParse(json['uploadedAtUtc'] as String),
    );
  }
}
