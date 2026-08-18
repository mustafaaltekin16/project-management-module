import '../entities/board_column.dart';
import '../entities/department.dart';
import '../entities/employee.dart';
import '../entities/gantt_task_summary.dart';
import '../entities/project.dart';
import '../entities/project_detail.dart';
import '../entities/timeline_work_package.dart';
import '../../data/dto/create_project_request_dto.dart';

abstract class ProjectRepository {
  Future<List<Project>> search({String? type, String? q});
  Future<ProjectDetail> getById(String id);
  Future<List<TimelineWorkPackage>> getTimeline(String id);
  Future<void> delete(String id);
  Future<String> create(CreateProjectRequestDto request);
  Future<void> uploadDocument(String projectId, String filePath, String fileName);
  Future<List<GanttTaskSummary>> getTaskGroupsForGantt(String projectId);

  Future<List<BoardColumn>> getBoardColumns();
  Future<void> movePlacement(String projectId, String targetColumnId, String expectedUpdatedAtUtc);

  Future<List<Employee>> listEmployees();
  Future<List<Department>> listDepartments();
}
