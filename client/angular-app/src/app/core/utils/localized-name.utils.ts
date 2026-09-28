import { TranslateService } from '@ngx-translate/core';

/**
 * Single place that knows how the current language is exposed by
 * @ngx-translate/core v18 (signal in v18, plain string in older versions).
 * Replaces the `as any` casts scattered across inventory/medicine dialogs.
 */
export function currentLanguage(translate: TranslateService): string {
  const raw: unknown = (translate as { currentLang: unknown }).currentLang;
  return typeof raw === 'function' ? (raw as () => string)() : String(raw ?? 'en');
}

export function isArabicLang(translate: TranslateService): boolean {
  return currentLanguage(translate) === 'ar';
}

interface LocalizableName {
  name: string;
  nameAr?: string | null;
}

interface LocalizableGenericName {
  genericName: string;
  genericNameAr?: string | null;
}

type SearchableMedicine = LocalizableName & LocalizableGenericName;

interface LocalizableMedicineName {
  medicineName: string;
  medicineNameAr?: string | null;
}

export function pickLocalizedName(row: LocalizableName, translate: TranslateService): string {
  return isArabicLang(translate) && row.nameAr ? row.nameAr : row.name;
}

export function pickLocalizedGenericName(row: LocalizableGenericName, translate: TranslateService): string {
  return isArabicLang(translate) && row.genericNameAr ? row.genericNameAr : row.genericName;
}

export function pickLocalizedMedicineName(row: LocalizableMedicineName, translate: TranslateService): string {
  return isArabicLang(translate) && row.medicineNameAr ? row.medicineNameAr : row.medicineName;
}

interface LocalizableVariant {
  form: number | null;
  unit: number | null;
  strength: number | null;
}

function enumKey(value: number | null | undefined, enumObj: Record<number, string>): string {
  if (value === null || value === undefined) {
    return '';
  }
  const name = enumObj[value] ?? '';
  if (!name) {
    return '';
  }
  return name.charAt(0).toLowerCase() + name.slice(1);
}

function lookupEnumLabel(prefix: string, key: string, translate: TranslateService): string {
  if (!key) {
    return '';
  }
  const fullKey = `${prefix}.${key}`;
  const translated = translate.instant(fullKey);
  if (translated !== fullKey) {
    return translated;
  }
  const lowerKey = `${prefix}.${key.toLowerCase()}`;
  const lowerTranslated = translate.instant(lowerKey);
  return lowerTranslated !== lowerKey ? lowerTranslated : key;
}

export function localizedVariantName(
  row: LocalizableVariant,
  formEnum: Record<number, string>,
  unitEnum: Record<number, string>,
  translate: TranslateService
): string {
  const parts = [
    lookupEnumLabel('dictionary.forms', enumKey(row.form, formEnum), translate),
    row.strength ?? '',
    lookupEnumLabel('dictionary.units', enumKey(row.unit, unitEnum), translate)
  ].filter((part) => part !== '' && part !== null && part !== undefined);
  return parts.join(' ');
}

export function filterMedicinesByName<T extends SearchableMedicine>(medicines: T[], search: string): T[] {
  const term = search.trim().toLowerCase();
  if (!term) {
    return medicines;
  }
  return medicines.filter(
    (medicine) =>
      medicine.name.toLowerCase().includes(term) ||
      medicine.nameAr?.toLowerCase().includes(term) ||
      medicine.genericName.toLowerCase().includes(term) ||
      medicine.genericNameAr?.toLowerCase().includes(term),
  );
}
