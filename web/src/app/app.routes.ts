import { Routes } from '@angular/router';
import { authGuard } from './shared/auth/auth.guard';
import { projectManagementGuard } from './shared/auth/project-management.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'projects' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login-page/login-page').then((m) => m.LoginPage),
    title: 'Giriş Yap'
  },
  {
    path: 'projects',
    loadComponent: () => import('./features/projects/projects-page/projects-page').then((m) => m.ProjectsPage),
    canActivate: [authGuard],
    title: 'Projeler Modülü'
  },
  {
    path: 'projects/new',
    loadComponent: () =>
      import('./features/projects/project-create-page/project-create-page').then((m) => m.ProjectCreatePage),
    canActivate: [authGuard],
    title: 'Yeni Proje Oluştur'
  },
  {
    path: 'projects/templates',
    loadComponent: () =>
      import('./features/projects/project-templates-page/project-templates-page').then(
        (m) => m.ProjectTemplatesPage
      ),
    canActivate: [authGuard],
    title: 'Şablonlar'
  },
  {
    path: 'projects/templates/new',
    loadComponent: () =>
      import('./features/projects/project-template-builder-page/project-template-builder-page').then(
        (m) => m.ProjectTemplateBuilderPage
      ),
    canActivate: [authGuard],
    title: 'Yeni Şablon Oluştur'
  },
  {
    path: 'projects/templates/:templateId',
    loadComponent: () =>
      import('./features/projects/project-template-builder-page/project-template-builder-page').then(
        (m) => m.ProjectTemplateBuilderPage
      ),
    canActivate: [authGuard],
    title: 'Şablonu Düzenle'
  },
  {
    path: 'organization',
    loadComponent: () =>
      import('./features/projects/project-departments-page/project-departments-page').then(
        (m) => m.ProjectDepartmentsPage
      ),
    canActivate: [authGuard, projectManagementGuard],
    title: 'Organizasyon Yönetimi'
  },
  { path: 'projects/departments', redirectTo: 'organization', pathMatch: 'full' },
  {
    path: 'projects/:projectId',
    loadComponent: () =>
      import('./features/projects/project-detail-page/project-detail-page').then((m) => m.ProjectDetailPage),
    canActivate: [authGuard],
    title: 'Proje Detayı'
  },
  { path: '**', redirectTo: 'projects' }
];
