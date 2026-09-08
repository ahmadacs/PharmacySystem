import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
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
import { PrescriptionDetailsDto, PrescriptionItemDto } from '../../../core/models/api.models';
import { AuthStore } from '../../../core/auth/auth.store';
import { TranslateService } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PrescriptionsService } from '../prescriptions.service';
import { ExportService } from '../../../core/services/export.service';

@Component({
  selector: 'app-prescription-details-dialog',
  standalone: true,
  imports: [
    MatButton,
    MatIconButton,
    MatIcon,
    MatTooltip,
    MatProgressBar,
    MatTable,
    TranslatePipe,
    EnumTranslatePipe,
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
  readonly columns = ['medicineName', 'prescribedQuantity', 'dispensedQuantity', 'remainingQuantity', 'dosageInstructions', 'refill'];

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

  protected canManage(): boolean {
    return this.authStore.hasPermission(Permissions.PrescriptionsManageOwn);
  }

  protected canRefillItem(item: PrescriptionItemDto): boolean {
    return (
      this.canManage() &&
      item.isRefillable &&
      item.dispensedQuantity >= item.prescribedQuantity &&
      item.refillsUsed < item.refillsAllowed
    );
  }

  protected eligibleItems(p: PrescriptionDetailsDto): PrescriptionItemDto[] {
    return p.items.filter((item) => this.canRefillItem(item));
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

  async refillItem(prescriptionId: string, itemId: string): Promise<void> {
    try {
      await this.prescriptionsService.refillItem(prescriptionId, itemId);
      this.toast.show('Item refilled.', 'success');
      this.prescription.set(await this.prescriptionsService.get(prescriptionId));
    } catch {
      // error toast already shown by the error interceptor
    }
  }

  async refillAllEligible(p: PrescriptionDetailsDto): Promise<void> {
    const ids = this.eligibleItems(p).map((item) => item.id);
    if (ids.length === 0) return;
    try {
      await this.prescriptionsService.refillItems(p.id, ids);
      this.toast.show('Eligible items refilled.', 'success');
      this.prescription.set(await this.prescriptionsService.get(p.id));
    } catch {
      // error toast already shown by the error interceptor
    }
  }
}