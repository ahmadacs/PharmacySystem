import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { CategoryEnum, MedicineForm, MedicineUnit } from '../../core/models/api.models';

const PREFIX_MAP: Record<string, string> = {
  CategoryEnum: 'dictionary.categories',
  MedicineForm: 'dictionary.forms',
  MedicineUnit: 'dictionary.units',
  PrescriptionStatus: 'prescriptions.statuses',
  StockStatus: 'inventory.stockStatus',
  ExpiryStatus: 'inventory.alertStatuses',
  BatchExpiryStatus: 'inventory.expiryStatuses',
  InventoryAdjustmentType: 'inventory.adjustmentTypes',
  BatchStatus: 'inventory.chips',
  UserRole: 'users.roles',
  AuditAction: 'auditLog.actions',
  EntityName: 'auditLog.entities',
  AuditProperty: 'auditLog.properties',
};

const ENUM_OBJECTS: Record<string, any> = {
  CategoryEnum,
  MedicineForm,
  MedicineUnit,
};

function toTranslationKey(enumName: string): string {
  if (!enumName) return enumName;
  // Handle special unit symbol
  if (enumName === '%') return 'percent';
  // Lowercase first char only to preserve camelCase: InStock -> inStock, Analgesics -> analgesics
  return enumName.charAt(0).toLowerCase() + enumName.slice(1);
}

@Pipe({
  name: 'enumTranslate',
  standalone: true,
  pure: false,
})
export class EnumTranslatePipe implements PipeTransform {
  private readonly translate = inject(TranslateService);

  transform(value: number | string | null | undefined, enumType: string): string {
    if (value === null || value === undefined || value === '') return '';

    let enumName: string;

    if (typeof value === 'number') {
      const enumObj = ENUM_OBJECTS[enumType];
      if (enumObj) {
        enumName = enumObj[value] ?? String(value);
      } else {
        enumName = String(value);
      }
    } else {
      enumName = String(value);
    }

    const prefix = PREFIX_MAP[enumType] ?? `enums.${enumType}`;
    const keyPart = toTranslationKey(enumName);
    const fullKey = `${prefix}.${keyPart}`;

    // Also try lowercased full version as fallback (for units like Mg -> mg)
    const lowerKey = `${prefix}.${enumName.toLowerCase()}`;

    let translated = this.translate.instant(fullKey);
    if (translated !== fullKey) return translated;

    translated = this.translate.instant(lowerKey);
    if (translated !== lowerKey) return translated;

    // Final fallback: try original case
    const originalKey = `${prefix}.${enumName}`;
    translated = this.translate.instant(originalKey);
    if (translated !== originalKey) return translated;

    return enumName;
  }
}
