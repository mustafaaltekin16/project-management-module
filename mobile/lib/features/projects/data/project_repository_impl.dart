import '../domain/entities/board_column.dart';
import '../domain/entities/department.dart';
import '../domain/entities/employee.dart';
import '../domain/entities/gantt_task_summary.dart';
import '../domain/entities/project.dart';
import '../domain/entities/project_detail.dart';
import '../domain/entities/timeline_work_package.dart';
import '../domain/repositories/project_repository.dart';
import 'department_api_service.dart';
import 'dto/create_project_request_dto.dart';
import 'employee_api_service.dart';
import 'project_api_service.dart';
import 'project_board_api_service.dart';

class ProjectRepositoryImpl implements ProjectRepository {
  ProjectRepositoryImpl({
    required ProjectApiService projectApi,
    required ProjectBoardApiService boardApi,
    required EmployeeApiService employeeApi,
    required DepartmentApiService departmentApi,
  })  : _projectApi = projectApi,
        _boardApi = boardApi,
        _employeeApi = employeeApi,
        _departmentApi = departmentApi;

  final ProjectApiService _projectApi;
  final ProjectBoardApiService _boardApi;
  final EmployeeApiService _employeeApi;
  final DepartmentApiService _departmentApi;

  @override
  Future<List<Project>> search({String? type, String? q}) async {
    final dtos = await _projectApi.search(type: type, q: q);
    return dtos.map((d) => d.toEntity()).toList();
  }

  @override
  Future<ProjectDetail> getById(String id) async {
    final dto = await _projectApi.getById(id);
    return dto.detail;
  }

  @override
  Future<List<TimelineWorkPackage>> getTimeline(String id) async {
    final dto = await _projectApi.getTimeline(id);
    return dto.workPackages;
  }

  @override
  Future<void> delete(String id) => _projectApi.delete(id);

  @override
  Future<String> create(CreateProjectRequestDto request) => _projectApi.create(request);

  @override
  Future<void> uploadDocument(String projectId, String filePath, String fileName) =>
      _projectApi.uploadDocument(projectId, filePath, fileName);

  @override
  Future<List<GanttTaskSummary>> getTaskGroupsForGantt(String projectId) =>
      _projectApi.getTaskGroupsForGantt(projectId);

  @override
  Future<List<BoardColumn>> getBoardColumns() async {
    final dtos = await _boardApi.getColumns();
    return dtos.map((d) => d.column).toList();
  }

  @override
  Future<void> movePlacement(String projectId, String targetColumnId, String expectedUpdatedAtUtc) =>
      _boardApi.movePlacement(projectId, targetColumnId, expectedUpdatedAtUtc);

  @override
  Future<List<Employee>> listEmployees() async {
    final dtos = await _employeeApi.list();
    return dtos.map((d) => d.employee).toList();
  }

  @override
  Future<List<Department>> listDepartments() async {
    final dtos = await _departmentApi.list();
    return dtos.map((d) => d.department).toList();
  }
}
