import { AbstractControl, FormControl, FormGroup, ValidationErrors } from '@angular/forms';

/** Shared date helpers (single source of truth, replaces duplicated copies in dialogs). */
export function startOfDay(value: Date): Date {
  return new Date(Date.UTC(value.getFullYear(), value.getMonth(), value.getDate()));
}

export function futureOrEqualDate(control: AbstractControl): ValidationErrors | null {
  const value = control.value as Date | null;
  if (!value) {
    return null;
  }
  return startOfDay(value) >= startOfDay(new Date()) ? null : { pastDate: true };
}

/** Rejects dates after today (used for date of birth / issue date). */
export function notInFuture(control: FormControl<Date | null>): ValidationErrors | null {
  const value = control.value;
  if (!value) {
    return null;
  }
  return startOfDay(value) <= startOfDay(new Date()) ? null : { futureDate: true };
}

/** Shared cross-field validator: expiry must not precede manufacture. */
export function expiryBeforeManufacture(control: AbstractControl): ValidationErrors | null {
  const group = control as FormGroup;
  const expiryDate = group.controls['expiryDate']?.value as Date | null;
  const manufactureDate = group.controls['manufactureDate']?.value as Date | null;
  return expiryDate && manufactureDate && startOfDay(expiryDate) < startOfDay(manufactureDate)
    ? { expiryBeforeManufacture: true }
    : null;
}

export function toDateString(date: Date | null): string | null {
  if (!date) {
    return null;
  }
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}
