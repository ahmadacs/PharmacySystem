import { WritableSignal } from '@angular/core';

/**
 * Loads entity details into a signal with the app's standard error contract
 * (the global error interceptor toasts; callers only observe state).
 * Replaces the repeated `service.get(id).then((d) => signal.set(d))`
 * blocks in the details dialogs, including the re-fetch after
 * refill/dispense mutations. `onError` covers dialogs that render an
 * inline error state (e.g. the dispense dialog's retry panel).
 */
export async function reloadDetails<T>(
  target: WritableSignal<T | null>,
  load: () => Promise<T>,
  onError?: () => void,
): Promise<void> {
  try {
    target.set(await load());
  } catch {
    onError?.();
    // error toast already shown by the error interceptor
  }
}

/**
 * Numeric members of a TypeScript enum, preserving the enum's type.
 * Replaces the 5x repeated
 * `Object.values(X).filter((v): v is X => typeof v === 'number')`
 * lines (plain `typeof v === 'number'` filters lose the enum type).
 */
export function numericEnumValues<T extends number>(enumObj: Record<string, T | string>): T[] {
  return Object.values(enumObj).filter((value): value is T => typeof value === 'number');
}
