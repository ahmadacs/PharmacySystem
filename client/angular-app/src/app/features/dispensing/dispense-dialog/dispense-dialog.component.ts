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
    void this.prescriptionsService
      .get(this.prescriptionId)
      .then((details) => this.prescription.set(details))
      .catch(() => this.error.set(true));
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
    const due = new Date(y, m - 1, d + item.refillIntervalDays);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const diff = Math.ceil((due.getTime() - today.getTime()) / 86400000);
    return diff > 0 ? diff : null;
  }

  async dispense(id: string): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    try {
      const result = await this.dispensingService.dispense({
        prescriptionId: id,
        notes: this.notes.value.trim()
      });
      // Warnings force visibility: show the result panel instead of closing.
      // Clean success keeps the previous fast flow (toast + close).
      if (result.warnings.length > 0) {
        this.dispenseResult.set(result);
      } else {
        this.toast.show('Prescription dispensed.', 'success');
        this.dialogRef.close(true);
      }
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting.set(false);
    }
  }
}