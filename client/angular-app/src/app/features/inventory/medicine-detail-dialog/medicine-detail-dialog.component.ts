import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatProgressBar } from '@angular/material/progress-bar';
import { CategoryEnum, MedicineDetailsDto, MedicineForm, MedicineUnit } from '../../../core/models/api.models';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';
import { MedicinesService } from '../../medicines/medicines.service';
import { TranslateService } from '@ngx-translate/core';

export interface MedicineDetailDialogData {
  id: string;
  name: string;
}

@Component({
  selector: 'app-medicine-detail-dialog',
  standalone: true,
  imports: [
    MatButton,
    MatProgressBar,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    RiyadhDatePipe,
    CurrencyPipe,
    TranslatePipe,
    EnumTranslatePipe
  ],
  templateUrl: './medicine-detail-dialog.component.html',
  styleUrl: './medicine-detail-dialog.component.scss'
})
export class MedicineDetailDialogComponent {
  private readonly medicinesService = inject(MedicinesService);
  protected readonly translate = inject(TranslateService);

  readonly data = inject<MedicineDetailDialogData>(MAT_DIALOG_DATA);
  protected readonly medicine = signal<MedicineDetailsDto | null>(null);
  private lang(): string { const c: any = (this.translate as any).currentLang; return typeof c === 'function' ? c() : c; }

  constructor() {
    void this.medicinesService
      .get(this.data.id)
      .then((details) => this.medicine.set(details));
  }

  displayName(details: MedicineDetailsDto): string { return this.lang() === 'ar' && details.nameAr ? details.nameAr : details.name; }
  genericDisplay(details: MedicineDetailsDto): string { return this.lang() === 'ar' && details.genericNameAr ? details.genericNameAr : details.genericName; }
}