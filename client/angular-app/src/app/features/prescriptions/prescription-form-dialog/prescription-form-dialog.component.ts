import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import {
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatTooltip } from '@angular/material/tooltip';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import {
  MedicineListItemDto,
  MedicineVariantDto,
  PatientMedicationItemDto,
  PatientPrescriptionHistoryDto
} from '../../../core/models/api.models';
import { startOfDay, toDateString } from '../../../core/utils/date-utils';
import { pickLocalizedGenericName, pickLocalizedName } from '../../../core/utils/localized-name.utils';
import { runFormSubmit, DialogSizes } from '../../../core/utils/dialog-helpers';
import { clearPickedFile, handlePickedFile } from '../../../core/utils/file-upload.utils';
import { ToastService } from '../../../core/services/toast.service';
import { FileService } from '../../../core/services/file.service';
import { PrescriptionsService } from '../prescriptions.service';
import { MedicineLookupService } from '../medicine-lookup.service';
import { FoundPatient } from '../patient-lookup.service';
import { PatientPhoneSearchService } from './patient-phone-search.service';
import { SaudiPhoneDigitsDirective } from '../../../shared/directives/saudi-phone-digits.directive';
import {
  personNameValidator,
  SAUDI_PHONE_SUFFIX_PATTERN,
  toFullSaudiPhone
} from '../../../shared/validators/phone.validators';
import { MedsLookback } from '../../../core/models/api.models';
import { PatientDetailsDialogComponent } from '../patient-details-dialog/patient-details-dialog.component';
import { notInFuture } from './prescription-item.validators';
import { createPrescriptionItemGroup, resetItemGroup } from './prescription-items-form';

@Component({
  selector: 'app-prescription-form-dialog',
  standalone: true,
  providers: [PatientPhoneSearchService],
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    EnumTranslatePipe,
    MatFormField,
    MatInput,
    MatLabel,
    MatError,
    MatHint,
    MatSuffix,
    MatPrefix,
    MatSelect,
    MatOption,
    MatCheckbox,
    MatDatepickerModule,
    MatAutocompleteModule,
    SaudiPhoneDigitsDirective,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatIconButton,
    MatIcon,
    MatTooltip
  ],
  templateUrl: './prescription-form-dialog.component.html',
  styleUrl: './prescription-form-dialog.component.scss'
})
export class PrescriptionFormDialogComponent {
  private readonly prescriptionsService = inject(PrescriptionsService);
  private readonly medicineLookup = inject(MedicineLookupService);
  private readonly phoneSearch = inject(PatientPhoneSearchService);
  private readonly toast = inject(ToastService);
  private readonly fileService = inject(FileService);
  private readonly dialogRef = inject(MatDialogRef<PrescriptionFormDialogComponent>);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly translate = inject(TranslateService);

  protected readonly submitting = signal(false);
  protected readonly file = signal<File | null>(null);

  onFileSelected(event: Event): void {
    handlePickedFile(event, this.file, this.fileService, this.toast);
  }

  removeFile(): void {
    clearPickedFile(this.file);
  }
  protected readonly today = startOfDay(new Date());

  protected readonly patientState = this.phoneSearch.patientState;
  protected readonly phoneSearching = this.phoneSearch.searching;
  protected readonly medsHistory = signal<PatientPrescriptionHistoryDto[]>([]);
  protected readonly medsLoading = signal(false);
  protected readonly medsLookback = signal<MedsLookback>(90);
  protected readonly lookbackOptions: MedsLookback[] = [90, 180, 365];

  /** The verified patient behind the current phone number, if any. */
  protected readonly foundPatient = computed(() => {
    const state = this.patientState();
    return state.kind === 'found' ? state.patient : null;
  });

  /** Badge count — unique medicines, matching the list rows. */
  protected readonly medsCount = computed(
    () => new Set(this.medsHistory().flatMap((p) => p.items.map((i) => i.medicineId))).size,
  );

  /** Avatar initials for the compact patient card (first letters, max 2). */
  protected readonly patientInitials = computed(() => {
    const patient = this.foundPatient();
    if (!patient) return '?';
    const initials = `${patient.firstName.charAt(0)}${patient.lastName.charAt(0)}`.trim();
    return initials || '?';
  });

  /**
   * Opens the doctor-facing patient snapshot as a dialog. History signals are
   * shared by reference, so a lookback change fetches once and both views update.
   */
  protected openPatientDetails(): void {
    const patient = this.foundPatient();
    if (!patient) return;
    this.dialog.open(PatientDetailsDialogComponent, {
      ...DialogSizes.wide,
      data: {
        patient,
        history: this.medsHistory,
        loading: this.medsLoading,
        lookback: this.medsLookback,
        lookbackOptions: this.lookbackOptions,
        onAdd: (item: PatientMedicationItemDto) => void this.addMedToForm(item),
        onLookbackChange: (days: MedsLookback) => this.setLookback(days),
      },
    });
  }

