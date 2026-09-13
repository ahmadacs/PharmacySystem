export interface DispenseRequest {
  prescriptionId: string;
  notes: string;
}

export interface DispensePrescriptionResponse {
  id: string;
  requestedQuantity: number;
  dispensedQuantity: number;
  warnings: string[];
}

export interface DispensingRecordItemDto {
  medicineBatchId: string;
  medicineName: string;
  variantName: string;
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
