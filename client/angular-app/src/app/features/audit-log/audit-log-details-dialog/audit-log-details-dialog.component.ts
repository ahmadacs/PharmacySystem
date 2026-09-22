import { Component, inject } from '@angular/core';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuditChangeDto, AuditEntryDto } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';
import {
  AuditHeadline,
  auditDeltaText,
  auditHeadline,
  auditReasonText,
  auditTableRows,
  isGuidValue,
  shortAuditId,
} from '../audit-display.utils';

/** Dictionaries tried (in order) when translating an audit change value. */
const VALUE_PREFIXES = [
  'prescriptions.statuses',
  'inventory.adjustmentTypes',
  'dictionary.forms',
  'dictionary.units',
  'dictionary.categories',
];

@Component({
  selector: 'app-audit-log-details-dialog',
  standalone: true,
  imports: [
    TranslatePipe,
    EnumTranslatePipe,
    RiyadhDatePipe,
    MatButton,
    MatIconButton,
    MatIcon,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
  ],
  templateUrl: './audit-log-details-dialog.component.html',
  styleUrl: './audit-log-details-dialog.component.scss',
})
export class AuditLogDetailsDialogComponent {
  protected readonly row = inject<AuditEntryDto>(MAT_DIALOG_DATA);

  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  /** Hero title: adjustment type when present (e.g. "Increase"), else the action. */
  protected summaryTitle(): string {
    const type = this.row.changes.find((c) => c.property === 'Type');
    const typeValue = type?.newValue ?? type?.oldValue;
    if (typeValue) return this.valueLabel(typeValue);
    return this.lookup('auditLog.actions', this.row.action) ?? this.row.action;
  }

  protected tableRows(): AuditChangeDto[] {
    return auditTableRows(this.row.changes);
  }

  /** Hides Before when nothing has a previous value (e.g. creation). */
  protected hideBefore(rows: AuditChangeDto[]): boolean {
    return rows.length > 0 && rows.every((c) => !c.oldValue);
  }

  protected reasonText(): string | null {
    return auditReasonText(this.row.changes);
  }

  protected headline(): AuditHeadline | null {
    return auditHeadline(this.row.changes);
  }

  protected deltaText(h: AuditHeadline): string {
    return auditDeltaText(h);
  }

  protected shortId(id: string): string {
    return shortAuditId(id);
  }

  /** Shortens raw GUID change-values (FK ids) to #XXXXXXXX; leaves real values untouched. */
  protected displayValue(value: string | null): string {
    if (!value) return '—';
    if (isGuidValue(value)) return `#${value.replaceAll('-', '').slice(0, 8).toUpperCase()}`;
    return value;
  }

  /**
   * Translates a change property name. Complex-type properties arrive dotted
   * from the backend (e.g. "QuantityReceived.Value"), which ngx-translate
   * would read as a nested path — so each segment is resolved separately.
   */
  protected propertyLabel(property: string): string {
    const [head, ...rest] = property.split('.');
    let label = this.lookup(`auditLog.properties`, head) ?? head;
    if (rest.length > 0) {
      const tail = rest.join('.');
      label = `${label} · ${this.lookup(`auditLog.properties`, tail) ?? tail}`;
    }
    return label;
  }

  /**
   * Translates a change value: a server-resolved display name wins (e.g. the
   * patient behind a PatientId); otherwise enum names via their dictionary
   * (statuses, adjustment types, forms, units, categories) and booleans via
   * auditLog.values; anything else (dates, numbers, text) passes through.
   */
  protected valueLabel(value: string | null, display: string | null = null): string {
    if (display) return display;
    const short = this.displayValue(value);
    if (short === '—' || short.startsWith('#') || value === null) return short;
    if (value === 'true' || value === 'false')
      return this.translate.instant(`auditLog.values.${value}`);
    for (const prefix of VALUE_PREFIXES) {
      const hit = this.lookup(prefix, value);
      if (hit !== null) return hit;
    }
    return short;
  }

  /** Same 3-step lookup as EnumTranslatePipe: camelCase, lowercase, original. */
  private lookup(prefix: string, name: string): string | null {
    const candidates = [
      `${prefix}.${name.charAt(0).toLowerCase() + name.slice(1)}`,
      `${prefix}.${name.toLowerCase()}`,
      `${prefix}.${name}`,
    ];
    for (const key of candidates) {
      const translated = this.translate.instant(key);
      if (translated !== key) return translated;
    }
    return null;
  }

  protected async copyId(id: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(id);
      this.toast.show(this.translate.instant('auditLog.copied'), 'success', 2000);
    } catch {
      this.toast.show(id, 'info', 4000);
    }
  }
}
