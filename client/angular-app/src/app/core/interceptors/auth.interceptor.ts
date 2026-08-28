import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { AuthStore } from '../auth/auth.store';
import { TokenStore } from '../auth/token.store';
import { SignalrService } from '../services/signalr.service';

const AUTH_SKIP_PATHS = ['/auth/login', '/auth/register', '/auth/refresh', '/auth/logout'];

function isAuthEndpoint(url: string): boolean {
  return AUTH_SKIP_PATHS.some((path) => url.includes(path));
}

let refreshPromise: Promise<unknown> | null = null;

/**
 * Attaches the in-memory access token to every request and implements the
 * silent-refresh flow: on 401 (and no refresh in flight) it calls
 * POST /auth/refresh once, reuses the httpOnly cookie, then retries the
 * original request. If the refresh itself fails, the session is expired and
 * the user is redirected to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStore = inject(TokenStore);
  const authStore = inject(AuthStore);
  const authService = inject(AuthService);
  const signalr = inject(SignalrService);

  const withToken = (request: typeof req): typeof req => {
    const token = tokenStore.accessToken();
    const headers: Record<string, string> = {};
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    const lang = localStorage.getItem('Abp.Localization.CultureName');
    if (lang) headers['Accept-Language'] = lang;
    return request.clone({ setHeaders: headers, withCredentials: true });
  };

  return next(withToken(req)).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthEndpoint(req.url) || tokenStore.sessionExpiredHandled()) {
        return throwError(() => error);
      }

      refreshPromise ??= authService
        .refresh()
        .then((response) => {
          tokenStore.resetSessionExpired();
          tokenStore.setAccessToken(response.accessToken);
          authStore.setSession(response.user);
          void signalr.start();
        })
        .finally(() => {
          refreshPromise = null;
        });

      return from(refreshPromise).pipe(
        switchMap(() => next(withToken(req))),
        catchError((refreshError: HttpErrorResponse) => {
          tokenStore.markSessionExpired();
          authStore.handleSessionExpired();
          return throwError(() => refreshError ?? error);
        })
      );
    })
  );
};