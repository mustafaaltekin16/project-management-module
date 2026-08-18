import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../projects/data/dto/create_project_request_dto.dart';
import '../../../projects/domain/entities/department.dart';
import '../../../projects/domain/entities/employee.dart';
import '../../../projects/domain/entities/project.dart';
import '../../../projects/presentation/controllers/projects_list_controller.dart';
import '../../../projects/presentation/controllers/projects_providers.dart';
import '../../domain/form_state.dart';

final employeesProvider = FutureProvider<List<Employee>>((ref) {
  return ref.read(projectRepositoryProvider).listEmployees();
});

final departmentsProvider = FutureProvider<List<Department>>((ref) {
  return ref.read(projectRepositoryProvider).listDepartments();
});

class ProjectCreateController extends Notifier<ProjectCreateFormState> {
  @override
  ProjectCreateFormState build() => ProjectCreateFormState();

  void setType(ProjectType type) {
    state = ProjectCreateFormState(
      type: type,
      name: state.name,
      description: state.description,
      startDate: state.startDate,
      endDate: state.endDate,
      managerEmployeeId: state.managerEmployeeId,
      managerName: state.managerName,
    );
  }

  void setName(String value) => state = state.copyWith(name: value, errors: []);
  void setDescription(String value) =>
      state = state.copyWith(description: value, errors: []);
  void setStartDate(DateTime value) =>
      state = state.copyWith(startDate: value, errors: []);
  void setEndDate(DateTime value) =>
      state = state.copyWith(endDate: value, errors: []);
  void setManager(String id, String name) => state = state.copyWith(
    managerEmployeeId: id,
    managerName: name,
    errors: [],
  );
  void setSecondManager(String? id, String? name) => state = state.copyWith(
    secondManagerEmployeeId: id,
    secondManagerName: name,
    errors: [],
  );
  void setBudget(double? value) =>
      state = state.copyWith(budget: value, errors: []);
  void setCurrency(String value) =>
      state = state.copyWith(currency: value, errors: []);

  void addDepartmentRow(Department department) {
    final rows = [
      ...state.departments,
      DepartmentRowFormData(
        departmentId: department.id,
        departmentName: department.name,
        managerEmployeeId: department.headEmployeeId,
      ),
    ];
    state = state.copyWith(departments: rows, errors: []);
  }

  void removeDepartmentRow(int index) {
    final rows = [...state.departments]..removeAt(index);
    state = state.copyWith(departments: rows, errors: []);
  }

  void updateDepartmentRow(int index, DepartmentRowFormData row) {
    final rows = [...state.departments];
    rows[index] = row;
    state = state.copyWith(departments: rows, errors: []);
  }

  void reorderDepartmentRows(int oldIndex, int newIndex) {
    final rows = [...state.departments];
    if (newIndex > oldIndex) newIndex -= 1;
    final row = rows.removeAt(oldIndex);
    rows.insert(newIndex, row);
    state = state.copyWith(departments: rows, errors: []);
  }

  void addAttachment(String path, String fileName) {
    state = state.copyWith(
      attachments: [
        ...state.attachments,
        AttachmentFormData(path: path, fileName: fileName),
      ],
    );
  }

  void removeAttachment(int index) {
    final list = [...state.attachments]..removeAt(index);
    state = state.copyWith(attachments: list);
  }

  List<String> validate() {
    final errors = <String>[];
    if (state.name.trim().isEmpty) errors.add('Proje adı gerekli.');
    if (state.startDate == null || state.endDate == null) {
      errors.add('Başlangıç ve bitiş tarihi gerekli.');
    } else if (!state.endDate!.isAfter(state.startDate!)) {
      errors.add('Bitiş tarihi başlangıçtan sonra olmalı.');
    }
    if (state.managerEmployeeId == null) {
      errors.add('Proje yöneticisi seçilmeli.');
    }
    if (state.type == ProjectType.multiUnit) {
      if (state.budget == null || state.budget! <= 0) {
        errors.add('Bütçe 0\'dan büyük olmalı.');
      }
      if (state.departments.isEmpty) {
        errors.add('En az bir departman/iş paketi eklenmeli.');
      }
    } else if (state.departments.isEmpty) {
      errors.add('Departman seçilmeli.');
    }
    for (final row in state.departments) {
      if (row.managerEmployeeId == null) {
        errors.add('${row.departmentName} için yönetici seçilmeli.');
      }
      if (state.type == ProjectType.multiUnit &&
          (row.startDate == null || row.endDate == null)) {
        errors.add('${row.departmentName} için tarihler gerekli.');
      }
    }
    state = state.copyWith(errors: errors);
    return errors;
  }

  Future<String> submit() async {
    state = state.copyWith(isSubmitting: true);
    try {
      final request = CreateProjectRequestDto(
        name: state.name.trim(),
        description: state.description.trim(),
        managerEmployeeId: state.managerEmployeeId!,
        managerName: state.managerName ?? '',
        secondManagerEmployeeId: state.type == ProjectType.multiUnit
            ? state.secondManagerEmployeeId
            : null,
        secondManagerName: state.type == ProjectType.multiUnit
            ? state.secondManagerName
            : null,
        unitDepartmentId: state.departments.isNotEmpty
            ? state.departments.first.departmentId
            : '',
        unit: state.departments.isNotEmpty
            ? state.departments.first.departmentName
            : '',
        type: projectTypeToJson(state.type),
        budget: state.type == ProjectType.multiUnit ? state.budget : null,
        currency: state.type == ProjectType.multiUnit ? state.currency : null,
        startDate: state.startDate!,
        endDate: state.endDate!,
        templateId: null,
        enabledComponents: const ['description', 'tasks', 'documents'],
        templateValues: const {},
        departments: state.departments
            .map(
              (d) => CreateDepartmentRowDto(
                departmentId: d.departmentId,
                title: d.title.isEmpty ? d.departmentName : d.title,
                departmentName: d.departmentName,
                managerEmployeeId: d.managerEmployeeId!,
                managerName: d.managerName ?? '',
                startDate: d.startDate,
                endDate: d.endDate,
              ),
            )
            .toList(),
      );

      final id = await ref.read(projectRepositoryProvider).create(request);

      for (final attachment in state.attachments) {
        try {
          await ref
              .read(projectRepositoryProvider)
              .uploadDocument(id, attachment.path, attachment.fileName);
        } catch (_) {
          // Web ile aynı davranış: bir dosya başarısız olsa da proje oluşturma iptal olmaz.
        }
      }

      await ref.read(projectsListControllerProvider.notifier).refresh();
      return id;
    } finally {
      state = state.copyWith(isSubmitting: false);
    }
  }
}

final projectCreateControllerProvider =
    NotifierProvider<ProjectCreateController, ProjectCreateFormState>(
      ProjectCreateController.new,
    );
