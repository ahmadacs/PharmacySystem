import { TranslateService } from '@ngx-translate/core';

/**
 * Shared 3-step dictionary lookup (single source of truth).
 * Tries camelCase, lowercase, then original: Status.Active -> status.active, etc.
 * Used by EnumTranslatePipe and AuditLogDetailsDialogComponent.
 */
export function translateLookup(translate: TranslateService, prefix: string, name: string): string | null {
  const candidates = [
    `${prefix}.${name.charAt(0).toLowerCase() + name.slice(1)}`,
    `${prefix}.${name.toLowerCase()}`,
    `${prefix}.${name}`,
  ];
  for (const key of candidates) {
    const translated = translate.instant(key);
    if (translated !== key) return translated;
  }
  return null;
}
