import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const SAUDI_PHONE_PATTERN = /^(?:\+9665\d{8}|05\d{8}|5\d{8})$/;

export const SAUDI_PHONE_SUFFIX_PATTERN = /^\d{8}$/;

export const SAUDI_PHONE_PREFIX = '+9665';

export const PERSON_NAME_PATTERN = /^[\p{L}][\p{L} .'-]*$/u;

export function personNameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (!value) {
      return null;
    }
    return PERSON_NAME_PATTERN.test(value.trim()) ? null : { invalidName: true };
  };
}

export function toFullSaudiPhone(suffix: string | null | undefined): string {
  const digits = (suffix ?? '').replace(/\D/g, '').slice(0, 8);
  return digits ? `${SAUDI_PHONE_PREFIX}${digits}` : '';
}
