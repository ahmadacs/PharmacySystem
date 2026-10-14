import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MedicineListItemDto } from '../../../core/models/api.models';
import { refillsAllowedValidator } from './prescription-item.validators';

export interface PrescriptionItemBlank {
  medicineSearch: string;
  medicineId: null;
  medicineVariantId: null;
  quantity: number;
  dosageInstructions: string;
  isRefillable: boolean;
  refillsAllowed: number;
  refillIntervalDays: number;
}

export const BLANK_PRESCRIPTION_ITEM: PrescriptionItemBlank = {
  medicineSearch: '',
  medicineId: null,
  medicineVariantId: null,
  quantity: 1,
  dosageInstructions: '',
  isRefillable: false,
  refillsAllowed: 0,
  refillIntervalDays: 0,
};

/**
 * Factory for one prescription item row. Extracted from the 450-line
 * PrescriptionFormDialogComponent so the refill toggle wiring lives once.
 */
export function createPrescriptionItemGroup(destroyRef: DestroyRef): FormGroup {
  const group = new FormGroup({
    medicineSearch: new FormControl<string | MedicineListItemDto>('', { nonNullable: true }),
    medicineId: new FormControl<string | null>(null, { validators: [Validators.required] }),
    medicineVariantId: new FormControl<string | null>(null, { validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    dosageInstructions: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(300)] }),
    isRefillable: new FormControl(false, { nonNullable: true }),
    refillsAllowed: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(99), refillsAllowedValidator] }),
    refillIntervalDays: new FormControl(0, { nonNullable: true, validators: [Validators.pattern(/^\d+$/), Validators.min(0), Validators.max(365)] }),
  });

  group.get('isRefillable')?.valueChanges.pipe(takeUntilDestroyed(destroyRef)).subscribe((refillable) => {
    const allowed = group.get('refillsAllowed');
    if (!refillable) {
      if (Number(allowed?.value ?? 0) !== 0) {
        allowed?.setValue(0);
      }
    } else if (Number(allowed?.value ?? 0) < 1) {
      allowed?.setValue(1);
    }
    allowed?.updateValueAndValidity();
  });

  return group;
}

export function resetItemGroup(group: FormGroup): void {
  group.reset({ ...BLANK_PRESCRIPTION_ITEM });
}
