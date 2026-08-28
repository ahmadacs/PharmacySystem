import { HttpClient, HttpParams } from '@angular/common/http';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { MatButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatError, MatFormField, MatLabel, MatHint, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatIcon } from '@angular/material/icon';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  InventoryAdjustmentType,
  InventoryAdjustmentTypeEnum,
  MedicineBatchDto,
  MedicineListItemDto,
  PagedResult
} from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { InventoryService } from '../inventory.service';

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

/**
 * Inbound types create a brand-new batch (official receive). Every other type
 * adjusts an existing batch.
 */
const INBOUND_TYPES: InventoryAdjustmentType[] = ['Increase', 'TransferIn'];

@Component({
  selector: 'app-adjust-stock-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    EnumTranslatePipe,
    MatFormField,
    MatInput,
    MatIcon,
    MatLabel,
    MatError,
    MatHint,
    MatSuffix,
    MatSelect,
    MatOption,
    MatAutocompleteModule,
    MatDatepickerModule,
    MatButton,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose
  ],
  templateUrl: './adjust-stock-dialog.component.html',
  styleUrl: './adjust-stock-dialog.component.scss'
})
export class AdjustStockDialogComponent {
  private readonly http = inject(HttpClient);
  private readonly inventoryService = inject(InventoryService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject(MatDialogRef<AdjustStockDialogComponent>);
  private readonly translate = inject(TranslateService);

  protected readonly types: InventoryAdjustmentType[] = [
    'Increase', 'Decrease', 'Correction', 'Damaged', 'Expired', 'Returned', 'Sold', 'TransferOut', 'TransferIn'
  ];
  protected readonly batches = signal<MedicineBatchDto[]>([]);
  protected readonly medicines = signal<MedicineListItemDto[]>([]);
  protected readonly submitting = signal(false);
  protected readonly today = startOfDay(new Date());
  protected readonly medicineSearchControl = new FormControl<string | MedicineListItemDto>('', { nonNullable: true });
  protected readonly medicineSearch = signal('');
  protected readonly isArabic = computed(() => this.translate.currentLang() === 'ar');
  protected readonly filteredMedicines = computed(() => {
    const search = this.medicineSearch().trim().toLowerCase();
    if (!search) return this.medicines();
    return this.medicines().filter((medicine) =>
      medicine.name.toLowerCase().includes(search) ||
      medicine.nameAr?.toLowerCase().includes(search) ||
      medicine.genericName.toLowerCase().includes(search) ||
      medicine.genericNameAr?.toLowerCase().includes(search)
    );
  });

  protected readonly form = new FormGroup(
    {
      medicineBatchId: new FormControl<string | null>(null),
      type: new FormControl<InventoryAdjustmentType | null>(null, { validators: [Validators.required] }),
      quantity: new FormControl(1, { nonNullable: true }),
      reason: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
      medicineId: new FormControl<string | null>(null),
      medicineVariantId: new FormControl<string | null>(null),
      manufactureDate: new FormControl<Date | null>(null),
      expiryDate: new FormControl<Date | null>(null),
      packagesReceived: new FormControl(10, { nonNullable: true, validators: [Validators.min(1)] }),
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

  protected readonly isInbound = computed(() =>
    INBOUND_TYPES.includes(this.selectedType() as InventoryAdjustmentType));

  private readonly selectedType = signal<InventoryAdjustmentType | null>(null);
  private readonly medicineIdSignal = signal<string | null>(null);
  private readonly variantIdSignal = signal<string | null>(null);
  private readonly packagesSignal = signal(10);

  protected readonly selectedMedicine = computed(() => {
    const id = this.medicineIdSignal();
    return this.medicines().find((m) => m.id === id) ?? null;
  });

  protected readonly selectedVariant = computed(() => {
    const id = this.variantIdSignal();
    return this.selectedMedicine()?.variants.find((v) => v.id === id) ?? null;
  });

  protected readonly packageHint = computed(() => {
    const v = this.selectedVariant();
    if (!v) {
      return null;
    }
    const total = v.unitsPerPackage * this.packagesSignal();
    return `1 ${v.packageUnitName} = ${v.unitsPerPackage} ${v.baseUnitName}s. ${this.packagesSignal()} ${v.packageUnitName}s = ${total} ${v.baseUnitName}s.`;
  });

  constructor() {
    void firstValueFrom(
      this.http.get<PagedResult<MedicineBatchDto>>(
        `${environment.apiUrl}/inventory/batches`,
        { params: new HttpParams().set('page', 1).set('pageSize', 200).set('sortBy', 'expiryDate').set('sortDir', 'asc') }
      )
    ).then((result) => this.batches.set(result.items));

    void firstValueFrom(
      this.http.get<PagedResult<MedicineListItemDto>>(
        `${environment.apiUrl}/medicines`,
        { params: new HttpParams().set('page', 1).set('pageSize', 200) }
      )
    ).then((result) => this.medicines.set(result.items));

    this.form.controls.type.valueChanges.subscribe((value) => {
      this.selectedType.set(value as InventoryAdjustmentType | null);
      this.applyValidators();
    });
    this.form.controls.medicineId.valueChanges.subscribe((value) => {
      this.medicineIdSignal.set(value);
      this.onMedicineChange();
    });
    this.form.controls.medicineVariantId.valueChanges.subscribe((value) => this.variantIdSignal.set(value));
    this.form.controls.packagesReceived.valueChanges.subscribe((value) => this.packagesSignal.set(value));
    this.medicineSearchControl.valueChanges.subscribe((value) => {
      this.medicineSearch.set(typeof value === 'string' ? value : this.displayMedicineName(value));
    });
    this.applyValidators();
  }

  onMedicineChange(): void {
    const medicine = this.selectedMedicine();
    const currentVariant = this.form.controls.medicineVariantId.value;
    const firstVariant = medicine?.variants[0];
    if (firstVariant && currentVariant !== firstVariant.id) {
      this.form.controls.medicineVariantId.setValue(firstVariant.id);
      this.variantIdSignal.set(firstVariant.id);
    }
  }

  onMedicineSelected(event: MatAutocompleteSelectedEvent): void {
    const medicine = event.option.value as MedicineListItemDto;
    this.form.controls.medicineId.setValue(medicine.id);
    this.onMedicineChange();
  }

  protected readonly displayMedicineName = (medicine: string | MedicineListItemDto): string => {
    if (!medicine) return '';
    if (typeof medicine === 'string') {
      return this.medicines().find((item) => item.id === medicine)?.name ?? medicine;
    }
    return this.isArabic() && medicine.nameAr ? medicine.nameAr : medicine.name;
  };

  private applyValidators(): void {
    const inbound = this.isInbound();
    const set = (name: string, validators: ValidatorFn[]) => {
      const control = this.form.controls[name as keyof typeof this.form.controls];
      control.setValidators(validators);
      control.updateValueAndValidity();
    };
    set('medicineBatchId', inbound ? [] : [Validators.required]);
    set('quantity', inbound ? [] : [Validators.required, Validators.min(1)]);
    set('medicineVariantId', inbound ? [Validators.required] : []);
    set('manufactureDate', inbound ? [Validators.required] : []);
    set('expiryDate', inbound ? [Validators.required, futureOrEqualDate] : []);
    set('packagesReceived', inbound ? [Validators.required, Validators.min(1)] : []);
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const value = this.form.getRawValue();
      if (this.isInbound()) {
        await this.inventoryService.receive({
          medicineVariantId: value.medicineVariantId as string,
          manufactureDate: toDateString(value.manufactureDate)!,
          expiryDate: toDateString(value.expiryDate)!,
          packagesReceived: value.packagesReceived,
          unitCost: value.unitCost,
          supplierName: value.supplierName || null,
          reason: value.reason,
          adjustmentType: InventoryAdjustmentTypeEnum[value.type as keyof typeof InventoryAdjustmentTypeEnum]
        });
        this.toast.show('Batch received.', 'success');
      } else {
        await this.inventoryService.adjust({
          medicineBatchId: value.medicineBatchId as string,
          type: value.type as InventoryAdjustmentType,
          quantity: value.quantity,
          reason: value.reason
        });
        this.toast.show('Stock adjusted.', 'success');
      }
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting.set(false);
    }
  }
}