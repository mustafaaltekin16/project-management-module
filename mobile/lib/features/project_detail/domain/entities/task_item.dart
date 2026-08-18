class TaskItem {
  TaskItem({
    required this.id,
    required this.title,
    required this.status,
    required this.assigneeName,
    required this.startDate,
    required this.dueDate,
    required this.subTasks,
    this.depth = 0,
  });

  final String id;
  final String title;
  final String status; // Todo, InProgress, Done
  final String? assigneeName;
  final DateTime? startDate;
  final DateTime? dueDate;
  final List<TaskItem> subTasks;
  final int depth;

  factory TaskItem.fromJson(Map<String, dynamic> json, {int depth = 0}) {
    final children = (json['subTasks'] as List? ?? json['children'] as List? ?? [])
        .map((e) => TaskItem.fromJson(e as Map<String, dynamic>, depth: depth + 1))
        .toList();
    return TaskItem(
      id: json['id'].toString(),
      title: json['title'] as String? ?? '',
      status: json['status'] as String? ?? 'Todo',
      assigneeName: json['assigneeName'] as String?,
      startDate: json['startDateUtc'] == null ? null : DateTime.tryParse(json['startDateUtc'] as String),
      dueDate: json['dueDateUtc'] == null ? null : DateTime.tryParse(json['dueDateUtc'] as String),
      subTasks: children,
      depth: depth,
    );
  }

  List<TaskItem> flatten() => [this, ...subTasks.expand((t) => t.flatten())];
}

class TaskGroup {
  TaskGroup({required this.id, required this.title, required this.tasks});

  final String id;
  final String title;
  final List<TaskItem> tasks;

  factory TaskGroup.fromJson(Map<String, dynamic> json) {
    return TaskGroup(
      id: json['id'].toString(),
      title: json['title'] as String? ?? '',
      tasks: (json['tasks'] as List? ?? []).map((e) => TaskItem.fromJson(e as Map<String, dynamic>)).toList(),
    );
  }
}
