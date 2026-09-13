import { MedicineForm } from './medicines.models';

export type InventoryAdjustmentType =
  | 'Increase'
  | 'Decrease'
  | 'Correction'
  | 'Damaged'
  | 'Expired'
  | 'Returned'
  | 'Sold'
  | 'TransferOut'
  | 'TransferIn';

export enum InventoryAdjustmentTypeEnum {
  Increase = 1,
  Decrease = 2,
  Correction = 3,
  Damaged = 4,
  Expired = 5,
  Returned = 6,
  Sold = 7,
  TransferOut = 8,
  TransferIn = 9
}

export interface LowStockDto {
  medicineId: string;
  medicineName: string;
  medicineNameAr?: string;
  medicineVariantId: string;
  variantName: string;
  availableQuantity: number;
  reorderLevel: number;
  form: MedicineForm;
  unit: string;
  strength: number | null;
}

export interface FileUploadDto {
  fileName: string;
  contentType: string;
  sizeBytes: number;
  base64Content: string;
}

export interface AdjustInventoryRequest {
  medicineBatchId: string;
  type: InventoryAdjustmentType;
  quantity: number;
  reason: string;
  file?: FileUploadDto;
}

export interface ReceiveInventoryRequest {
  medicineVariantId: string;
  manufactureDate: string;
  expiryDate: string;
  packagesReceived: number;
  unitCost: number;
  supplierName: string | null;
  reason: string;
  adjustmentType: InventoryAdjustmentTypeEnum;
  file?: FileUploadDto;
}

export interface InventoryAdjustmentDto {
  id: string;
  medicineBatchId: string;
  medicineName: string;
  medicineNameAr?: string;
  variantName: string;
  batchNumber: string;
  type: InventoryAdjustmentType;
  quantityChanged: number;
  quantityBefore: number;
  quantityAfter: number;
  reason: string;
  adjustedBy: string | null;
  adjustedByName: string | null;
  adjustedAt: string;
}

export type StockStatus = 'InStock' | 'LowStock' | 'OutOfStock';

export interface MedicineInventorySummaryDto {
  id: string;
  name: string;
  nameAr?: string;
  genericName: string;
  genericNameAr?: string;
  variantCount: number;
  totalQuantity: number;
  reorderLevel: number;
  stockStatus: StockStatus;
  nearestExpiryDate: string | null;
  activeBatchCount: number;
}

export type ExpiryStatus = 'Critical' | 'Warning' | 'Safe' | 'Expired';

export interface ExpiryAlertDto {
  batchId: string;
  medicineName: string;
  medicineNameAr?: string;
  variantName: string;
  batchNumber: string;
  expiryDate: string;
  daysRemaining: number;
  remainingQuantity: number;
  status: ExpiryStatus;
}
