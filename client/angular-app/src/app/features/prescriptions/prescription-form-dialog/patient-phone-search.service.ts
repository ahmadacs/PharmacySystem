import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, debounceTime, distinctUntilChanged, map, tap } from 'rxjs';
import {
  FoundPatient,
  PatientLookupService,
  SAUDI_PHONE_PATTERN,
  computePhoneHintKey,
} from '../patient-lookup.service';

export type PatientState = { kind: 'none' } | { kind: 'found'; patient: FoundPatient } | { kind: 'new' };

/**
 * Phone-first patient lookup state machine, extracted from
 * PrescriptionFormDialogComponent. A pure signal producer: it owns the input
 * pipeline and exposes patientState/searching/hintKey — the dialog consumes
 * them with effects (no callbacks), consistent with the signals-based style
 * of the rest of the code. Testable with a mocked PatientLookupService.
 *
 * Dialog-scoped: provided in PrescriptionFormDialogComponent (one instance per
 * open dialog), so its DestroyRef completes the pipeline on dialog close.
 */
@Injectable()
export class PatientPhoneSearchService {
  private readonly lookup = inject(PatientLookupService);
  private readonly destroyRef = inject(DestroyRef);

  readonly patientState = signal<PatientState>({ kind: 'none' });
  readonly searching = signal(false);
  readonly hintKey = signal<string | null>(null);
  private seq = 0;

  /** Wire once from the dialog constructor. */
  track(phoneChanges: Observable<string>): void {
    phoneChanges
      .pipe(
        map((val) => (val ?? '').trim()),
        distinctUntilChanged(),
        tap((phone) => this.onPhoneInput(phone)),
        debounceTime(400),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((phone) => {
        if (!this.lookup.isCompletePhone(phone)) {
          return;
        }
        void this.resolve(phone);
      });
  }

  /**
   * Runs synchronously on every phone keystroke: voids the previous lookup
   * (stale names/DOB/meds can never stick to a new number) and updates the
   * hint/searching indicator.
   *
   * Emission contract: `none` is emitted ONLY on a genuine kind change
   * (leaving found/new). Keystrokes while already unresolved emit nothing, so
   * consumers observe exactly one synchronous emission per real transition —
   * leaving found always lands on none exactly once.
   */
  private onPhoneInput(phone: string): void {
    // A new keystroke cancels any in-flight lookup (see resolve).
    this.seq++;
    if (this.patientState().kind !== 'none') {
      this.patientState.set({ kind: 'none' });
    }
    this.hintKey.set(null);
    if (!SAUDI_PHONE_PATTERN.test(phone)) {
      this.searching.set(false);
      this.hintKey.set(computePhoneHintKey(phone));
      return;
    }
    this.hintKey.set(null);
    // Keep the rest visible with a spinner while debouncing/resolving.
    this.searching.set(true);
  }

  private async resolve(phone: string): Promise<void> {
    const seq = ++this.seq;
    this.searching.set(true);
    try {
      const patient = await this.lookup.findByPhone(phone);
      // A newer keystroke invalidated this lookup while it was in flight.
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
}
