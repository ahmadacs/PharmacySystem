import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MatButton } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatProgressBar } from '@angular/material/progress-bar';
import {
  MatTable,
  MatColumnDef,
  MatHeaderCellDef,
  MatCellDef,
  MatHeaderRowDef,
  MatRowDef,
  MatHeaderCell,
  MatCell,
  MatHeaderRow,
  MatRow
} from '@angular/material/table';
import { firstValueFrom } from 'rxjs';
import { Permissions } from '../../../core/constants/permissions';
import { PrescriptionDetailsDto } from '../../../core/models/api.models';
import { AuthStore } from '../../../core/auth/auth.store';
import { TranslateService } from '@ngx-translate/core';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PrescriptionsService } from '../prescriptions.service';
import { ExportService } from '../../../core/services/export.service';

@Component({
  selector: 'app-prescription-details-dialog',
  standalone: true,
  imports: [
    MatButton,
    MatProgressBar,
    MatTable,
    TranslatePipe,
    MatColumnDef,
    MatHeaderCellDef,
    MatCellDef,
    MatHeaderRowDef,
    MatRowDef,
    MatHeaderCell,
    MatCell,
    MatHeaderRow,
    MatRow,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose
  ],
  templateUrl: './prescription-details-dialog.component.html',
  styleUrl: './prescription-details-dialog.component.scss'
})
export class PrescriptionDetailsDialogComponent {
  private readonly prescriptionsService = inject(PrescriptionsService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly authStore = inject(AuthStore);
  private readonly dialogRef = inject(MatDialogRef<PrescriptionDetailsDialogComponent>);
  private readonly exportService = inject(ExportService);
  protected readonly translate = inject(TranslateService);

  readonly prescriptionId = inject<string>(MAT_DIALOG_DATA);
  protected readonly prescription = signal<PrescriptionDetailsDto | null>(null);
  readonly columns = ['medicineName', 'prescribedQuantity', 'dispensedQuantity', 'remainingQuantity', 'dosageInstructions'];

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      this.prescription.set(await this.prescriptionsService.get(this.prescriptionId));
    } catch {
      // error toast already shown by the error interceptor
    }
  }

  protected shortId(id: string): string {
    return id.slice(0, 8).toUpperCase();
  }

  protected canCancel(p: PrescriptionDetailsDto): boolean {
    return (
      this.authStore.hasPermission(Permissions.PrescriptionsManageOwn) &&
      (p.status === 'Pending' || p.status === 'PartiallyDispensed')
    );
  }

  protected canRefill(p: PrescriptionDetailsDto): boolean {
    return (
      this.authStore.hasPermission(Permissions.PrescriptionsManageOwn) &&
      p.status === 'FullyDispensed' &&
      p.isRefillable &&
      p.refillsUsed < p.refillsAllowed
    );
  }

  async cancel(id: string): Promise<void> {
    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDialogComponent, {
          data: {
            title: 'Cancel prescription',
            message: 'Cancel this prescription? This cannot be undone.',
            confirmLabel: 'Cancel prescription',
            danger: true
          }
        })
        .afterClosed()
    );
    if (!confirmed) return;
    try {
      await this.prescriptionsService.cancel(id);
      this.toast.show('Prescription cancelled.', 'success');
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    }
  }

  async print(): Promise<void> {
    try {
      await this.exportService.export('prescriptions', 'pdf', this.prescriptionId);
      this.toast.show(this.translate.instant('dialogs.prescriptionDetails.exported'), 'success');
    } catch {
      // handled
    }
  }

  async refill(id: string): Promise<void> {
    try {
      await this.prescriptionsService.refill(id);
      this.toast.show('Prescription refilled.', 'success');
      this.dialogRef.close(true);
    } catch {
      // error toast already shown by the error interceptor
    }
  }
}