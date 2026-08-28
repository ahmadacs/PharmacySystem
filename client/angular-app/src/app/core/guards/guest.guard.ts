import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../auth/auth.store';

/** Blocks already-authenticated users from the login/register pages. */
export const guestGuard: CanActivateFn = async () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (!authStore.initialized()) {
    await authStore.initialize();
  }

  if (authStore.isAuthenticated()) {
    return router.createUrlTree(['/dashboard']);
  }
  return true;
};