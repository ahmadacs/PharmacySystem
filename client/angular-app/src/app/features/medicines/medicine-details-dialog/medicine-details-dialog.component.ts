import { Component, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
import { reloadDetails } from '../../../core/utils/entity-helpers';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { MedicineDetailsBase } from './medicine-details-base';

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
export class MedicineDetailsDialogComponent extends MedicineDetailsBase {
  private readonly medicinesService = inject(MedicinesService);
  private readonly dialogRef = inject(MatDialogRef<MedicineDetailsDialogComponent>);

  readonly medicineId = inject<string>(MAT_DIALOG_DATA);
  protected readonly medicine = signal<MedicineDetailsDto | null>(null);

  get batchColumns(): string[] {
    const cols = ['batchNumber', 'expiry', 'supplier'];
    if (this.canViewBatchAttachments()) cols.push('attachments');
    return cols;
  }

  constructor() {
    super();
    void reloadDetails(this.medicine, () => this.medicinesService.get(this.medicineId));
  }

  close(): void {
    this.dialogRef.close(true);
  }
}
