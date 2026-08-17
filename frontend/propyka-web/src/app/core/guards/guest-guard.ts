import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';

/**
 * The mirror of authGuard: authGuard keeps signed-out users off /home,
 * this keeps signed-in users off /login and /register.
 */
export const guestGuard: CanActivateFn = () => {

  const auth = inject(Auth);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return router.createUrlTree(['/home']);
  }

  return true;
};