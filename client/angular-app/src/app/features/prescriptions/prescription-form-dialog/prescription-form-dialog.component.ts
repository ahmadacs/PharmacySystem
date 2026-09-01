import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import {
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatError, MatFormField, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatAutocompleteModule, MatAutocompleteTrigger, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  MedicineDetailsDto,
  MedicineListItemDto,
  MedicineVariantDto,
  PagedResult
} from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { PrescriptionsService } from '../prescriptions.service';
import { TranslateService } from '@ngx-translate/core';

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

function notInFuture(control: FormControl<Date | null>): Record<string, boolean> | null {
  const value = control.value;
  if (!value) {
    return null;
  }
  return startOfDay(value) <= startOfDay(new Date()) ? null : { futureDate: true };
}

type PatientPhoneCheckPayload = {
  id?: string;
  firstName?: string | null;
  lastName?: string | null;
  dateOfBirth?: string | null;
  phoneNumber?: string | null;
  exists?: boolean;
};

interface PatientPhoneCheckResponse {
  isFailure?: boolean;
  value?: PatientPhoneCheckPayload | null;
  error?: unknown;
  statusCode?: number;
}

@Component({
  selector: 'app-prescription-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    EnumTranslatePipe,
    MatFormField,
    MatInput,
    MatLabel,
    MatError,
    MatSuffix,
    MatSelect,
    MatOption,
    MatCheckbox,
    MatDatepickerModule,
    MatAutocompleteModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatIconButton,
    MatIcon
  ],
  templateUrl: './prescription-form-dialog.component.html',
  styleUrl: './prescription-form-dialog.component.scss'
})
export class PrescriptionFormDialogComponent {
  private readonly http = inject(HttpClient);
  private readonly prescriptionsService = inject(PrescriptionsService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject(MatDialogRef<PrescriptionFormDialogComponent>);
  protected readonly translate = inject(TranslateService);

  protected readonly submitting = signal(false);
  protected readonly medicines = signal<MedicineListItemDto[]>([]);
  private readonly variantsByMedicine = signal<Record<string, MedicineVariantDto[]>>({});
  protected readonly today = startOfDay(new Date());

  // Phone-first search
  protected readonly foundPatient = signal<{ id: string; firstName: string; lastName: string; dateOfBirth: string; phoneNumber: string } | null>(null);
  protected readonly isNewPatient = signal(false);
  protected readonly phoneSearching = signal(false);
  protected readonly previousPrescriptions = signal<{ id: string; issuedDate: string; status: string; itemCount: number }[]>([]);
  protected readonly isReadOnlyPatient = computed(() => this.foundPatient() !== null);

  // Helper for template
  protected readonly isArabic = computed(() => this.translate.currentLang() === 'ar');

  // Medicine search
  protected readonly medicineSearchControls: FormControl<string | MedicineListItemDto>[] = [];
  protected readonly medicineSearches = signal<string[]>([]);
  protected filteredMedicines(index: number): MedicineListItemDto[] {
    const search = (this.medicineSearches()[index] ?? '').toLowerCase();
    if (!search) return this.medicines();
    return this.medicines().filter(m =>
      m.name.toLowerCase().includes(search) ||
      m.nameAr?.toLowerCase().includes(search) ||
      m.genericName.toLowerCase().includes(search) ||
      m.genericNameAr?.toLowerCase().includes(search)
    );
  }

  protected readonly form = new FormGroup({
    patientFirstName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    patientLastName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    patientDateOfBirth: new FormControl<Date | null>(null, { validators: [Validators.required, notInFuture] }),
    patientPhoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(30), Validators.pattern(/^(?:\+9665\d{8}|05\d{8}|5\d{8})$/)] }),
    issuedDate: new FormControl<Date | null>(startOfDay(new Date()), { validators: [Validators.required, notInFuture] }),
    diagnosis: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
    isRefillable: new FormControl(false, { nonNullable: true }),
    refillsAllowed: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(99)] }),
    items: new FormArray<FormGroup>([])
  });

  constructor() {
    const params = new HttpParams().set('page', 1).set('pageSize', 200).set('sortBy', 'name').set('sortDir', 'asc');
    void firstValueFrom(
      this.http.get<PagedResult<MedicineListItemDto>>(`${environment.apiUrl}/medicines`, { params })
    ).then((result) => {
      this.medicines.set(result.items.filter((m) => m.isActive));
    });

    this.addItem();

    // Auto search by phone (debounce 400ms) - Saudi pattern
    let phoneTimer: ReturnType<typeof setTimeout> | null = null;
    this.form.controls.patientPhoneNumber.valueChanges.subscribe((val) => {
      if (phoneTimer) clearTimeout(phoneTimer);
      const phone = (val ?? '').trim();
      const saudiPattern = /^(?:\+9665\d{8}|05\d{8}|5\d{8})$/;
      if (!saudiPattern.test(phone)) {
        this.foundPatient.set(null);
        this.isNewPatient.set(false);
        this.previousPrescriptions.set([]);
        this.setPatientReadonly(false);
        return;
      }
      phoneTimer = setTimeout(() => void this.searchPatient(phone), 400);
    });

  }

  private setPatientReadonly(readonly: boolean): void {
    const fn = this.form.controls.patientFirstName;
    const ln = this.form.controls.patientLastName;
    const dob = this.form.controls.patientDateOfBirth;
    if (readonly) { fn.disable(); ln.disable(); dob.disable(); } else { fn.enable(); ln.enable(); dob.enable(); }
  }

  private async searchPatient(phone: string): Promise<void> {
    this.phoneSearching.set(true);
    try {
      const result = await firstValueFrom(
        this.http.get<PatientPhoneCheckResponse>(`${environment.apiUrl}/patients/by-phone/${encodeURIComponent(phone)}`)
      );

      const payload = result?.value ?? (result as PatientPhoneCheckPayload | undefined);
      const patientData: PatientPhoneCheckPayload | null = payload && typeof payload === 'object' ? payload : null;

      const isExistingPatient = !result?.isFailure && !!patientData && (
        patientData.exists === true ||
        !!patientData.firstName ||
        !!patientData.lastName ||
        !!patientData.dateOfBirth
      );

      if (isExistingPatient) {
        const patient = {
          id: patientData.id ?? '',
          firstName: patientData.firstName ?? '',
          lastName: patientData.lastName ?? '',
          dateOfBirth: patientData.dateOfBirth ?? '',
          phoneNumber: patientData.phoneNumber ?? phone
        };

        this.foundPatient.set(patient);
        this.isNewPatient.set(false);
        this.form.controls.patientFirstName.setValue(patient.firstName);
        this.form.controls.patientLastName.setValue(patient.lastName);
        this.form.controls.patientDateOfBirth.setValue(patient.dateOfBirth ? new Date(patient.dateOfBirth) : null);
        this.setPatientReadonly(true);
        this.previousPrescriptions.set([]);
        return;
      }

      this.foundPatient.set(null);
      this.isNewPatient.set(true);
      this.previousPrescriptions.set([]);
      this.setPatientReadonly(false);
    } catch {
      this.foundPatient.set(null);
      this.isNewPatient.set(true);
      this.previousPrescriptions.set([]);
      this.setPatientReadonly(false);
    } finally {
      this.phoneSearching.set(false);
    }
  }


  get items(): FormArray<FormGroup> {
    return this.form.controls.items;
  }

  variantsFor(index: number): MedicineVariantDto[] {
    const medicineId = this.items.at(index).get('medicineId')?.value as string | null;
    if (!medicineId) {
      return [];
    }
    return this.variantsByMedicine()[medicineId] ?? [];
  }

  addItem(): void {
    const medicineSearchControl = new FormControl<string | MedicineListItemDto>('', { nonNullable: true });
    this.medicineSearchControls.push(medicineSearchControl);
    this.medicineSearches.update((searches) => [...searches, '']);
    medicineSearchControl.valueChanges.subscribe((value) => {
      const index = this.medicineSearchControls.indexOf(medicineSearchControl);
      if (index < 0) {
        return;
      }
      this.medicineSearches.update((searches) => {
        const next = [...searches];
        next[index] = typeof value === 'string' ? value : this.displayMedicineName(value);
        return next;
      });
    });
    this.items.push(
      new FormGroup({
        medicineId: new FormControl<string | null>(null, { validators: [Validators.required] }),
        medicineVariantId: new FormControl<string | null>(null, { validators: [Validators.required] }),
        quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
        dosageInstructions: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(300)] })
      })
    );
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
    this.medicineSearchControls.splice(index, 1);
    this.medicineSearches.update((searches) => searches.filter((_, itemIndex) => itemIndex !== index));
  }

  onMedicineChange(index: number, medicineId: string): void {
    const group = this.items.at(index);
    group.get('medicineVariantId')?.setValue(null);

    if (this.variantsByMedicine()[medicineId]) {
      return;
    }
    void firstValueFrom(
      this.http.get<MedicineDetailsDto>(`${environment.apiUrl}/medicines/${medicineId}`)
    ).then((details) => {
      this.variantsByMedicine.update((map) => ({
        ...map,
        [medicineId]: details.variants.filter((v) => v.isActive)
      }));
    });
  }

  onMedicineSelected(event: MatAutocompleteSelectedEvent, index?: number): void {
    const medicine = event.option.value as MedicineListItemDto;
    const medicineId = medicine.id;
    if (index !== undefined && index >= 0) {
      const itemGroup = this.items.at(index);
      if (itemGroup) {
        itemGroup.get('medicineId')?.setValue(medicineId);
        this.onMedicineChange(index, medicineId);
      }
    }
  }

  protected compareMedicine(m1: MedicineListItemDto | string, m2: MedicineListItemDto | string): boolean {
    if (typeof m1 === 'string' && typeof m2 === 'string') return m1 === m2;
    if (typeof m1 === 'object' && typeof m2 === 'object') return m1 && m2 ? m1.id === m2.id : m1 === m2;
    return m1 === m2;
  }

  protected compareVariant(v1: MedicineVariantDto, v2: MedicineVariantDto): boolean {
    return v1 && v2 ? v1.id === v2.id : v1 === v2;
  }

  protected readonly displayMedicineName = (medicineIdOrString: string | MedicineListItemDto): string => {
    if (!medicineIdOrString) return '';
    if (typeof medicineIdOrString === 'string') {
      // Look up the medicine by ID in the medicines list
      const medicine = this.medicines().find(m => m.id === medicineIdOrString);
      if (!medicine) return '';
      return this.isArabic() && medicine.nameAr ? medicine.nameAr : medicine.name;
    }
    return this.isArabic() && medicineIdOrString.nameAr ? medicineIdOrString.nameAr : medicineIdOrString.name;
  };

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      this.items.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const value = this.form.getRawValue();
      await this.prescriptionsService.create({
        patientFirstName: value.patientFirstName,
        patientLastName: value.patientLastName,
        patientDateOfBirth: toDateString(value.patientDateOfBirth)!,
        patientPhoneNumber: value.patientPhoneNumber || undefined,
        diagnosis: value.diagnosis || undefined,
        issuedDate: toDateString(value.issuedDate)!,
        isRefillable: value.isRefillable,
        refillsAllowed: value.refillsAllowed,
        items: value.items.map((item) => ({
          medicineVariantId: item['medicineVariantId'] as string,
          quantity: item['quantity'],
          dosageInstructions: item['dosageInstructions'] || undefined
        }))
      });
      this.toast.show('Prescription created.', 'success');
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting.set(false);
    }
  }
}