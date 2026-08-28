import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatAccordion, MatExpansionPanel, MatExpansionPanelDescription, MatExpansionPanelHeader, MatExpansionPanelTitle } from '@angular/material/expansion';
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
import { CategoryEnum, MedicineDetailsDto, MedicineForm, MedicineUnit } from '../../../core/models/api.models';
import { FileService } from '../../../core/services/file.service';
import { ToastService } from '../../../core/services/toast.service';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload.component';
import { MedicinesService } from '../medicines.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-medicine-details-dialog',
  standalone: true,
  imports: [
    MatButton,
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
    FileUploadComponent,
    TranslatePipe,
    EnumTranslatePipe
  ],
  templateUrl: './medicine-details-dialog.component.html',
  styleUrl: './medicine-details-dialog.component.scss'
})
export class MedicineDetailsDialogComponent {
  protected readonly medicineForm = MedicineForm;
  private readonly medicinesService = inject(MedicinesService);
  protected readonly fileService = inject(FileService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject(MatDialogRef<MedicineDetailsDialogComponent>);
  protected readonly translate = inject(TranslateService);

  readonly medicineId = inject<string>(MAT_DIALOG_DATA);
  protected readonly medicine = signal<MedicineDetailsDto | null>(null);
  readonly batchColumns = ['batchNumber', 'expiry', 'supplier'];
  protected readonly files = signal<import('../../../core/services/file.service').FileAttachmentDto[]>([]);

  constructor() {
    void this.medicinesService.get(this.medicineId).then((details) => this.medicine.set(details));
    void this.loadFiles();
  }

  async onFileSelected(file: File): Promise<void> {
    try {
      const uploaded = await this.fileService.upload('Medicine', this.medicineId, file);
      this.toast.show(`Uploaded ${uploaded.fileName}`, 'success');
      void this.loadFiles();
    } catch {}
  }

  private async loadFiles(): Promise<void> {
    try { this.files.set(await this.fileService.list('Medicine', this.medicineId)); } catch {}
  }

  close(): void {
    this.dialogRef.close(true);
  }

  displayName(details: MedicineDetailsDto): string {
    return this.lang() === 'ar' && details.nameAr ? details.nameAr : details.name;
  }
  genericDisplay(details: MedicineDetailsDto): string {
    return this.lang() === 'ar' && details.genericNameAr ? details.genericNameAr : details.genericName;
  }

  private lang(): string { const c: any = (this.translate as any).currentLang; return typeof c === 'function' ? c() : c; }
}