import { Component, Inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { MatButton } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatButton, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, FormsModule, TranslatePipe],
  templateUrl: './confirm-dialog.component.html'
})
export class ConfirmDialogComponent {
  constructor(
    @Inject(MAT_DIALOG_DATA) public readonly data: ConfirmDialogData,
    private readonly dialogRef: MatDialogRef<ConfirmDialogComponent>
  ) {}

  close(): void {
    this.dialogRef.close(false);
  }
}