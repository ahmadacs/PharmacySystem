import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { translateLookup } from '../../core/utils/translate-lookup';
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

type NumericEnumObject = Record<number, string>;

const ENUM_OBJECTS: Record<string, NumericEnumObject | undefined> = {
  CategoryEnum: CategoryEnum as unknown as NumericEnumObject,
  MedicineForm: MedicineForm as unknown as NumericEnumObject,
  MedicineUnit: MedicineUnit as unknown as NumericEnumObject,
};

function toTranslationKey(enumName: string): string {
  if (!enumName) return enumName;
  // Handle special unit symbol
  if (enumName === '%') return 'percent';
  // Lowercase first char only to preserve camelCase: InStock -> inStock, Analgesics -> analgesics
  return enumName.charAt(0).toLowerCase() + enumName.slice(1);
}

function toLookupName(enumName: string): string {
  return enumName === '%' ? 'percent' : enumName;
}

@Pipe({
  name: 'enumTranslate',
  standalone: true,
  // Must stay impure: a pure pipe caches by input, so markForCheck() on
  // language change can never bust the cache and labels would stay stuck in
  // the previous language. instant() is a cheap sync lookup, so per-CD cost
  // is negligible.
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

    // Single source of truth for the 3-step lookup (camelCase -> lower -> original).
    const translated = translateLookup(this.translate, prefix, toLookupName(enumName));
    if (translated !== null) return translated;

    // Legacy fallback for keys stored with an explicit lowercaseFirst transform.
    const legacyKey = `${prefix}.${toTranslationKey(enumName)}`;
    const legacy = this.translate.instant(legacyKey);
    if (legacy !== legacyKey) return legacy;

    return enumName;
  }
}
