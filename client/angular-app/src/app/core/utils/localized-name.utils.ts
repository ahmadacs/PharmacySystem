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
