import { ChangeDetectorRef, DestroyRef, Pipe, PipeTransform, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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

@Pipe({
  name: 'enumTranslate',
  standalone: true,
  // Pure + explicit refresh on language change: avoids the per-CD cost of
  // pure:false while still updating when the user switches en<->ar.
  pure: true,
})
export class EnumTranslatePipe implements PipeTransform {
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    this.translate.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.cdr.markForCheck();
    });
  }

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
