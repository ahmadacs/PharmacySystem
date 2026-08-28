import { Component, inject, Input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatError, MatFormField, MatLabel, MatHint } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { UserRole } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { UsersService } from '../users.service';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/;

function strongPassword(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string;
  if (!value) {
    return null;
  }
  return PASSWORD_PATTERN.test(value) ? null : { weak: true };
}

function matchPassword(password: FormControl<string>): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    return control.value === password.value ? null : { mismatch: true };
  };
}

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, EnumTranslatePipe, MatFormField, MatInput, MatLabel, MatError, MatHint, MatSelect, MatOption, MatButton, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose],
  templateUrl: './user-form-dialog.component.html',
  styleUrl: './user-form-dialog.component.scss'
})
export class UserFormDialogComponent {
  private readonly usersService = inject(UsersService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  
  @Input() isEdit = false;
  
  protected readonly roles: UserRole[] = ['Admin', 'Pharmacist', 'Doctor'];

  protected readonly submitting = signal(false);

  protected readonly passwordControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(8), strongPassword]
  });

  protected readonly form = new FormGroup({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: this.passwordControl,
    confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required, matchPassword(this.passwordControl)] }),
    role: new FormControl<UserRole>('Pharmacist', { nonNullable: true, validators: [Validators.required] }),
    licenseNumber: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(20)] }),
    specialization: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(150)] }),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(30)] })
  });

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const value = this.form.getRawValue();
      const isDoctor = value.role === 'Doctor';
      await this.usersService.create({
        firstName: value.firstName,
        lastName: value.lastName,
        email: value.email,
        password: value.password,
        confirmPassword: value.confirmPassword,
        role: value.role,
        licenseNumber: isDoctor && value.licenseNumber ? value.licenseNumber : undefined,
        specialization: isDoctor && value.specialization ? value.specialization : undefined,
        phoneNumber: isDoctor && value.phoneNumber ? value.phoneNumber : undefined
      });
      this.toast.show('User created.', 'success');
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting.set(false);
    }
  }
}