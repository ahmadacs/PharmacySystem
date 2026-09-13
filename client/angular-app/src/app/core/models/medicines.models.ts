export enum MedicineForm {
  Tablet = 1,
  Capsule = 2,
  Syrup = 3,
  Injection = 4,
  Ointment = 5,
  Drops = 6,
  Inhaler = 7,
  Suspension = 8,
  Solution = 9,
  Cream = 10,
  Gel = 11,
  Powder = 12,
  Spray = 13,
  Suppository = 14,
  Patch = 15,
  Lozenges = 16,
  Chewable = 17,
  Effervescent = 18,
  Granules = 19,
  Emulsion = 20,
  Lotion = 21,
  Other = 99
}

export enum MedicineUnit {
  Mg = 1,
  Ml = 2,
  G = 3,
  Tablet = 4,
  Capsule = 5,
  Drop = 6,
  Vial = 7,
  Ampoule = 8,
  Sachet = 9,
  Patch = 10,
  Spray = 11,
  Suppository = 12,
  Iu = 13,
  Percent = 14,
  Other = 99
}

export enum CategoryEnum {
  Analgesics = 1,
  Antibiotics = 2,
  Antipyretics = 3,
  Anticoagulants = 4,
  Antihistamines = 5,
  Cardiovascular = 6,
  Diabetic = 7,
  Antidiabetics = 8,
  Respiratory = 9,
  Other = 10
}

export interface MedicineVariantRequest {
  form: MedicineForm;
  unit: string;
  strength: number | null;
  reorderLevel: number;
  baseUnitName: string;
  packageUnitName: string;
  unitsPerPackage: number;
  isDivisible: boolean;
}

export interface CreateMedicineRequest {
  name: string;
  nameAr?: string;
  genericName: string;
  genericNameAr?: string;
  category: number;
  isControlled: boolean;
  variants: MedicineVariantRequest[];
}

export interface UpdateMedicineRequest {
  id: string;
  name: string;
  nameAr?: string;
  genericName: string;
  genericNameAr?: string;
  category: number;
  isControlled: boolean;
  isActive: boolean;
}

export interface CreateBatchRequest {
  medicineVariantId: string;
  manufactureDate: string;
  expiryDate: string;
  packagesReceived: number;
  unitCost: number;
  supplierName?: string;
}

export interface MedicineVariantDto {
  id: string;
  medicineId: string;
  form: MedicineForm;
  unit: string;
  strength: number | null;
  displayName: string;
  isActive: boolean;
  availableQuantity: number;
  reorderLevel: number;
  isLowStock: boolean;
  baseUnitName: string;
  packageUnitName: string;
  unitsPerPackage: number;
  isDivisible: boolean;
  batches: MedicineBatchDto[];
}

export interface MedicineBatchDto {
  id: string;
  medicineVariantId: string;
  medicineName: string;
  medicineNameAr?: string;
  variantName: string;
  batchNumber: string;
  manufactureDate: string;
  expiryDate: string;
  quantityReceived: number;
  quantityAvailable: number;
  unitCost: number;
  supplierName: string | null;
  isExpired: boolean;
  daysToExpiry: number | null;
  batchStatus: string;
  receivedDate: string;
}

export interface MedicineVariantSummaryDto {
  id: string;
  form: MedicineForm;
  unit: string;
  strength: number | null;
  displayName: string;
  availableQuantity: number;
  reorderLevel: number;
  isLowStock: boolean;
  baseUnitName: string;
  packageUnitName: string;
  unitsPerPackage: number;
  isDivisible: boolean;
}

export interface MedicineListItemDto {
  id: string;
  name: string;
  nameAr?: string;
  genericName: string;
  genericNameAr?: string;
  category: number;
  categoryAr?: string;
  variants: MedicineVariantSummaryDto[];
  isControlled: boolean;
  isActive: boolean;
  availableQuantity: number;
  variantCount: number;
  isLowStock: boolean;
}

export interface MedicineDetailsDto {
  id: string;
  name: string;
  nameAr?: string;
  genericName: string;
  genericNameAr?: string;
  category: number;
  categoryAr?: string;
  isControlled: boolean;
  isActive: boolean;
  availableQuantity: number;
  variants: MedicineVariantDto[];
}

export interface CategoryDto {
  id: number;
  name: string;
  nameAr?: string;
}
