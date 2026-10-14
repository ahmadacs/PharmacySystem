import { TranslateService } from '@ngx-translate/core';
import { DispensingLookupResponse, MedicineForm, MedicineUnit } from '../../core/models/api.models';
import { localizedVariantName, pickLocalizedMedicineName } from '../../core/utils/localized-name.utils';

export interface DispensePickerResult {
  lookup: DispensingLookupResponse;
  phoneNumber: string;
}

export type DispenseDialogData = DispensePickerResult | string;

export interface DispenseViewItem {
  id: string;
  medicineName: string;
  medicineNameAr: string | null;
  variantName: string;
  form: number | null;
  unit: number | null;
  strength: number | null;
  dosageInstructions: string | null;
  prescribedQuantity: number;
  remainingQuantity: number;
  availableQuantity: number | null;
  isRefillable: boolean;
  refillsRemaining: number | null;
  nextRefillEligibleDate: string | null;
  canRequestRefill: boolean;
}

export interface DispenseView {
  shortCode: string;
  phoneNumber: string;
  patientName: string;
  items: DispenseViewItem[];
}

type LocalizableItem = Pick<DispenseViewItem, 'form' | 'unit' | 'strength' | 'variantName'>;

/** Shared variant label (single source of truth for list + dialog). */
export function dispenseVariantLabel(item: LocalizableItem, translate: TranslateService): string {
  return (
    localizedVariantName(
      item,
      MedicineForm as unknown as Record<number, string>,
      MedicineUnit as unknown as Record<number, string>,
      translate,
    ) || item.variantName
  );
}

/** Shared medicine display name. */
export function dispenseMedicineName(
  item: Pick<DispenseViewItem, 'medicineName' | 'medicineNameAr'>,
  translate: TranslateService,
): string {
  return pickLocalizedMedicineName(item, translate);
}
