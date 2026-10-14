export type PrescriptionStatus =
  | 'Pending'
  | 'PartiallyDispensed'
  | 'FullyDispensed'
  | 'Cancelled'
  | 'Expired';

/** History lookback window (days). Moved here so dialogs don't import it from a component. */
export type MedsLookback = 90 | 180 | 365;

export interface PrescriptionItemRequest {
  medicineVariantId: string;
  quantity: number;
  dosageInstructions?: string;
  isRefillable: boolean;
  refillsAllowed: number;
  refillIntervalDays: number;
}

export interface CreatePrescriptionRequest {
  patientFirstName: string;
  patientLastName: string;
  patientDateOfBirth: string;
  patientPhoneNumber?: string;
  diagnosis?: string;
  issuedDate: string;
  items: PrescriptionItemRequest[];
}

export interface PrescriptionItemDto {
  id: string;
  medicineVariantId: string;
  medicineName: string;
  medicineNameAr?: string;
  variantName: string;
  prescribedQuantity: number;
  dispensedQuantity: number;
  remainingQuantity: number;
  dosageInstructions: string;
  isRefillable: boolean;
  refillsAllowed: number;
  refillsUsed: number;
  refillIntervalDays: number;
  lastDispensedAt: string | null;
  form: number | null;
  unit: number | null;
  strength: number | null;
  // Derived refill helpers from the API (optional for backwards compatibility).
  refillsRemaining?: number;
  canRequestRefill?: boolean;
  isAwaitingRefillDispense?: boolean;
  nextRefillEligibleDate?: string | null;
}

export interface RefillPrescriptionRequest {
  itemIds: string[];
}

export interface PrescriptionListItemDto {
  id: string;
  shortCode: string;
  doctorId: string;
  doctorName: string;
  patientName: string;
  patientDateOfBirth: string;
  patientAge: number;
  patientPhoneNumber: string | null;
  issuedDate: string;
  status: PrescriptionStatus;
  itemCount: number;
  hasAwaitingRefillItems?: boolean;
}

export interface PrescriptionDetailsDto {
  id: string;
  shortCode: string;
  doctorId: string;
  doctorName: string;
  patientName: string;
  patientDateOfBirth: string;
  patientAge: number;
  patientPhoneNumber: string | null;
  diagnosis: string | null;
  issuedDate: string;
  status: PrescriptionStatus;
  createdBy: string | null;
  createdAt: string;
  items: PrescriptionItemDto[];
  hasAwaitingRefillItems?: boolean;
}

/** Fallbacks so the UI works even if the API response predates the new fields. */
export function refillsRemainingOf(item: PrescriptionItemDto): number {
  if (typeof item.refillsRemaining === 'number') return item.refillsRemaining;
  return Math.max(0, item.refillsAllowed - item.refillsUsed);
}

export function canRequestRefillOf(item: PrescriptionItemDto): boolean {
  if (typeof item.canRequestRefill === 'boolean') return item.canRequestRefill;
  return (
    item.isRefillable &&
    item.dispensedQuantity >= item.prescribedQuantity &&
    item.refillsUsed < item.refillsAllowed
  );
}

export function isAwaitingRefillOf(item: PrescriptionItemDto): boolean {
  if (typeof item.isAwaitingRefillDispense === 'boolean') return item.isAwaitingRefillDispense;
  return item.isRefillable && item.refillsUsed > 0 && item.dispensedQuantity < item.prescribedQuantity;
}

export function hasAwaitingRefillOf(p: PrescriptionListItemDto | PrescriptionDetailsDto): boolean {
  if (typeof p.hasAwaitingRefillItems === 'boolean') return p.hasAwaitingRefillItems;
  return (
    'items' in p &&
    Array.isArray(p.items) &&
    p.items.some((item) => isAwaitingRefillOf(item))
  );
}

export interface PatientMedicationItemDto {
  prescriptionItemId: string;
  medicineVariantId: string;
  medicineId: string;
  medicineName: string;
  medicineNameAr?: string;
  variantName: string;
  form: string;
  unit: string;
  strength: number;
  dosageInstructions: string | null;
  prescribedQuantity: number;
  dispensedQuantity: number;
  remainingQuantity: number;
  isRefillable: boolean;
  refillsAllowed: number;
  refillsUsed: number;
  refillIntervalDays: number;
  lastDispensedAt: string | null;
  nextEligibleDate: string | null;
  isCurrentlyActive: boolean;
}

export interface PatientPrescriptionHistoryDto {
  id: string;
  issuedDate: string;
  status: PrescriptionStatus;
  itemCount: number;
  items: PatientMedicationItemDto[];
}
