import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_indicator.dart';
import '../../domain/entities/project_document.dart';
import '../controllers/documents_controller.dart';
import 'document_tile.dart';

class DocumentsTab extends ConsumerWidget {
  const DocumentsTab({super.key, required this.projectId});

  final String projectId;

  Future<void> _pickAndUpload(BuildContext context, WidgetRef ref) async {
    final file = await openFile();
    if (file == null) return;
    try {
      await ref
          .read(documentsControllerProvider(projectId).notifier)
          .upload(projectId, file.path, file.name);
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Dosya yüklendi.')));
      }
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Dosya yüklenemedi.')));
      }
    }
  }

  Future<void> _confirmDelete(
    BuildContext context,
    WidgetRef ref,
    ProjectDocument document,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        icon: const Icon(Icons.delete_outline_rounded),
        title: const Text('Dosya silinsin mi?'),
        content: Text('${document.fileName} kalıcı olarak silinecek.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Vazgeç'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Sil'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    try {
      await ref
          .read(documentsControllerProvider(projectId).notifier)
          .delete(projectId, document.id);
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Dosya silindi.')));
      }
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Dosya silinemedi.')));
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final documentsAsync = ref.watch(documentsControllerProvider(projectId));

    return Scaffold(
      backgroundColor: Colors.transparent,
      body: documentsAsync.when(
        loading: () => const LoadingIndicator(label: 'Dosyalar hazırlanıyor'),
        error: (e, _) => ErrorView(
          message: 'Dosyalar yüklenemedi.',
          onRetry: () => ref.invalidate(documentsControllerProvider(projectId)),
        ),
        data: (documents) {
          if (documents.isEmpty) {
            return EmptyState(
              title: 'Henüz dosya yok',
              message:
                  'Proje ekibinin ihtiyaç duyduğu dokümanları buraya yükleyin.',
              icon: Icons.folder_open_outlined,
              actionLabel: 'Dosya yükle',
              onAction: () => _pickAndUpload(context, ref),
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.all(AppSpacing.md),
            itemCount: documents.length,
            separatorBuilder: (context, index) =>
                const SizedBox(height: AppSpacing.xs),
            itemBuilder: (context, index) {
              final document = documents[index];
              return Card(
                child: DocumentTile(
                  document: document,
                  onDelete: () => _confirmDelete(context, ref, document),
                ),
              );
            },
          );
        },
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _pickAndUpload(context, ref),
        icon: const Icon(Icons.upload_file_rounded),
        label: const Text('Dosya yükle'),
      ),
    );
  }
}
