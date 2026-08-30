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

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string | null;
  role: string | null;
  permissions: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: CurrentUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ErrorEnvelope {
  success: boolean;
  message: string | null;
  errors: Record<string, string[]> | null;
  traceId: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type NotificationType = 'LowStock' | 'NearExpiry' | 'PrescriptionCreated' | 'PrescriptionDispensed';

export interface NotificationDto {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  data: string | null;
  localizationKey: string | null;
  localizationParamsJson: string | null;
  isRead: boolean;
  createdAt: string;
}

export type PrescriptionStatus =
  | 'Pending'
  | 'PartiallyDispensed'
  | 'FullyDispensed'
  | 'Cancelled'
  | 'Expired';

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

export type UserRole = 'Admin' | 'Pharmacist' | 'Doctor';

export interface MedicineVariantRequest {
  form: MedicineForm;
  unit: string;
  strength: number | null;
  baseUnitName: string;
  packageUnitName: string;
  unitsPerPackage: number;
  isDivisible: boolean;
}

export interface CreateMedicineRequest {
  name: string;
  genericName: string;
  category: number;
  reorderLevel: number;
  isControlled: boolean;
  variants: MedicineVariantRequest[];
}

export interface UpdateMedicineRequest {
  id: string;
  name: string;
  genericName: string;
  category: number;
  reorderLevel: number;
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
  reorderLevel: number;
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
  reorderLevel: number;
  availableQuantity: number;
  variants: MedicineVariantDto[];
}

export interface CategoryDto {
  id: number;
  name: string;
  nameAr?: string;
}

export interface PrescriptionItemRequest {
  medicineVariantId: string;
  quantity: number;
  dosageInstructions?: string;
}

export interface CreatePrescriptionRequest {
  patientFirstName: string;
  patientLastName: string;
  patientDateOfBirth: string;
  patientPhoneNumber?: string;
  diagnosis?: string;
  issuedDate: string;
  isRefillable: boolean;
  refillsAllowed: number;
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
  isRefillable: boolean;
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
  isRefillable: boolean;
  refillsAllowed: number;
  refillsUsed: number;
  createdBy: string | null;
  createdAt: string;
  items: PrescriptionItemDto[];
}

export interface DispenseRequest {
  prescriptionId: string;
  notes: string;
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

export interface LowStockDto {
  medicineId: string;
  name: string;
  nameAr?: string;
  availableQuantity: number;
  reorderLevel: number;
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

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: UserRole;
  licenseNumber?: string;
  specialization?: string;
  phoneNumber?: string;
}

export interface UserDto {
  id: string;
  email: string;
  fullName: string | null;
  isActive: boolean;
  roles: string[];
}

export type AuditAction = 'Created' | 'Updated' | 'Deleted';

export interface AuditChangeDto {
  property: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface AuditEntryDto {
  id: string;
  entityName: string;
  entityId: string;
  action: AuditAction;
  changedBy: string | null;
  changedByName: string | null;
  changedAt: string;
  changes: AuditChangeDto[];
}