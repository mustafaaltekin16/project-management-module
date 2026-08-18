import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/api/api_response.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../projects/domain/entities/employee.dart';
import '../../../projects/domain/entities/project.dart';
import '../controllers/project_create_controller.dart';
import '../widgets/attachment_picker.dart';
import '../widgets/department_row_list.dart';
import '../widgets/mode_selector.dart';

class ProjectCreateScreen extends ConsumerStatefulWidget {
  const ProjectCreateScreen({super.key});

  @override
  ConsumerState<ProjectCreateScreen> createState() =>
      _ProjectCreateScreenState();
}

class _ProjectCreateScreenState extends ConsumerState<ProjectCreateScreen> {
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _scrollController = ScrollController();

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final controller = ref.read(projectCreateControllerProvider.notifier);
    final errors = controller.validate();
    if (errors.isNotEmpty) {
      if (_scrollController.hasClients) {
        await _scrollController.animateTo(
          0,
          duration: const Duration(milliseconds: 320),
          curve: Curves.easeOutCubic,
        );
      }
      return;
    }
    try {
      final id = await controller.submit();
      if (mounted) context.pushReplacement(RoutePaths.projectDetail(id));
    } catch (e) {
      final message = e is ApiException ? e.message : 'Proje oluşturulamadı.';
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(message)));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final formState = ref.watch(projectCreateControllerProvider);
    final controller = ref.read(projectCreateControllerProvider.notifier);
    final employeesAsync = ref.watch(employeesProvider);
    final scheme = Theme.of(context).colorScheme;
    final showValidation = formState.errors.isNotEmpty;
    final datesInvalid =
        formState.startDate == null ||
        formState.endDate == null ||
        !formState.endDate!.isAfter(formState.startDate!);

    return Scaffold(
      appBar: AppBar(title: const Text('Yeni proje')),
      body: Column(
        children: [
          Expanded(
            child: ListView(
              controller: _scrollController,
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                AppSpacing.xs,
                AppSpacing.md,
                AppSpacing.xl,
              ),
              children: [
                Text(
                  'Proje planını oluştur',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  'Temel bilgileri, ekibi ve çalışma alanlarını belirleyin. Zorunlu alanlar * ile işaretlidir.',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: scheme.onSurfaceVariant,
                  ),
                ),
                if (showValidation) ...[
                  const SizedBox(height: AppSpacing.md),
                  _ValidationSummary(errors: formState.errors),
                ],
                const SizedBox(height: AppSpacing.xl),
                _SectionCard(
                  step: '1',
                  icon: Icons.tune_rounded,
                  title: 'Proje türü',
                  subtitle: 'Çalışma biçiminize en uygun yapıyı seçin.',
                  children: [
                    ModeSelector(
                      value: formState.type,
                      onChanged: controller.setType,
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                _SectionCard(
                  step: '2',
                  icon: Icons.description_outlined,
                  title: 'Temel bilgiler',
                  subtitle: 'Projenin kapsamını ve zaman aralığını tanımlayın.',
                  children: [
                    TextFormField(
                      controller: _nameController,
                      textInputAction: TextInputAction.next,
                      decoration: InputDecoration(
                        labelText: 'Proje adı *',
                        hintText: 'Örn. Mobil deneyim yenileme',
                        errorText:
                            showValidation && formState.name.trim().isEmpty
                            ? 'Proje adı gerekli.'
                            : null,
                      ),
                      onChanged: controller.setName,
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    TextFormField(
                      controller: _descriptionController,
                      minLines: 3,
                      maxLines: 5,
                      decoration: const InputDecoration(
                        labelText: 'Açıklama',
                        hintText:
                            'Amaç, kapsam ve beklenen çıktıları kısaca yazın',
                        alignLabelWithHint: true,
                      ),
                      onChanged: controller.setDescription,
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    _DateField(
                      label: 'Başlangıç tarihi *',
                      value: formState.startDate,
                      errorText:
                          showValidation &&
                              datesInvalid &&
                              formState.startDate == null
                          ? 'Tarih seçin.'
                          : null,
                      onChanged: controller.setStartDate,
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    _DateField(
                      label: 'Bitiş tarihi *',
                      value: formState.endDate,
                      errorText: showValidation && datesInvalid
                          ? formState.endDate == null
                                ? 'Tarih seçin.'
                                : 'Bitiş başlangıçtan sonra olmalı.'
                          : null,
                      onChanged: controller.setEndDate,
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                _SectionCard(
                  step: '3',
                  icon: Icons.groups_outlined,
                  title: 'Ekip ve bütçe',
                  subtitle:
                      'Proje sahipliğini ve gerekiyorsa bütçeyi belirleyin.',
                  children: [
                    employeesAsync.when(
                      loading: () => const LinearProgressIndicator(),
                      error: (e, _) => Text(
                        'Çalışanlar yüklenemedi.',
                        style: TextStyle(color: scheme.error),
                      ),
                      data: (employees) => Column(
                        children: [
                          _EmployeeDropdown(
                            label: 'Proje yöneticisi *',
                            employees: employees,
                            value: formState.managerEmployeeId,
                            errorText:
                                showValidation &&
                                    formState.managerEmployeeId == null
                                ? 'Proje yöneticisi seçin.'
                                : null,
                            onChanged: (id, name) =>
                                controller.setManager(id, name),
                          ),
                          if (formState.type == ProjectType.multiUnit) ...[
                            const SizedBox(height: AppSpacing.sm),
                            _EmployeeDropdown(
                              label: 'İkinci proje yöneticisi',
                              employees: employees,
                              value: formState.secondManagerEmployeeId,
                              onChanged: (id, name) =>
                                  controller.setSecondManager(id, name),
                            ),
                          ],
                        ],
                      ),
                    ),
                    if (formState.type == ProjectType.multiUnit) ...[
                      const SizedBox(height: AppSpacing.sm),
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: TextFormField(
                              keyboardType:
                                  const TextInputType.numberWithOptions(
                                    decimal: true,
                                  ),
                              decoration: InputDecoration(
                                labelText: 'Bütçe *',
                                errorText:
                                    showValidation &&
                                        (formState.budget == null ||
                                            formState.budget! <= 0)
                                    ? 'Geçerli bütçe girin.'
                                    : null,
                              ),
                              onChanged: (v) => controller.setBudget(
                                double.tryParse(v.replaceAll(',', '.')),
                              ),
                            ),
                          ),
                          const SizedBox(width: AppSpacing.sm),
                          SizedBox(
                            width: 108,
                            child: DropdownButtonFormField<String>(
                              initialValue: formState.currency,
                              decoration: const InputDecoration(
                                labelText: 'Para',
                              ),
                              items: const [
                                DropdownMenuItem(
                                  value: 'TRY',
                                  child: Text('TRY'),
                                ),
                                DropdownMenuItem(
                                  value: 'USD',
                                  child: Text('USD'),
                                ),
                                DropdownMenuItem(
                                  value: 'EUR',
                                  child: Text('EUR'),
                                ),
                              ],
                              onChanged: (v) =>
                                  controller.setCurrency(v ?? 'TRY'),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                _SectionCard(
                  step: '4',
                  icon: Icons.apartment_outlined,
                  title: 'Departmanlar',
                  subtitle: formState.type == ProjectType.multiUnit
                      ? 'İş paketlerini, sorumluları ve tarihleri ekleyin.'
                      : 'Projeyi yürütecek departmanı seçin.',
                  children: const [DepartmentRowList()],
                ),
                const SizedBox(height: AppSpacing.md),
                const _SectionCard(
                  step: '5',
                  icon: Icons.attach_file_rounded,
                  title: 'Dosyalar',
                  subtitle:
                      'Başlangıçta ekibe gerekli olacak belgeleri ekleyin.',
                  children: [AttachmentPicker()],
                ),
              ],
            ),
          ),
          SafeArea(
            top: false,
            child: Container(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                AppSpacing.sm,
                AppSpacing.md,
                AppSpacing.md,
              ),
              decoration: BoxDecoration(
                color: scheme.surface,
                border: Border(top: BorderSide(color: scheme.outlineVariant)),
              ),
              child: FilledButton.icon(
                onPressed: formState.isSubmitting ? null : _submit,
                icon: formState.isSubmitting
                    ? const SizedBox(
                        height: 18,
                        width: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Icon(Icons.check_rounded),
                label: Text(
                  formState.isSubmitting
                      ? 'Proje oluşturuluyor…'
                      : 'Projeyi oluştur',
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ValidationSummary extends StatelessWidget {
  const _ValidationSummary({required this.errors});

  final List<String> errors;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: scheme.errorContainer.withValues(alpha: 0.7),
        borderRadius: BorderRadius.circular(AppRadius.md),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.info_outline_rounded, color: scheme.onErrorContainer),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Eksik bilgileri tamamlayın',
                  style: TextStyle(
                    color: scheme.onErrorContainer,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: AppSpacing.xxs),
                Text(
                  errors.first,
                  style: TextStyle(
                    color: scheme.onErrorContainer,
                    fontSize: 13,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _SectionCard extends StatelessWidget {
  const _SectionCard({
    required this.step,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.children,
  });

  final String step;
  final IconData icon;
  final String title;
  final String subtitle;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Stack(
                  clipBehavior: Clip.none,
                  children: [
                    IconBadge(icon: icon, size: 42),
                    Positioned(
                      right: -4,
                      top: -5,
                      child: Container(
                        width: 20,
                        height: 20,
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          color: scheme.primary,
                          shape: BoxShape.circle,
                          border: Border.all(color: scheme.surface, width: 2),
                        ),
                        child: Text(
                          step,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 10,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: AppSpacing.xxs),
                      Text(
                        subtitle,
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: scheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.lg),
            ...children,
          ],
        ),
      ),
    );
  }
}

class _DateField extends StatelessWidget {
  const _DateField({
    required this.label,
    required this.value,
    required this.onChanged,
    this.errorText,
  });

  final String label;
  final DateTime? value;
  final void Function(DateTime) onChanged;
  final String? errorText;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(AppRadius.md),
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
        decoration: InputDecoration(
          labelText: label,
          errorText: errorText,
          suffixIcon: const Icon(Icons.calendar_today_outlined, size: 18),
        ),
        child: Text(
          value != null
              ? DateFormat('dd.MM.yyyy').format(value!)
              : 'Tarih seçin',
        ),
      ),
    );
  }
}

class _EmployeeDropdown extends StatelessWidget {
  const _EmployeeDropdown({
    required this.label,
    required this.employees,
    required this.value,
    required this.onChanged,
    this.errorText,
  });

  final String label;
  final List<Employee> employees;
  final String? value;
  final void Function(String id, String name) onChanged;
  final String? errorText;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<String>(
      initialValue: value,
      isExpanded: true,
      decoration: InputDecoration(labelText: label, errorText: errorText),
      items: employees
          .map((e) => DropdownMenuItem(value: e.id, child: Text(e.fullName)))
          .toList(),
      onChanged: (id) {
        if (id == null) return;
        final name = employees.firstWhere((e) => e.id == id).fullName;
        onChanged(id, name);
      },
    );
  }
}
