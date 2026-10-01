import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ToastComponent } from '../../shared/components/toast/toast.component';

export type ToastType = 'success' | 'error' | 'info';

const DEFAULT_DURATIONS: Record<ToastType, number> = {
  success: 4000,
  error: 6000,
  info: 4000,
};

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  show(message: string, type: ToastType = 'info', duration?: number): void {
    // Dismiss any visible toast so messages never stack or overlap.
    this.snackBar.dismiss();
    this.snackBar.openFromComponent(ToastComponent, {
      data: {
        message,
        type,
        closeLabel: this.translate.instant('common.close'),
      },
      duration: duration ?? DEFAULT_DURATIONS[type],
      // Top-center: symmetric in LTR/RTL (no direction surprises), clear of
      // the side panels and notification bell, and away from bottom
      // pagination/filters/actions.
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['app-toast', `toast-${type}`],
    });
  }
}
