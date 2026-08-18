import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const projectManagementGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.hasAnyRole(['Admin', 'ProjectManager'])
    ? true
    : inject(Router).createUrlTree(['/projects']);
};
