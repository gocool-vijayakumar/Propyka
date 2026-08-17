import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { Auth } from '../services/auth';

/**
 * Roles live on the server, so on a cold page load the profile may not be
 * loaded yet — the guard fetches it before deciding. A guard may return an
 * Observable, and the router waits for it.
 *
 * This is a UX gate, not a security boundary: every admin endpoint is also
 * protected by [Authorize(Roles = "Admin")] on the server.
 */
export const adminGuard: CanActivateFn = () => {

  const auth = inject(Auth);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (auth.profile()) {
    return auth.isAdmin() ? true : router.createUrlTree(['/home']);
  }

  return auth.loadProfile().pipe(
    map((profile) =>
      profile.roles.includes('Admin')
        ? true
        : router.createUrlTree(['/home'])
    ),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};