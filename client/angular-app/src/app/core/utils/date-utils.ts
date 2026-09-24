/** Shared date helpers (single source of truth, replaces duplicated copies in dialogs). */
export function startOfDay(value: Date): Date {
  return new Date(Date.UTC(value.getFullYear(), value.getMonth(), value.getDate()));
}

export function toDateString(date: Date | null): string | null {
  if (!date) {
    return null;
  }
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** Days from today (midnight) until a yyyy-MM-dd date string; null when past/invalid. */
export function daysUntil(dateStr: string | null | undefined): number | null {
  if (!dateStr) return null;
  const [y, m, d] = dateStr.split('-').map(Number);
  if (!y || !m || !d) return null;
  return daysFromToday(new Date(y, m - 1, d));
}

/** Days from today (midnight) until a Date; null when past. */
export function daysFromToday(due: Date): number | null {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const diff = Math.ceil((due.getTime() - today.getTime()) / 86400000);
  return diff > 0 ? diff : null;
}
