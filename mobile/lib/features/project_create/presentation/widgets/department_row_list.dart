import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../projects/domain/entities/department.dart';
import '../../../projects/domain/entities/employee.dart';
import '../../../projects/domain/entities/project.dart';
import '../controllers/project_create_controller.dart';

class DepartmentRowList extends ConsumerWidget {
  const DepartmentRowList({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final formState = ref.watch(projectCreateControllerProvider);
    final controller = ref.read(projectCreateControllerProvider.notifier);
    final departmentsAsync = ref.watch(departmentsProvider);
    final employeesAsync = ref.watch(employeesProvider);
    final isMulti = formState.type == ProjectType.multiUnit;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        ReorderableListView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: formState.departments.length,
          onReorderItem: controller.reorderDepartmentRows,
          itemBuilder: (context, index) {
            final row = formState.departments[index];
            return Card(
              key: ValueKey('${row.departmentId}_$index'),
              margin: const EdgeInsets.symmetric(vertical: 4),
              child: Padding(
                padding: const EdgeInsets.all(10),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            row.departmentName,
                            style: const TextStyle(fontWeight: FontWeight.w600),
                          ),
                        ),
                        IconButton(
                          icon: const Icon(Icons.delete_outline),
                          onPressed: () =>
                              controller.removeDepartmentRow(index),
                        ),
                      ],
                    ),
                    if (isMulti)
                      TextFormField(
                        initialValue: row.title,
                        decoration: const InputDecoration(
                          labelText: 'Başlık',
                          isDense: true,
                        ),
                        onChanged: (v) => controller.updateDepartmentRow(
                          index,
                          row..title = v,
                        ),
                      ),
                    employeesAsync.maybeWhen(
                      data: (employees) => DropdownButtonFormField<String>(
                        initialValue: row.managerEmployeeId,
                        decoration: const InputDecoration(
                          labelText: 'Departman Yöneticisi',
                          isDense: true,
                        ),
                        items: employees
                            .map(
                              (e) => DropdownMenuItem(
                                value: e.id,
                                child: Text(e.fullName),
                              ),
                            )
                            .toList(),
                        onChanged: (v) {
                          final name = employees
                              .firstWhere(
                                (e) => e.id == v,
                                orElse: () => Employee(
                                  id: '',
                                  fullName: '',
                                  roles: [],
                                  isActive: true,
                                ),
                              )
                              .fullName;
                          controller.updateDepartmentRow(
                            index,
                            row
                              ..managerEmployeeId = v
                              ..managerName = name,
                          );
                        },
                      ),
                      orElse: () => const SizedBox.shrink(),
                    ),
                    if (isMulti)
                      Row(
                        children: [
                          Expanded(
                            child: _DatePickerField(
                              label: 'Başlangıç',
                              value: row.startDate,
                              onChanged: (d) => controller.updateDepartmentRow(
                                index,
                                row..startDate = d,
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: _DatePickerField(
                              label: 'Bitiş',
                              value: row.endDate,
                              onChanged: (d) => controller.updateDepartmentRow(
                                index,
                                row..endDate = d,
                              ),
                            ),
                          ),
                        ],
                      ),
                  ],
                ),
              ),
            );
          },
        ),
        departmentsAsync.when(
          loading: () => const LinearProgressIndicator(),
          error: (e, _) => const Text('Departmanlar yüklenemedi.'),
          data: (departments) => Align(
            alignment: Alignment.centerLeft,
            child: TextButton.icon(
              onPressed: () async {
                final selected = await showModalBottomSheet<Department>(
                  context: context,
                  builder: (context) => SafeArea(
                    child: ListView(
                      shrinkWrap: true,
                      children: departments
                          .map(
                            (d) => ListTile(
                              title: Text(d.name),
                              onTap: () => Navigator.of(context).pop(d),
                            ),
                          )
                          .toList(),
                    ),
                  ),
                );
                if (selected != null) controller.addDepartmentRow(selected);
              },
              icon: const Icon(Icons.add),
              label: const Text('Departman ekle'),
            ),
          ),
        ),
      ],
    );
  }
}

class _DatePickerField extends StatelessWidget {
  const _DatePickerField({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  final String label;
  final DateTime? value;
  final void Function(DateTime) onChanged;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () async {
        final picked = await showDatePicker(
          context: context,
          initialDate: value ?? DateTime.now(),
          firstDate: DateTime(2000),
          lastDate: DateTime(2100),
        );
        if (picked != null) onChanged(picked);
      },
      child: InputDecorator(
        decoration: InputDecoration(labelText: label, isDense: true),
        child: Text(
          value != null ? DateFormat('dd.MM.yyyy').format(value!) : '-',
        ),
      ),
    );
  }
}
