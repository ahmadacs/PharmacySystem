import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatDialogTitle, MatDialogActions, MatDialogClose, MatDialogContent, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatAccordion, MatExpansionPanel, MatExpansionPanelHeader, MatExpansionPanelTitle, MatExpansionPanelDescription } from '@angular/material/expansion';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatTooltip } from '@angular/material/tooltip';
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
import { MedicineDetailsDto } from '../../../core/models/api.models';
import { MedicinesService } from '../medicines.service';
import { TranslateService } from '@ngx-translate/core';
import { reloadDetails } from '../../../core/utils/entity-helpers';
import { pickLocalizedGenericName, pickLocalizedName } from '../../../core/utils/localized-name.utils';
import { AttachmentViewerService } from '../../../core/services/attachment-viewer.service';
import { FileEntityType } from '../../../core/services/file.service';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { AuthStore } from '../../../core/auth/auth.store';
import { Permissions } from '../../../core/constants/permissions';

@Component({
  selector: 'app-medicine-details-dialog',
  standalone: true,
  imports: [
    MatButton,
    MatIconButton,
    MatIcon,
    MatProgressBar,
    MatTable,
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
    MatDialogClose,
    MatAccordion,
    MatExpansionPanel,
    MatExpansionPanelHeader,
    MatExpansionPanelTitle,
    MatExpansionPanelDescription,
    TranslatePipe,
    EnumTranslatePipe,
    MatTooltip
  ],
  templateUrl: './medicine-details-dialog.component.html',
  styleUrl: './medicine-details-dialog.component.scss'
})
export class MedicineDetailsDialogComponent {
  private readonly medicinesService = inject(MedicinesService);
  private readonly viewer = inject(AttachmentViewerService);
  private readonly dialogRef = inject(MatDialogRef<MedicineDetailsDialogComponent>);
  protected readonly translate = inject(TranslateService);
  protected readonly authStore = inject(AuthStore);

  readonly medicineId = inject<string>(MAT_DIALOG_DATA);
  protected readonly medicine = signal<MedicineDetailsDto | null>(null);

  protected canViewMedicineAttachments(): boolean {
    return this.authStore.hasPermission(Permissions.MedicinesView);
  }

  protected canViewBatchAttachments(): boolean {
    return (
      this.authStore.hasPermission(Permissions.InventoryView) ||
      this.authStore.hasPermission(Permissions.InventoryAdjust)
    );
  }

  get batchColumns(): string[] {
    const cols = ['batchNumber', 'expiry', 'supplier'];
    if (this.canViewBatchAttachments()) cols.push('attachments');
    return cols;
  }

  constructor() {
    void reloadDetails(this.medicine, () => this.medicinesService.get(this.medicineId));
  }

  close(): void {
    this.dialogRef.close(true);
  }

  displayName(details: MedicineDetailsDto): string {
    return pickLocalizedName(details, this.translate);
  }
  genericDisplay(details: MedicineDetailsDto): string {
    return pickLocalizedGenericName(details, this.translate);
  }

  protected openImages(entityType: FileEntityType, entityId: string): void {
    this.viewer.open(entityType, entityId);
  }
}
