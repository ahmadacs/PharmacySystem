import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../core/services/toast.service';

export interface ChangePasswordData {
  email: string;
}

@Component({
  selector: 'app-change-password-dialog',
  standalone: true,
  imports: [MatButton, MatFormField, MatInput, MatLabel, MatError, FormsModule, TranslatePipe, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose],
  templateUrl: './change-password-dialog.component.html',
  styleUrl: './change-password-dialog.component.scss'
})
export class ChangePasswordDialogComponent {
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject(MatDialogRef<ChangePasswordDialogComponent>);

  readonly email = inject(MAT_DIALOG_DATA).email;

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  submitting = false;

  async submit(): Promise<void> {
    if (this.submitting) {
      return;
    }
    this.submitting = true;
    try {
      await this.authService.changePassword({
        currentPassword: this.currentPassword,
        newPassword: this.newPassword,
        confirmNewPassword: this.confirmPassword
      });
      this.toast.show('Password changed successfully.', 'success');
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    } finally {
      this.submitting = false;
    }
  }
}