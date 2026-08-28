import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../auth/auth.store';

/**
 * Requires authentication AND at least one of the given permissions; else
 * /forbidden. Mirrors backend policies that accept multiple permission paths
 * (e.g. `Prescriptions.View` OR `Prescriptions.ManageOwn`).
 */
export function permissionGuard(...permissions: string[]): CanActivateFn {
  return async () => {
    const authStore = inject(AuthStore);
    const router = inject(Router);

    if (!authStore.initialized()) {
      await authStore.initialize();
    }

    if (!authStore.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }
    if (!permissions.some((permission) => authStore.hasPermission(permission))) {
      return router.createUrlTree(['/forbidden']);
    }
    return true;
  };
}