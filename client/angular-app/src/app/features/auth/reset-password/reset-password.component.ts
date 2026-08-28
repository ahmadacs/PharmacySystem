import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent, MatCardFooter, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressBar } from '@angular/material/progress-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({ selector: 'app-reset-password', standalone: true, imports: [ReactiveFormsModule, MatCard, MatCardHeader, MatCardTitle, MatCardSubtitle, MatCardContent, MatCardFooter, MatFormField, MatInput, MatLabel, MatError, MatButton, MatIcon, MatProgressBar, RouterLink], templateUrl: './reset-password.component.html', styleUrl: './reset-password.component.scss' })
export class ResetPasswordComponent {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  protected readonly submitting = signal(false);
  protected readonly form = new FormGroup({
    email: new FormControl(this.route.snapshot.queryParamMap.get('email') ?? '', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    token: new FormControl(this.route.snapshot.queryParamMap.get('token') ?? '', { nonNullable: true, validators: [Validators.required] }),
    newPassword: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
    confirmNewPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    try { await this.authService.resetPassword(this.form.getRawValue()); this.toast.show('Password reset successfully.', 'success'); await this.router.navigate(['/login']); }
    catch { /* Error toast is shown by the error interceptor. */ }
    finally { this.submitting.set(false); }
  }
}
