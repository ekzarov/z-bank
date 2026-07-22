import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from './session.service';

export const roleGuard: CanActivateFn = route => {
  const sessions = inject(SessionService);
  const router = inject(Router);
  const requiredRole = route.data['role'] as string;
  return sessions.session()?.roles.includes(requiredRole) ? true : router.createUrlTree(['/']);
};
