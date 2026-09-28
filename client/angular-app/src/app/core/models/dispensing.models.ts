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
