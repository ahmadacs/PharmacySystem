import { AuditChangeDto } from '../../core/models/api.models';

/** Change fields promoted out of the Changes table into Summary / Event Info. */
export const AUDIT_PROMOTED_PROPS = ['Reason', 'Type'];

/** Quantity fields whose creation (null → N) reads as a "+N" headline. */
const SINGLE_QTY_PROPS = new Set([
  'QuantityReceived',
  'QuantityAvailable',
  'Quantity',
  'PrescribedQuantity',
  'DispensedQuantity',
]);

/** Numeric headline for the Event Summary, e.g. +300 units (0 → 300). */
export interface AuditHeadline {
  delta: number;
  beforeText: string;
  afterText: string;
}

export function shortAuditId(id: string): string {
  return id.replaceAll('-', '').slice(0, 8).toUpperCase();
}

export function isGuidValue(value: string): boolean {
  const compact = value.replaceAll('-', '');
  return value.includes('-') && /^[0-9a-fA-F]{32}$/.test(compact);
}

function toAuditNumber(value: string | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

/** Rows for the Changes table (promoted Reason/Type live in Summary/Info). */
export function auditTableRows(changes: AuditChangeDto[]): AuditChangeDto[] {
  return changes.filter((c) => !AUDIT_PROMOTED_PROPS.includes(c.property));
}

/** Reason surfaced in Event Information (raw text, never enum-translated). */
export function auditReasonText(changes: AuditChangeDto[]): string | null {
  const reason = changes.find((c) => c.property === 'Reason');
  return reason?.newValue ?? reason?.oldValue ?? null;
}

/**
 * Derives a numeric headline from quantity changes (presentation only):
 * a Before/After pair first, then a direct QuantityChanged delta, then a
 * created single quantity (null → N reads as 0 → N). Null otherwise.
 */
export function auditHeadline(changes: AuditChangeDto[]): AuditHeadline | null {
  const byProp = new Map(changes.map((c) => [c.property, c]));

  for (const c of changes) {
    if (!c.property.endsWith('Before')) continue;
    const after = byProp.get(`${c.property.slice(0, -'Before'.length)}After`);
    if (!after) continue;
    const beforeNum = toAuditNumber(c.newValue ?? c.oldValue) ?? 0;
    const afterNum = toAuditNumber(after.newValue ?? after.oldValue);
    if (afterNum === null || afterNum === beforeNum) return null;
    return {
      delta: afterNum - beforeNum,
      beforeText: String(beforeNum),
      afterText: String(afterNum),
    };
  }

  const directProp = byProp.get('QuantityChanged');
  const direct = toAuditNumber(directProp?.newValue ?? directProp?.oldValue);
  if (direct !== null && direct !== 0)
    return { delta: direct, beforeText: '—', afterText: String(direct) };

  for (const c of changes) {
    const base = c.property.endsWith('.Value')
      ? c.property.slice(0, -'.Value'.length)
      : c.property;
    if (c.oldValue || !SINGLE_QTY_PROPS.has(base)) continue;
    const created = toAuditNumber(c.newValue);
    if (created === null || created === 0) continue;
    return { delta: created, beforeText: '0', afterText: String(created) };
  }

  return null;
}

export function auditDeltaText(h: AuditHeadline): string {
  return `${h.delta > 0 ? '+' : ''}${h.delta}`;
}