  // Manual phone lookup: the rest of the form appears only after the user
  // presses Search and a result (found / new) arrives. No auto-tracking.
  protected readonly showRest = computed(() => this.patientState().kind !== 'none');

  // Preset refill intervals (days) the doctor picks from. 0 = no time limit.
  protected readonly refillIntervalOptions = [0, 7, 14, 15, 30, 60, 90];

  protected readonly form = new FormGroup({
    patientFirstName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100), personNameValidator()] }),
    patientLastName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100), personNameValidator()] }),
    patientDateOfBirth: new FormControl<Date | null>(null, { validators: [Validators.required, notInFuture] }),
    patientPhoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(SAUDI_PHONE_SUFFIX_PATTERN)] }),
    issuedDate: new FormControl<Date | null>(startOfDay(new Date()), { validators: [Validators.required, notInFuture] }),
    diagnosis: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
    items: new FormArray<FormGroup>([])
  });

  constructor() {
    void this.medicineLookup.ensureMedicines();
    this.addItem();
  }

  /** Explicit Search button (or Enter in the phone field). */
  protected async searchPatient(): Promise<void> {
    const phoneControl = this.form.controls.patientPhoneNumber;
    phoneControl.markAsTouched();
    if (phoneControl.invalid || this.phoneSearching()) {
      return;
    }
    this.teardownPatientContext();
    await this.phoneSearch.search(toFullSaudiPhone(phoneControl.value));
    const state = this.patientState();
    if (state.kind === 'new') {
      this.medsHistory.set([]);
      this.setPatientReadonly(false);
    } else if (state.kind === 'found') {
      this.applyFoundPatient(state.patient);
    }
  }

  /** Any edit after a result voids it — the user must press Search again. */
  protected onPhoneEdited(): void {
    if (this.patientState().kind !== 'none') {
      this.phoneSearch.reset();
      this.teardownPatientContext();
    }
  }

  private setPatientReadonly(readonly: boolean): void {
    const fn = this.form.controls.patientFirstName;
    const ln = this.form.controls.patientLastName;
    const dob = this.form.controls.patientDateOfBirth;
    if (readonly) { fn.disable(); ln.disable(); dob.disable(); } else { fn.enable(); ln.enable(); dob.enable(); }
  }

  /** Clears everything bound to the previous patient lookup. */
  private teardownPatientContext(): void {
    this.medsHistory.set([]);
    if (this.form.controls.patientFirstName.disabled) {
      this.form.controls.patientFirstName.setValue('');
      this.form.controls.patientLastName.setValue('');
      this.form.controls.patientDateOfBirth.setValue(null);
    }
    this.setPatientReadonly(false);
    this.resetItemsToBlank();
  }

  private applyFoundPatient(patient: FoundPatient): void {
    this.setPatientReadonly(false);
    this.form.controls.patientFirstName.setValue(patient.firstName);
    this.form.controls.patientLastName.setValue(patient.lastName);
    this.form.controls.patientDateOfBirth.setValue(patient.dateOfBirth ? new Date(patient.dateOfBirth) : null);
    this.setPatientReadonly(true);
    this.medsHistory.set([]);
    if (patient.id) {
      void this.loadPatientMeds(patient.id);
    }
  }

  protected setLookback(days: MedsLookback): void {
    if (this.medsLookback() === days) return;
    this.medsLookback.set(days);
    const state = this.patientState();
    const id = state.kind === 'found' ? state.patient.id : undefined;
    if (id) void this.loadPatientMeds(id);
  }

  private async loadPatientMeds(patientId: string): Promise<void> {
    this.medsLoading.set(true);
    try {
      const history = await this.prescriptionsService.patientHistory(patientId, this.medsLookback());
      this.medsHistory.set(history);
    } catch {
      this.medsHistory.set([]);
    } finally {
      this.medsLoading.set(false);
    }
  }

  private showMedNotAvailable(): void {
    this.toast.show(this.translate.instant('dialogs.prescriptionForm.medsHistory.medNotAvailable'), 'error');
  }

  /** Copies a history line into the current prescription, reusing the first empty row. */
  protected async addMedToForm(item: PatientMedicationItemDto): Promise<void> {
    const t = (key: string): string => this.translate.instant(key);
    if (!item.medicineId || !item.medicineVariantId) {
      this.showMedNotAvailable();
      return;
    }
    const medicine = this.medicineLookup.findMedicine(item.medicineId);
    if (!medicine) {
      this.showMedNotAvailable();
      return;
    }
    // Ensure variants cached for the select.
    if (!this.medicineLookup.hasVariants(item.medicineId)) {
      try {
        await this.medicineLookup.ensureVariants(item.medicineId);
      } catch {
        this.showMedNotAvailable();
        return;
      }
    }
    // Reuse the first empty row (e.g. the initial blank item) instead of appending.
    let index = this.items.controls.findIndex(
      (row) => !row.get('medicineId')?.value && !row.get('medicineVariantId')?.value
    );
    if (index < 0) {
      this.addItem();
      index = this.items.length - 1;
    }
    const group = this.items.at(index);
    group.get('medicineId')?.setValue(item.medicineId);
    group.get('medicineVariantId')?.setValue(item.medicineVariantId);
    group.get('quantity')?.setValue(item.prescribedQuantity > 0 ? item.prescribedQuantity : 1);
    group.get('dosageInstructions')?.setValue(item.dosageInstructions ?? '');
    group.get('medicineSearch')?.setValue(medicine);
    this.toast.show(t('dialogs.prescriptionForm.medsHistory.added'), 'success');
  }

  get items(): FormArray<FormGroup> {
    return this.form.controls.items;
  }

  /** Search box control for row `index` — owned by the item group itself. */
  protected searchControl(index: number): FormControl<string | MedicineListItemDto> {
    return this.items.at(index).get('medicineSearch') as FormControl<string | MedicineListItemDto>;
  }

  variantsFor(index: number): MedicineVariantDto[] {
    const medicineId = this.items.at(index).get('medicineId')?.value as string | null;
    return this.medicineLookup.variantsFor(medicineId);
  }

  protected filteredMedicines(index: number): MedicineListItemDto[] {
    const raw = this.items.at(index).get('medicineSearch')?.value as string | MedicineListItemDto | undefined;
    const text = !raw ? '' : typeof raw === 'string' ? raw : this.displayMedicineName(raw);
    return this.medicineLookup.filterMedicines(text);
  }

  addItem(): void {
    this.items.push(createPrescriptionItemGroup(this.destroyRef));
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
  }

  /** A row counts as filled once a medicine is picked or search text is typed. */
  protected isItemFilled(index: number): boolean {
    const group = this.items.at(index);
    return !!group.get('medicineId')?.value
      || !!group.get('medicineVariantId')?.value
      || !!group.get('medicineSearch')?.value;
  }

  /** Clears a single row in place (used when it is the only item and cannot be removed). */
  protected clearItem(index: number): void {
    resetItemGroup(this.items.at(index));
  }

  /** Resets the items to a single blank row (used when the patient lookup changes). */
  private resetItemsToBlank(): void {
    this.items.clear();
    this.addItem();
  }

  onMedicineChange(index: number, medicineId: string): void {
    const group = this.items.at(index);
    group.get('medicineVariantId')?.setValue(null);

    if (this.medicineLookup.hasVariants(medicineId)) {
      return;
    }
    void this.medicineLookup.ensureVariants(medicineId).catch(() => undefined);
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

  protected compareVariant(v1: MedicineVariantDto, v2: MedicineVariantDto): boolean {
    return v1 && v2 ? v1.id === v2.id : v1 === v2;
  }

  protected readonly displayMedicineName = (medicineIdOrString: string | MedicineListItemDto): string => {
    if (!medicineIdOrString) return '';
    const medicine = typeof medicineIdOrString === 'string'
      ? this.medicineLookup.findMedicine(medicineIdOrString)
      : medicineIdOrString;
    if (!medicine) return '';
    return pickLocalizedName(medicine, this.translate);
  };

  protected displayMedicineGenericName(medicine: MedicineListItemDto): string {
    return pickLocalizedGenericName(medicine, this.translate);
  }

  async submit(): Promise<void> {
    await runFormSubmit(
      this.form,
      this.submitting,
      async () => {
        const value = this.form.getRawValue();
        const created = await this.prescriptionsService.create({
          patientFirstName: value.patientFirstName,
          patientLastName: value.patientLastName,
          patientDateOfBirth: toDateString(value.patientDateOfBirth)!,
          patientPhoneNumber: toFullSaudiPhone(value.patientPhoneNumber) || undefined,
          diagnosis: value.diagnosis || undefined,
          issuedDate: toDateString(value.issuedDate)!,
          items: value.items.map((item) => ({
            medicineVariantId: item['medicineVariantId'] as string,
            quantity: item['quantity'],
            dosageInstructions: item['dosageInstructions'] || undefined,
            isRefillable: item['isRefillable'] as boolean,
            refillsAllowed: item['refillsAllowed'] as number,
            refillIntervalDays: Number(item['refillIntervalDays']) || 0
          }))
        });
        if (this.file()) {
          await this.fileService.upload('Prescription', created.id, this.file()!);
        }
      },
      () => {
        this.toast.show(this.translate.instant('dialogs.prescriptionForm.created'), 'success');
        this.dialogRef.close(true);
      }
    );
  }
}
