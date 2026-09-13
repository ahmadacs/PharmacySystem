export type PrescriptionStatus =
  | 'Pending'
  | 'PartiallyDispensed'
  | 'FullyDispensed'
  | 'Cancelled'
  | 'Expired';

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
}

export interface RefillPrescriptionRequest {
  itemIds: string[];
}

export interface PrescriptionListItemDto {
  id: string;
  doctorId: string;
  doctorName: string;
  patientName: string;
  patientDateOfBirth: string;
  patientAge: number;
  patientPhoneNumber: string | null;
  issuedDate: string;
  status: PrescriptionStatus;
  itemCount: number;
}

export interface PrescriptionDetailsDto {
  id: string;
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
