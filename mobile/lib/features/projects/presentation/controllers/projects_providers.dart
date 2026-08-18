import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/dio_client_provider.dart';
import '../../data/department_api_service.dart';
import '../../data/employee_api_service.dart';
import '../../data/project_api_service.dart';
import '../../data/project_board_api_service.dart';
import '../../data/project_repository_impl.dart';
import '../../domain/repositories/project_repository.dart';

final projectApiServiceProvider = Provider((ref) => ProjectApiService(ref.watch(dioProvider)));
final projectBoardApiServiceProvider = Provider((ref) => ProjectBoardApiService(ref.watch(dioProvider)));
final employeeApiServiceProvider = Provider((ref) => EmployeeApiService(ref.watch(dioProvider)));
final departmentApiServiceProvider = Provider((ref) => DepartmentApiService(ref.watch(dioProvider)));

final projectRepositoryProvider = Provider<ProjectRepository>((ref) {
  return ProjectRepositoryImpl(
    projectApi: ref.watch(projectApiServiceProvider),
    boardApi: ref.watch(projectBoardApiServiceProvider),
    employeeApi: ref.watch(employeeApiServiceProvider),
    departmentApi: ref.watch(departmentApiServiceProvider),
  );
});
