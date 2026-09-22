import { Component, Signal, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MAT_DIALOG_DATA, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';
import { FoundPatient } from '../patient-lookup.service';
import { PatientMedicationItemDto, PatientPrescriptionHistoryDto } from '../../../core/models/api.models';
import { MedsHistoryListComponent, MedsLookback } from '../meds-history-list/meds-history-list.component';

export interface PatientDetailsDialogData {
  patient: FoundPatient;
  /** The form's signals, shared by reference — one fetch serves both views. */
  history: Signal<PatientPrescriptionHistoryDto[]>;
  loading: Signal<boolean>;
  lookback: Signal<MedsLookback>;
  lookbackOptions: MedsLookback[];
  onAdd: (item: PatientMedicationItemDto) => void;
  onLookbackChange: (days: MedsLookback) => void;
}

/**
 * Doctor-facing patient snapshot: identity + concise prescribing signals
 * (last visit, last dispense, active meds) with the full medication history
 * below. Purely presentational — history state is owned by the prescription
 * form and read here via dialog data, so a lookback change fetches once and
 * both views update. Same MatDialog pattern as the other dialogs.
 */
@Component({
  selector: 'app-patient-details-dialog',
  standalone: true,
  imports: [
    TranslatePipe,
    MedsHistoryListComponent,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatIcon,
    MatProgressBar,
  ],
  templateUrl: './patient-details-dialog.component.html',
  styleUrl: './patient-details-dialog.component.scss',
})
export class PatientDetailsDialogComponent {
  readonly data = inject<PatientDetailsDialogData>(MAT_DIALOG_DATA);

  protected readonly expanded = signal(true);
  protected readonly prescriptionCount = computed(() => this.data.history().length);
  protected readonly activeCount = computed(() =>
    this.data.history().reduce((n, p) => n + p.items.filter((i) => i.isCurrentlyActive).length, 0),
  );
  protected readonly lastVisit = computed<string | null>(() => {
    let latest: string | null = null;
    for (const p of this.data.history()) {
      if (!latest || p.issuedDate > latest) latest = p.issuedDate;
    }
    return latest;
  });
  protected readonly lastDispense = computed<string | null>(() => {
    let latest: string | null = null;
    for (const p of this.data.history()) {
      for (const i of p.items) {
        if (i.lastDispensedAt && (!latest || i.lastDispensedAt > latest)) latest = i.lastDispensedAt;
      }
    }
    return latest;
  });

  protected patientAge(): number | null {
    const dob = this.data.patient.dateOfBirth;
    if (!dob) return null;
    const birth = new Date(dob);
    if (Number.isNaN(birth.getTime())) return null;
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    const m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--;
    return age >= 0 ? age : null;
  }
}
