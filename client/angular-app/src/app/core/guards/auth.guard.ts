import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../auth/auth.store';

/** Blocks unauthenticated users; redirects to /login preserving the return URL. */
export const authGuard: CanActivateFn = async () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  // Wait for the silent session restore (in-memory token is lost on reload)
  // before deciding, otherwise a valid session is treated as logged out.
  if (!authStore.initialized()) {
    await authStore.initialize();
  }

  if (authStore.isAuthenticated()) {
    return true;
  }
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: router.url }
  });
};