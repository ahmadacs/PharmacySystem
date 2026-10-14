export interface DispenseRequest {
  shortCode: string;
  phoneNumber: string;
  notes: string;
}

export interface DispensePrescriptionResponse {
  id: string;
  requestedQuantity: number;
  dispensedQuantity: number;
  warnings: string[];
  items: DispensedItemDto[];
  nextShortCode: string | null;
}

export interface DispensedItemDto {
  prescriptionItemId: string;
  medicineName: string;
  medicineNameAr: string | null;
  variantName: string;
  form: number | null;
  unit: number | null;
  strength: number | null;
  prescribedQuantity: number;
  dispensedThisTime: number;
  totalDispensedQuantity: number;
  isRefillable: boolean;
  refillsUsed: number;
  refillsRemaining: number;
  nextRefillEligibleDate: string | null;
}

export interface DispensingLookupItemDto {
  prescriptionItemId: string;
  medicineName: string;
  medicineNameAr: string | null;
  variantName: string;
  form: number | null;
  unit: number | null;
  strength: number | null;
  dosageInstructions: string | null;
  prescribedQuantity: number;
  dispensedQuantity: number;
  remainingQuantity: number;
  availableQuantity: number;
  isRefillable: boolean;
  refillsAllowed: number;
  refillsUsed: number;
  refillsRemaining: number;
  refillIntervalDays: number;
  nextRefillEligibleDate: string | null;
  canRequestRefill: boolean;
}

export interface DispensingLookupResponse {
  prescriptionId: string;
  shortCode: string;
  patientName: string;
  issuedDate: string;
  status: string;
  items: DispensingLookupItemDto[];
}

export interface DispensingRecordItemDto {
  medicineBatchId: string;
  medicineName: string;
  medicineNameAr: string | null;
  variantName: string;
  form: number | null;
  unit: number | null;
  strength: number | null;
  batchNumber: string;
  quantity: number;
}

export interface DispensingRecordDto {
  id: string;
  prescriptionId: string;
  patientName: string;
  pharmacistId: string;
  pharmacistName: string;
  dispensedAt: string;
  notes: string;
  items: DispensingRecordItemDto[];
}
