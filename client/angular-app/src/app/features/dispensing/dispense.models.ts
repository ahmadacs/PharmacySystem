import { DispensingLookupResponse } from '../../core/models/api.models';

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
}

export interface DispenseView {
  shortCode: string;
  phoneNumber: string;
  patientName: string;
  items: DispenseViewItem[];
}
