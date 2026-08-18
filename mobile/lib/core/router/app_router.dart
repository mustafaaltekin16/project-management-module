import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/dashboard/presentation/screens/dashboard_screen.dart';
import '../../features/login/presentation/controllers/auth_controller.dart';
import '../../features/login/presentation/screens/login_screen.dart';
import '../../features/profile/presentation/screens/profile_screen.dart';
import '../../features/project_create/presentation/screens/project_create_screen.dart';
import '../../features/project_detail/presentation/screens/project_detail_screen.dart';
import '../../features/projects/presentation/screens/projects_list_screen.dart';
import '../../shared/navigation/main_shell.dart';
import 'route_paths.dart';

class _AuthRouterRefreshNotifier extends ChangeNotifier {
  _AuthRouterRefreshNotifier(Ref ref) {
    ref.listen(authControllerProvider, (_, __) => notifyListeners());
  }
}

final _routerRefreshProvider = Provider<_AuthRouterRefreshNotifier>((ref) {
  return _AuthRouterRefreshNotifier(ref);
});

final appRouterProvider = Provider<GoRouter>((ref) {
  final refreshNotifier = ref.watch(_routerRefreshProvider);

  return GoRouter(
    initialLocation: RoutePaths.dashboard,
    refreshListenable: refreshNotifier,
    redirect: (context, state) {
      final authState = ref.read(authControllerProvider);
      if (authState.isLoading) return null;

      final isLoggedIn = authState.valueOrNull != null;
      final isLoggingIn = state.matchedLocation == RoutePaths.login;

      if (!isLoggedIn && !isLoggingIn) return RoutePaths.login;
      if (isLoggedIn && isLoggingIn) return RoutePaths.dashboard;
      return null;
    },
    routes: [
      GoRoute(path: RoutePaths.login, builder: (context, state) => const LoginScreen()),
      GoRoute(
        path: RoutePaths.projectCreate,
        builder: (context, state) => const ProjectCreateScreen(),
      ),
      GoRoute(
        path: '${RoutePaths.projects}/:id',
        builder: (context, state) => ProjectDetailScreen(projectId: state.pathParameters['id']!),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) => MainShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: RoutePaths.dashboard, builder: (context, state) => const DashboardScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: RoutePaths.projects, builder: (context, state) => const ProjectsListScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: RoutePaths.profile, builder: (context, state) => const ProfileScreen()),
          ]),
        ],
      ),
    ],
  );
});
