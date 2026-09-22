import { Component, DestroyRef, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import {
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatTooltip } from '@angular/material/tooltip';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { map, startWith } from 'rxjs';
import {
  MedicineListItemDto,
  MedicineVariantDto,
  PatientMedicationItemDto,
  PatientPrescriptionHistoryDto
} from '../../../core/models/api.models';
import { startOfDay, toDateString } from '../../../core/utils/date-utils';
import { pickLocalizedGenericName, pickLocalizedName } from '../../../core/utils/localized-name.utils';
import { ToastService } from '../../../core/services/toast.service';
import { FileService } from '../../../core/services/file.service';
import { FileUploadDto } from '../../../core/models/inventory.models';
import { PrescriptionsService } from '../prescriptions.service';
import { MedicineLookupService } from '../medicine-lookup.service';
import { FoundPatient, SAUDI_PHONE_PATTERN } from '../patient-lookup.service';
import { PatientPhoneSearchService, PatientState } from './patient-phone-search.service';
import { MedsLookback } from '../meds-history-list/meds-history-list.component';
import { PatientDetailsDialogComponent } from '../patient-details-dialog/patient-details-dialog.component';
import { refillsAllowedValidator, notInFuture } from './prescription-item.validators';

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
    MatSelect,
    MatOption,
    MatCheckbox,
    MatChipsModule,
    MatDatepickerModule,
    MatAutocompleteModule,
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
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const selected = input.files[0];
      const error = this.fileService.validateUpload(selected);
      if (error) {
        this.toast.show(error, 'error');
        return;
      }
      this.file.set(selected);
    }
  }

  removeFile(): void {
    this.file.set(null);
  }
  protected readonly medicines = this.medicineLookup.medicines;
  protected readonly today = startOfDay(new Date());

  // Single patient lookup state (none / found / new) owned by the service —
  // found + new can never contradict each other. Aliased here for the template.
  protected readonly patientState = this.phoneSearch.patientState;
  protected readonly phoneSearching = this.phoneSearch.searching;
  protected readonly phoneHintKey = this.phoneSearch.hintKey;
  // Medication history for the details button badge. The patient details
  // dialog reads these same signals via dialog data (shared by reference),
  // so one fetch serves both views with no extra state object.
  protected readonly medsHistory = signal<PatientPrescriptionHistoryDto[]>([]);
  protected readonly medsLoading = signal(false);
  protected readonly medsLookback = signal<MedsLookback>(90);
  protected readonly lookbackOptions: MedsLookback[] = [90, 180, 365];

  /** The verified patient behind the current phone number, if any. */
  protected readonly foundPatient = computed(() => {
    const state = this.patientState();
    return state.kind === 'found' ? state.patient : null;
  });

  /** Total prescribed lines in the loaded window — shown on the details button. */
  protected readonly medsCount = computed(() =>
    this.medsHistory().reduce((n, p) => n + p.items.length, 0),
  );

  /**
   * Opens the doctor-facing patient snapshot as a dialog (same MatDialog
   * pattern as every other dialog). History signals are shared by reference,
   * so a lookback change fetches once in the parent and both views update.
   * Adding a line inside the dialog copies it into this form via callback.
   */
  protected openPatientDetails(): void {
    const patient = this.foundPatient();
    if (!patient) return;
    this.dialog.open(PatientDetailsDialogComponent, {
      width: '920px',
      maxWidth: '94vw',
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

  // Phone-first flow: the rest of the form appears only after a phone is entered and looked up
  protected readonly showRest = computed(
    () => this.phoneSearching() || this.patientState().kind !== 'none'
  );
  private readonly restAnchor = viewChild<ElementRef<HTMLElement>>('restAnchor');
  /** Previous patient kind — exact transition detection over a clean emission stream. */
  private prevPatientKind: PatientState['kind'] = 'none';

  // Preset refill intervals (days) the doctor picks from. 0 = no time limit.
  protected readonly refillIntervalOptions = [0, 7, 14, 15, 30, 60, 90];

  protected readonly form = new FormGroup({
    patientFirstName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    patientLastName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    patientDateOfBirth: new FormControl<Date | null>(null, { validators: [Validators.required, notInFuture] }),
    patientPhoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(30), Validators.pattern(SAUDI_PHONE_PATTERN)] }),
    issuedDate: new FormControl<Date | null>(startOfDay(new Date()), { validators: [Validators.required, notInFuture] }),
    diagnosis: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
    items: new FormArray<FormGroup>([])
  });

  // Medicine search lives INSIDE each item group (`medicineSearch` control), so
  // the FormArray is the single source of truth. `searchTexts` derives every
  // row's filter text as one standard toSignal — no manual version counter.
  // Declared after `form` (field initializers run in order).
  private readonly searchTexts = toSignal(
    this.form.controls.items.valueChanges.pipe(
      map(() => this.readSearchTexts()),
      startWith(this.readSearchTexts()),
    ),
    { initialValue: [] as string[] },
  );

  constructor() {
    void this.medicineLookup.ensureMedicines();
    this.addItem();

    // Phone-first lookup machine (pure signal producer); this dialog consumes
    // its signals with the effect below — no callbacks.
    this.phoneSearch.track(this.form.controls.patientPhoneNumber.valueChanges);

    // Patient-state consumer: the service emits exactly one notification per
    // genuine kind transition, so `prevKind` is exact transition detection
    // (not a guess). Teardown runs on EVERY path that leaves a patient
    // context — found/new -> anything — never confined to one branch, so
    // items picked for a previous number can never stick, whatever the
    // destination (none, new, or a future direct found->found switch).
    effect(() => {
      const state = this.patientState();
      const prevKind = this.prevPatientKind;
      this.prevPatientKind = state.kind;
      if (prevKind !== 'none' && state.kind !== prevKind) {
        this.teardownPatientContext();
      }
      if (state.kind === 'none') {
        return;
      }
      if (state.kind === 'new') {
        this.medsHistory.set([]);
        this.setPatientReadonly(false);
        return;
      }
      this.applyFoundPatient(state.patient);
    });

    // Gentle reveal: scroll the least amount needed when the rest appears
    let restWasShown = false;
    effect(() => {
      const shown = this.showRest();
      if (shown && !restWasShown) {
        restWasShown = true;
        this.scrollRestIntoView();
      } else if (!shown) {
        restWasShown = false;
      }
    });
  }

  private setPatientReadonly(readonly: boolean): void {
    const fn = this.form.controls.patientFirstName;
    const ln = this.form.controls.patientLastName;
    const dob = this.form.controls.patientDateOfBirth;
    if (readonly) { fn.disable(); ln.disable(); dob.disable(); } else { fn.enable(); ln.enable(); dob.enable(); }
  }

  /**
   * Clears everything bound to the previous patient context. Runs on every
   * path that leaves found/new — never from a single destination branch.
   */
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

  private scrollRestIntoView(): void {
    setTimeout(() => {
      this.restAnchor()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }, 80);
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

  private readSearchTexts(): string[] {
    return this.items.controls.map((group) => {
      const raw = group.get('medicineSearch')?.value as string | MedicineListItemDto | undefined;
      if (!raw) {
        return '';
      }
      return typeof raw === 'string' ? raw : this.displayMedicineName(raw);
    });
  }

  protected filteredMedicines(index: number): MedicineListItemDto[] {
    return this.medicineLookup.filterMedicines(this.searchTexts()?.[index] ?? '');
  }

  addItem(): void {
    const group = new FormGroup({
      medicineSearch: new FormControl<string | MedicineListItemDto>('', { nonNullable: true }),
      medicineId: new FormControl<string | null>(null, { validators: [Validators.required] }),
      medicineVariantId: new FormControl<string | null>(null, { validators: [Validators.required] }),
      quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
      dosageInstructions: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(300)] }),
      isRefillable: new FormControl(false, { nonNullable: true }),
      refillsAllowed: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(99), refillsAllowedValidator] }),
      refillIntervalDays: new FormControl(0, { nonNullable: true, validators: [Validators.pattern(/^\d+$/), Validators.min(0), Validators.max(365)] })
    });
    // Refill UX: the count field is only shown while refillable is on.
    // Unchecking resets it to 0; checking pre-fills 1 so the user rarely types.
    // Validation itself lives in refillsAllowedValidator — this handler only
    // adjusts the value and re-triggers validation, never touches errors.
    group.get('isRefillable')?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((refillable) => {
      const allowed = group.get('refillsAllowed');
      if (!refillable) {
        if (Number(allowed?.value ?? 0) !== 0) {
          allowed?.setValue(0);
        }
      } else if (Number(allowed?.value ?? 0) < 1) {
        allowed?.setValue(1);
      }
      allowed?.updateValueAndValidity();
    });
    this.items.push(group);
  }

  removeItem(index: number): void {
    // Single-structure removal — no parallel arrays, so off-by-one is impossible.
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
    this.items.at(index).reset({
      medicineSearch: '',
      medicineId: null,
      medicineVariantId: null,
      quantity: 1,
      dosageInstructions: '',
      isRefillable: false,
      refillsAllowed: 0,
      refillIntervalDays: 0,
    });
  }

  /** Resets the items to a single blank row (used when the patient context changes). */
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
    // Look up the medicine by ID in the medicines list
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
    // Refill correctness is enforced by refillsAllowedValidator on each item
    // group — no duplicated manual check here. markAllAsTouched recurses into
    // the FormArray, so one call covers every row.
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const value = this.form.getRawValue();
      let fileDto: FileUploadDto | undefined;
      if (this.file()) {
        const selected = this.file()!;
        fileDto = {
          fileName: selected.name,
          contentType: selected.type,
          sizeBytes: selected.size,
          base64Content: await this.fileService.fileToBase64(selected)
        };
      }
      await this.prescriptionsService.create({
        patientFirstName: value.patientFirstName,
        patientLastName: value.patientLastName,
        patientDateOfBirth: toDateString(value.patientDateOfBirth)!,
        patientPhoneNumber: value.patientPhoneNumber || undefined,
        diagnosis: value.diagnosis || undefined,
        issuedDate: toDateString(value.issuedDate)!,
        items: value.items.map((item) => ({
          medicineVariantId: item['medicineVariantId'] as string,
          quantity: item['quantity'],
          dosageInstructions: item['dosageInstructions'] || undefined,
          isRefillable: item['isRefillable'] as boolean,
          refillsAllowed: item['refillsAllowed'] as number,
          refillIntervalDays: Number(item['refillIntervalDays']) || 0
        })),
        file: fileDto
      });
      this.toast.show(this.translate.instant('dialogs.prescriptionForm.created'), 'success');
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting.set(false);
    }
  }
}
