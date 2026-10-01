import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import {
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { MatFormField, MatLabel, MatError, MatPrefix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatProgressBar } from '@angular/material/progress-bar';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { DispensingLookupResponse } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { SaudiPhoneDigitsDirective } from '../../../shared/directives/saudi-phone-digits.directive';
import { SAUDI_PHONE_SUFFIX_PATTERN, toFullSaudiPhone } from '../../../shared/validators/phone.validators';
import { DispensingService } from '../dispensing.service';
import { DispensePickerResult } from '../dispense.models';

@Component({
  selector: 'app-dispense-picker-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatFormField,
    MatLabel,
    MatInput,
    MatError,
    MatPrefix,
    MatProgressBar,
    SaudiPhoneDigitsDirective,
    TranslatePipe
  ],
  templateUrl: './dispense-picker-dialog.component.html',
  styleUrl: './dispense-picker-dialog.component.scss'
})
export class DispensePickerDialogComponent {
  private readonly dispensingService = inject(DispensingService);
  private readonly dialogRef = inject(MatDialogRef<DispensePickerDialogComponent>);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  protected readonly form = new FormGroup({
    shortCode: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(8),
        Validators.pattern(/^[A-Za-z0-9]+$/)
      ]
    }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(SAUDI_PHONE_SUFFIX_PATTERN)]
    })
  });

  protected readonly searching = signal(false);
  protected readonly notFound = signal(false);

  get shortCode(): FormControl<string> {
    return this.form.controls.shortCode;
  }

  get phoneNumber(): FormControl<string> {
    return this.form.controls.phoneNumber;
  }

  /** Short codes are uppercase alphanumeric: normalize while typing. */
  protected onShortCodeInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const cleaned = input.value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 8);
    if (cleaned !== input.value) {
      input.value = cleaned;
    }
    if (cleaned !== this.shortCode.value) {
      this.shortCode.setValue(cleaned);
    }
  }

  async search(): Promise<void> {
    if (this.form.invalid || this.searching()) {
      this.form.markAllAsTouched();
      return;
    }
    this.searching.set(true);
    this.notFound.set(false);
    try {
      const phoneNumber = toFullSaudiPhone(this.phoneNumber.value);
      const result: DispensingLookupResponse = await this.dispensingService.lookup(
        this.shortCode.value,
        phoneNumber
      );
      const picked: DispensePickerResult = { lookup: result, phoneNumber };
      this.dialogRef.close(picked);
    } catch {
      this.notFound.set(true);
      this.toast.show(this.translate.instant('dialogs.dispensePicker.notFound'), 'error');
    } finally {
      this.searching.set(false);
    }
  }
}
