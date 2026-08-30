import { Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatError, MatFormField, MatLabel, MatHint, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatIcon } from '@angular/material/icon';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatTooltip } from '@angular/material/tooltip';
import { MedicineDetailsDto } from '../../../core/models/api.models';
import { FileService } from '../../../core/services/file.service';
import { ToastService } from '../../../core/services/toast.service';
import { MedicinesService } from '../medicines.service';

function startOfDay(value: Date): Date {
  return new Date(Date.UTC(value.getFullYear(), value.getMonth(), value.getDate()));
}

function toDateString(date: Date | null): string | null {
  if (!date) {
    return null;
  }
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function futureOrEqualDate(control: AbstractControl): ValidationErrors | null {
  const value = control.value as Date | null;
  if (!value) {
    return null;
  }
  return startOfDay(value) >= startOfDay(new Date()) ? null : { pastDate: true };
}

@Component({
  selector: 'app-batch-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    MatFormField,
    MatInput,
    MatIcon,
    MatLabel,
    MatError,
    MatHint,
    MatSuffix,
    MatSelect,
    MatOption,
    MatDatepickerModule,
    MatButton,
    MatProgressBar,
    MatTooltip,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose
  ],
  templateUrl: './batch-form-dialog.component.html',
  styleUrl: './batch-form-dialog.component.scss'
})
export class BatchFormDialogComponent {
  private readonly medicinesService = inject(MedicinesService);
  private readonly fileService = inject(FileService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject(MatDialogRef<BatchFormDialogComponent>);

  readonly medicineId = inject<string>(MAT_DIALOG_DATA);
  protected readonly medicine = signal<MedicineDetailsDto | null>(null);
  protected readonly submitting = signal(false);
  protected readonly today = startOfDay(new Date());
  protected readonly file = signal<File | null>(null);
  protected readonly fileUploading = signal(false);

  constructor() {
    void this.medicinesService.get(this.medicineId).then((details) => {
      this.medicine.set(details);
      const first = details.variants[0];
      if (first) {
        this.form.controls.medicineVariantId.setValue(first.id);
      }
    });
  }

  protected readonly form = new FormGroup(
    {
      medicineVariantId: new FormControl<string | null>(null, { validators: [Validators.required] }),
      manufactureDate: new FormControl<Date | null>(null, { validators: [Validators.required] }),
      expiryDate: new FormControl<Date | null>(null, { validators: [Validators.required, futureOrEqualDate] }),
      packagesReceived: new FormControl(10, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
      unitCost: new FormControl(0, { nonNullable: true, validators: [Validators.min(0)] }),
      supplierName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] })
    },
    {
      validators: (control) => {
        const group = control as FormGroup;
        const expiryDate = group.controls['expiryDate'].value as Date | null;
        const manufactureDate = group.controls['manufactureDate'].value as Date | null;
        return (
          expiryDate &&
          manufactureDate &&
          startOfDay(expiryDate) < startOfDay(manufactureDate)
        )
          ? { expiryBeforeManufacture: true }
          : null;
      }
    }
  );

  protected readonly selectedVariant = computed(() => {
    const id = this.form.controls.medicineVariantId.value;
    return this.medicine()?.variants.find((v) => v.id === id) ?? null;
  });

  protected readonly packageHint = computed(() => {
    const v = this.selectedVariant();
    if (!v) {
      return null;
    }
    const total = v.unitsPerPackage * this.form.controls.packagesReceived.value;
    return `1 ${v.packageUnitName} = ${v.unitsPerPackage} ${v.baseUnitName}s. ${this.form.controls.packagesReceived.value} ${v.packageUnitName}s = ${total} ${v.baseUnitName}s.`;
  });

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const maxSize = 5 * 1024 * 1024; // 5MB
      const allowedTypes = ['application/pdf', 'image/jpeg', 'image/png'];
      if (!allowedTypes.includes(file.type)) {
        this.toast.show('Only PDF, JPG, and PNG files are allowed.', 'error');
        return;
      }
      if (file.size > maxSize) {
        this.toast.show('File size must be less than 5MB.', 'error');
        return;
      }
      this.file.set(file);
    }
  }

  removeFile(): void {
    this.file.set(null);
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const value = this.form.getRawValue();

      const batch = await this.medicinesService.addBatch(this.medicineId, {
        medicineVariantId: value.medicineVariantId as string,
        manufactureDate: toDateString(value.manufactureDate)!,
        expiryDate: toDateString(value.expiryDate)!,
        packagesReceived: value.packagesReceived,
        unitCost: value.unitCost,
        supplierName: value.supplierName || ''
      });

      // Upload file for Batch if provided
      if (this.file()) {
        this.fileUploading.set(true);
        try {
          const file = this.file()!;
          await this.fileService.upload('Batch', batch.id, file);
        } finally {
          this.fileUploading.set(false);
        }
      }

      this.toast.show('Batch added.', 'success');
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting.set(false);
    }
  }
}