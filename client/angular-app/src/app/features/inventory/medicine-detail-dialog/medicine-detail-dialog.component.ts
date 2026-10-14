import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatTooltip } from '@angular/material/tooltip';
import { CategoryEnum, MedicineDetailsDto, MedicineForm, MedicineUnit } from '../../../core/models/api.models';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';
import { MedicinesService } from '../../medicines/medicines.service';
import { reloadDetails } from '../../../core/utils/entity-helpers';
import { MedicineDetailsBase } from '../../medicines/medicine-details-dialog/medicine-details-base';

export interface MedicineDetailDialogData {
  id: string;
  name: string;
}

@Component({
  selector: 'app-medicine-detail-dialog',
  standalone: true,
  imports: [
    MatButton,
    MatIconButton,
    MatIcon,
    MatProgressBar,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    RiyadhDatePipe,
    CurrencyPipe,
    TranslatePipe,
    EnumTranslatePipe,
    MatTooltip
  ],
  templateUrl: './medicine-detail-dialog.component.html',
  styleUrl: './medicine-detail-dialog.component.scss'
})
export class MedicineDetailDialogComponent extends MedicineDetailsBase {
  private readonly medicinesService = inject(MedicinesService);

  readonly data = inject<MedicineDetailDialogData>(MAT_DIALOG_DATA);
  protected readonly medicine = signal<MedicineDetailsDto | null>(null);

  constructor() {
    super();
    void reloadDetails(this.medicine, () => this.medicinesService.get(this.data.id));
  }
}
