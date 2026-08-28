import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatProgressBar } from '@angular/material/progress-bar';
import { PrescriptionDetailsDto } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { PrescriptionsService } from '../../prescriptions/prescriptions.service';
import { DispensingService } from '../dispensing.service';

@Component({
  selector: 'app-dispense-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, MatFormField, MatInput, MatLabel, MatButton, MatProgressBar, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose],
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

  async dispense(id: string): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    try {
      await this.dispensingService.dispense({
        prescriptionId: id,
        notes: this.notes.value.trim()
      });
      this.toast.show('Prescription dispensed.', 'success');
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting.set(false);
    }
  }
}