import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { SessionService } from './session.service';

export const authGuard: CanActivateFn = () => {
  const sessions = inject(SessionService);
  const router = inject(Router);
  if (sessions.session()) {
    return true;
  }

  return sessions.load().pipe(
    map(session => session ? true : router.createUrlTree(['/sign-in']))
  );
};
