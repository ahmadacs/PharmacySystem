import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import {
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatOption } from '@angular/material/autocomplete';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatError, MatFormField, MatLabel, MatHint } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatSelect } from '@angular/material/select';
import { CategoryEnum, MedicineForm, MedicineListItemDto, MedicineUnit } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { MedicinesService } from '../medicines.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-medicine-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    EnumTranslatePipe,
    MatFormField,
    MatInput,
    MatLabel,
    MatError,
    MatHint,
    MatSelect,
    MatOption,
    MatCheckbox,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatIconButton,
    MatIcon
  ],
  templateUrl: './medicine-form-dialog.component.html',
  styleUrl: './medicine-form-dialog.component.scss'
})
export class MedicineFormDialogComponent {
  private readonly medicinesService = inject(MedicinesService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject(MatDialogRef<MedicineFormDialogComponent>);
  private readonly translate = inject(TranslateService);

  readonly isEdit = inject(MAT_DIALOG_DATA) !== null;
  private readonly medicine = inject<MedicineListItemDto | null>(MAT_DIALOG_DATA);

  protected readonly submitting = signal(false);
  protected readonly medicineForms = Object.values(MedicineForm).filter(
    (form): form is MedicineForm => typeof form === 'number'
  );
  protected readonly medicineForm = MedicineForm;
  protected readonly medicineUnits = Object.values(MedicineUnit).filter((u): u is MedicineUnit => typeof u === 'number');
  protected readonly categories = Object.values(CategoryEnum).filter(v => typeof v === 'number');

  protected readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    genericName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    category: new FormControl<number | null>(null, { nonNullable: true, validators: [Validators.required] }),
    reorderLevel: new FormControl(10, { nonNullable: true, validators: [Validators.min(0)] }),
    isControlled: new FormControl(false, { nonNullable: true }),
    isActive: new FormControl(true, { nonNullable: true }),
    variants: new FormArray<FormGroup>([])
  });

  constructor() {
    // categories are now static enum values
    if (this.isEdit && this.medicine) {
      this.form.patchValue({
        name: this.medicine.name,
        genericName: this.medicine.genericName,
        category: this.medicine.category,
        reorderLevel: this.medicine.reorderLevel,
        isControlled: this.medicine.isControlled,
        isActive: this.medicine.isActive
      });
    } else {
      this.addVariant();
    }
  }

  get variants(): FormArray<FormGroup> {
    return this.form.controls.variants;
  }

  addVariant(): void {
    this.variants.push(
      new FormGroup({
        form: new FormControl<string | null>(null, { validators: [Validators.required] }),
        unit: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
        strength: new FormControl<number | null>(null, { validators: [Validators.min(0.01)] }),
        baseUnitName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
        packageUnitName: new FormControl('Box', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
        unitsPerPackage: new FormControl(30, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
        isDivisible: new FormControl(true, { nonNullable: true })
      })
    );
  }

  removeVariant(index: number): void {
    this.variants.removeAt(index);
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      this.variants.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const value = this.form.getRawValue();
      const categoryValue = value.category as number; // category is required, so it won't be null when valid
      if (this.isEdit && this.medicine) {
        await this.medicinesService.update(this.medicine.id, {
          id: this.medicine.id,
          name: value.name,
          genericName: value.genericName,
          category: categoryValue,
          reorderLevel: value.reorderLevel,
          isControlled: value.isControlled,
          isActive: value.isActive
        });
        this.toast.show('Medicine updated.', 'success');
      } else {
        await this.medicinesService.create({
          name: value.name,
          genericName: value.genericName,
          category: categoryValue,
          reorderLevel: value.reorderLevel,
          isControlled: value.isControlled,
          variants: value.variants.map((v) => ({
            form: v['form'] as MedicineForm,
            unit: v['unit'],
            strength: v['strength'] ?? null,
            baseUnitName: v['baseUnitName'],
            packageUnitName: v['packageUnitName'],
            unitsPerPackage: v['unitsPerPackage'],
            isDivisible: v['isDivisible']
          }))
        });
        this.toast.show('Medicine created.', 'success');
      }
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting.set(false);
    }
  }

protected compareCategory(c1: number, c2: number): boolean {
    return c1 === c2;
  }

  protected compareUnit(u1: string, u2: string): boolean {
    return u1 === u2;
  }
}