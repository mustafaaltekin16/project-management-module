import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/dio_client_provider.dart';
import '../../data/document_api_service.dart';
import '../../data/note_api_service.dart';
import '../../data/task_api_service.dart';

final taskApiServiceProvider = Provider((ref) => TaskApiService(ref.watch(dioProvider)));
final documentApiServiceProvider = Provider((ref) => DocumentApiService(ref.watch(dioProvider)));
final noteApiServiceProvider = Provider((ref) => NoteApiService(ref.watch(dioProvider)));
