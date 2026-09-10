/**
 * Single source of truth for the UI-culture localStorage key, namespaced
 * to the application.
 */
export const CULTURE_STORAGE_KEY = 'PharmacySystem.CultureName';

export function readStoredCulture(): string | null {
  return localStorage.getItem(CULTURE_STORAGE_KEY);
}
