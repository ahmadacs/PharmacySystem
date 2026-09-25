import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent, MatCardFooter, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressBar } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { runFormSubmit } from '../../../core/utils/dialog-helpers';

@Component({ selector: 'app-forgot-password', standalone: true,   imports: [ReactiveFormsModule, TranslatePipe, MatCard, MatCardHeader, MatCardTitle, MatCardSubtitle, MatCardContent, MatCardFooter, MatFormField, MatInput, MatLabel, MatError, MatButton, MatIcon, MatProgressBar, RouterLink], templateUrl: './forgot-password.component.html', styleUrl: './forgot-password.component.scss' })
export class ForgotPasswordComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  protected readonly submitting = signal(false);
  protected readonly form = new FormGroup({ email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }) });

  async submit(): Promise<void> {
    await runFormSubmit(
      this.form,
      this.submitting,
      () => this.authService.forgotPassword(this.form.getRawValue()),
      async () => { this.toast.show('If the account exists, a reset link has been sent.', 'success'); await this.router.navigate(['/login']); }
    );
  }
}
