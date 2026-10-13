import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { Auth } from '../services/auth';

/**
 * These return 401 for reasons other than an expired access token (a wrong
 * password, or a refresh token the server no longer accepts). They must never
 * trigger a refresh, and refreshing on /refresh itself would loop forever.
 */
const NO_REFRESH_PATHS = ['/login', '/refresh'];

function withBearer<T>(req: HttpRequest<T>, token: string): HttpRequest<T> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isExpiredTokenError(error: unknown, url: string): boolean {
  return (
    error instanceof HttpErrorResponse &&
    error.status === 401 &&
    !NO_REFRESH_PATHS.some((path) => url.endsWith(path))
  );
}

/**
 * Attaches the access token. If the server answers 401 because the token has
 * expired, the session is refreshed once and the request is replayed. If the
 * refresh fails, the user is signed out and sent to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(Auth);
  const router = inject(Router);

  const sentWith = auth.accessToken;

  if (!sentWith) {
    return next(req);
  }

  return next(withBearer(req, sentWith)).pipe(
    catchError((error: unknown) => {
      if (!isExpiredTokenError(error, req.url)) {
        return throwError(() => error);
      }

      // Another request already refreshed while this one was in flight.
      const latest = auth.accessToken;

      if (latest && latest !== sentWith) {
        return next(withBearer(req, latest));
      }

      return auth.refreshSession().pipe(
        // Only refresh failures land here. Errors from the replayed request
        // must reach the caller untouched, not sign the user out.
        catchError(() => {
          auth.logout();
          void router.navigate(['/login']);
          return throwError(() => error);
        }),
        switchMap((tokens) => next(withBearer(req, tokens.accessToken)))
      );
    })
  );
};
