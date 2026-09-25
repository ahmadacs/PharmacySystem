import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressBar } from '@angular/material/progress-bar';
import { DispensePrescriptionResponse, PrescriptionDetailsDto, PrescriptionItemDto } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { daysFromToday } from '../../../core/utils/date-utils';
import { runSubmit } from '../../../core/utils/dialog-helpers';
import { reloadDetails } from '../../../core/utils/entity-helpers';
import { PrescriptionsService } from '../../prescriptions/prescriptions.service';
import { DispensingService } from '../dispensing.service';

@Component({
  selector: 'app-dispense-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, MatFormField, MatInput, MatLabel, MatButton, MatIcon, MatProgressBar, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose],
  templateUrl: './dispense-dialog.component.html',
  styleUrl: './dispense-dialog.component.scss'
})
export class DispenseDialogComponent {
  private readonly prescriptionsService = inject(PrescriptionsService);
  private readonly dispensingService = inject(DispensingService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject(MatDialogRef<DispenseDialogComponent>);

  readonly prescriptionId = inject<string>(MAT_DIALOG_DATA);
  protected readonly prescription = signal<PrescriptionDetailsDto | null>(null);
  protected readonly submitting = signal(false);
  protected readonly error = signal(false);
  protected readonly notes = new FormControl('', { nonNullable: true });
  protected readonly dispenseResult = signal<DispensePrescriptionResponse | null>(null);

  protected isPartialResult(r: DispensePrescriptionResponse): boolean {
    return r.dispensedQuantity < r.requestedQuantity;
  }

  constructor() {
    void this.load();
  }

  protected load(): void {
    this.error.set(false);
    this.prescription.set(null);
    void reloadDetails(
      this.prescription,
      () => this.prescriptionsService.get(this.prescriptionId),
      () => this.error.set(true)
    );
  }

  /**
   * Proactive UX hint only: days until the item may be dispensed again.
   * Returns null when unconstrained or already due. The real enforcement
   * lives server-side (409) — this never blocks the request.
   */
  protected dueInDays(item: PrescriptionItemDto): number | null {
    if (!item.refillIntervalDays || item.refillIntervalDays <= 0) return null;
    if (!item.lastDispensedAt || item.remainingQuantity <= 0) return null;
    const [y, m, d] = item.lastDispensedAt.split('-').map(Number);
    if (!y || !m || !d) return null;
    return daysFromToday(new Date(y, m - 1, d + item.refillIntervalDays));
  }

  async dispense(id: string): Promise<void> {
    await runSubmit(
      this.submitting,
      () =>
        this.dispensingService.dispense({
          prescriptionId: id,
          notes: this.notes.value.trim()
        }),
      (result) => {
        // Warnings force visibility: show the result panel instead of closing.
        // Clean success keeps the previous fast flow (toast + close).
        if (result.warnings.length > 0) {
          this.dispenseResult.set(result);
        } else {
          this.toast.show('Prescription dispensed.', 'success');
          this.dialogRef.close(true);
        }
      }
    );
  }
}