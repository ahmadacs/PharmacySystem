import { Injectable, inject, signal } from '@angular/core';
import { FoundPatient, PatientLookupService } from '../patient-lookup.service';

export type PatientState = { kind: 'none' } | { kind: 'found'; patient: FoundPatient } | { kind: 'new' };

/**
 * Manual phone lookup state. The dialog calls `search()` from an explicit
 * Search button — no auto-tracking, no debounce, no hint stream.
 * Dialog-scoped (provided in the dialog component).
 */
@Injectable()
export class PatientPhoneSearchService {
  private readonly lookup = inject(PatientLookupService);

  readonly patientState = signal<PatientState>({ kind: 'none' });
  readonly searching = signal(false);
  private seq = 0;

  /** Explicit lookup of a full phone (+9665...). Ignores incomplete numbers. */
  async search(fullPhone: string): Promise<void> {
    if (!this.lookup.isCompletePhone(fullPhone)) {
      return;
    }
    const seq = ++this.seq;
    this.searching.set(true);
    try {
      const patient = await this.lookup.findByPhone(fullPhone);
      if (seq !== this.seq) {
        return;
      }
      this.patientState.set(patient ? { kind: 'found', patient } : { kind: 'new' });
    } finally {
      if (seq === this.seq) {
        this.searching.set(false);
      }
    }
  }

  /** Clears the lookup (called when the user edits the phone after a result). */
  reset(): void {
    this.seq++;
    this.searching.set(false);
    this.patientState.set({ kind: 'none' });
  }
}
