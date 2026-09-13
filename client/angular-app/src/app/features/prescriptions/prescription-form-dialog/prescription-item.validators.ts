import { AbstractControl, FormControl, ValidationErrors } from '@angular/forms';
import { startOfDay } from '../../../core/utils/date-utils';

/** Rejects dates after today (used for date of birth / issue date). */
export function notInFuture(control: FormControl<Date | null>): Record<string, boolean> | null {
  const value = control.value;
  if (!value) {
    return null;
  }
  return startOfDay(value) <= startOfDay(new Date()) ? null : { futureDate: true };
}

/**
 * Requires `refillsAllowed` to be an integer >= 1, but ONLY while the sibling
 * `isRefillable` control in the same item group is on.
 *
 * Single source of truth for the refill rule — the dialog no longer duplicates
 * it with a manual loop inside `submit()`. The `isRefillable` toggle handler
 * only adjusts the *value* (prefill 1 / reset 0) and re-triggers validation;
 * it never touches errors directly, so it cannot wipe other errors (e.g. max).
 */
export function refillsAllowedValidator(control: AbstractControl): ValidationErrors | null {
  const parent = control.parent;
  if (!parent) {
    return null;
  }
  const refillable = parent.get('isRefillable')?.value === true;
  if (!refillable) {
    return null;
  }
  const raw: unknown = control.value;
  const value = typeof raw === 'string' ? Number(raw) : (raw as number);
  if (!Number.isInteger(value) || (value as number) < 1) {
    return { refillRequired: true };
  }
  return null;
}
