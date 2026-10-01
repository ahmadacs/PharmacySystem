import { Component, inject } from '@angular/core';
import { MAT_SNACK_BAR_DATA, MatSnackBarRef } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import type { ToastType } from '../../../core/services/toast.service';

export interface ToastData {
  message: string;
  type: ToastType;
  closeLabel: string;
}

const ICONS: Record<ToastType, string> = {
  success: 'check_circle',
  error: 'error',
  info: 'info',
};

/**
 * Custom snackbar content: severity icon + message + dismiss button.
 * Rendered inside MatSnackBar via openFromComponent; the outer container
 * is styled globally through the `app-toast` panelClass in styles.scss.
 */
@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [MatIconModule, MatButtonModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss',
})
export class ToastComponent {
  protected readonly ref = inject(MatSnackBarRef<ToastComponent>);
  protected readonly data = inject<ToastData>(MAT_SNACK_BAR_DATA);

  protected get icon(): string {
    return ICONS[this.data.type] ?? ICONS.info;
  }
}
