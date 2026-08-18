import '../../domain/entities/board_column.dart';

class BoardColumnDto {
  BoardColumnDto({required this.column});

  final BoardColumn column;

  factory BoardColumnDto.fromJson(Map<String, dynamic> json) {
    return BoardColumnDto(
      column: BoardColumn(
        id: json['id'].toString(),
        name: json['name'] as String? ?? '',
        sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
        color: json['color'] as String? ?? '#697386',
      ),
    );
  }
}
