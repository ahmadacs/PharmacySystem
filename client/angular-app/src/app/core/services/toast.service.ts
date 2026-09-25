import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';

export type ToastType = 'success' | 'error' | 'info';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  show(message: string, type: ToastType = 'info', duration = 4000): void {
    this.snackBar.open(message, this.translate.instant('common.close'), {
      duration,
      panelClass: [`toast-${type}`]
    });
  }
}