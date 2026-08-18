class RoutePaths {
  static const login = '/login';
  static const dashboard = '/dashboard';
  static const projects = '/projects';
  static const projectCreate = '/projects/new';
  static const profile = '/profile';

  static String projectDetail(String id) => '/projects/$id';
}
