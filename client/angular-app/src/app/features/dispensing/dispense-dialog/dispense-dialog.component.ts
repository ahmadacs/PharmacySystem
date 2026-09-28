import { Component, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressBar } from '@angular/material/progress-bar';
import { DispensePrescriptionResponse, MedicineForm, MedicineUnit } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { localizedVariantName, pickLocalizedMedicineName } from '../../../core/utils/localized-name.utils';
import { runSubmit } from '../../../core/utils/dialog-helpers';
import { reloadDetails } from '../../../core/utils/entity-helpers';
import { PrescriptionsService } from '../../prescriptions/prescriptions.service';
import { DispensingService } from '../dispensing.service';
import { DispensePickerResult } from '../dispense-picker-dialog/dispense-picker-dialog.component';

export type DispenseDialogData = DispensePickerResult | string;

interface DispenseViewItem {
  id: string;
  medicineName: string;
  medicineNameAr: string | null;
  variantName: string;
  form: number | null;
  unit: number | null;
  strength: number | null;
  dosageInstructions: string | null;
  prescribedQuantity: number;
  remainingQuantity: number;
  availableQuantity: number | null;
}

interface DispenseView {
  shortCode: string;
  phoneNumber: string;
  patientName: string;
  items: DispenseViewItem[];
}

function isPicked(data: DispenseDialogData): data is DispensePickerResult {
  return typeof data === 'object' && data !== null && 'lookup' in data;
}

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
  private readonly translate = inject(TranslateService);
  private readonly dialogRef = inject(MatDialogRef<DispenseDialogComponent>);

  private readonly data = inject<DispenseDialogData>(MAT_DIALOG_DATA);
  protected readonly view = signal<DispenseView | null>(
    isPicked(this.data)
      ? {
          shortCode: this.data.lookup.shortCode,
          phoneNumber: this.data.phoneNumber,
          patientName: this.data.lookup.patientName,
          items: this.data.lookup.items.map((i) => ({
            id: i.prescriptionItemId,
            medicineName: i.medicineName,
            medicineNameAr: i.medicineNameAr,
            form: i.form,
            unit: i.unit,
            strength: i.strength,
            variantName: i.variantName,
            dosageInstructions: i.dosageInstructions,
            prescribedQuantity: i.prescribedQuantity,
            remainingQuantity: i.remainingQuantity,
            availableQuantity: i.availableQuantity
          }))
        }
      : null
  );
  protected readonly submitting = signal(false);
  protected readonly loadingError = signal(false);
  protected readonly notes = new FormControl('', { nonNullable: true });
  protected readonly dispenseResult = signal<DispensePrescriptionResponse | null>(null);

  protected isPartialResult(r: DispensePrescriptionResponse): boolean {
    return r.dispensedQuantity < r.requestedQuantity;
  }

  protected medName(item: DispenseViewItem): string {
    return pickLocalizedMedicineName(item, this.translate);
  }

  protected variantLabel(item: DispenseViewItem): string {
    const label = localizedVariantName(
      item,
      MedicineForm as unknown as Record<number, string>,
      MedicineUnit as unknown as Record<number, string>,
      this.translate
    );
    return label || item.variantName;
  }

  constructor() {
    if (!isPicked(this.data)) {
      const prescriptionId = this.data;
      void reloadDetails(
        this.view,
        async () => {
          const p = await this.prescriptionsService.get(prescriptionId);
          if (!p.patientPhoneNumber) throw new Error('missing patient phone');
          const mapped: DispenseView = {
            shortCode: p.shortCode,
            phoneNumber: p.patientPhoneNumber,
            patientName: p.patientName,
            items: p.items.map((i) => ({
              id: i.id,
              medicineName: i.medicineName,
              medicineNameAr: i.medicineNameAr ?? null,
              variantName: i.variantName,
              form: i.form,
              unit: i.unit,
              strength: i.strength,
              dosageInstructions: i.dosageInstructions,
              prescribedQuantity: i.prescribedQuantity,
              remainingQuantity: i.remainingQuantity,
              availableQuantity: null
            }))
          };
          return mapped;
        },
        () => this.loadingError.set(true)
      );
    }
  }

  protected retry(): void {
    if (isPicked(this.data)) return;
    const prescriptionId = this.data;
    this.loadingError.set(false);
    this.view.set(null);
    void reloadDetails(
      this.view,
      async () => {
        const p = await this.prescriptionsService.get(prescriptionId);
        if (!p.patientPhoneNumber) throw new Error('missing patient phone');
        const mapped: DispenseView = {
          shortCode: p.shortCode,
          phoneNumber: p.patientPhoneNumber,
          patientName: p.patientName,
          items: p.items.map((i) => ({
            id: i.id,
            medicineName: i.medicineName,
            medicineNameAr: i.medicineNameAr ?? null,
            variantName: i.variantName,
            form: i.form,
            unit: i.unit,
            strength: i.strength,
            dosageInstructions: i.dosageInstructions,
            prescribedQuantity: i.prescribedQuantity,
            remainingQuantity: i.remainingQuantity,
            availableQuantity: null
          }))
        };
        return mapped;
      },
      () => this.loadingError.set(true)
    );
  }

  async dispense(): Promise<void> {
    const current = this.view();
    if (!current || !current.phoneNumber) return;
    await runSubmit(
      this.submitting,
      () =>
        this.dispensingService.dispense({
          shortCode: current.shortCode,
          phoneNumber: current.phoneNumber,
          notes: this.notes.value.trim()
        }),
      (result) => {
        if (result.warnings.length > 0) {
          this.dispenseResult.set(result);
        } else {
          this.toast.show(this.translate.instant('dialogs.dispense.success'), 'success');
          this.dialogRef.close(true);
        }
      }
    );
  }
}
