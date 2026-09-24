import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { pickLocalizedMedicineName } from '../../../core/utils/localized-name.utils';
import { daysUntil } from '../../../core/utils/date-utils';
import {
  PatientMedicationItemDto,
  PatientPrescriptionHistoryDto,
} from '../../../core/models/api.models';

export type MedsLookback = 90 | 180 | 365;

/**
 * Presentational medication-history panel, extracted from the 569-line
 * PrescriptionFormDialogComponent to isolate history rendering from the
 * phone-lookup + prescription-form responsibilities.
 */
@Component({
  selector: 'app-meds-history-list',
  standalone: true,
  imports: [NgTemplateOutlet, TranslatePipe, EnumTranslatePipe, MatButton, MatIconButton, MatIcon, MatChipsModule, MatProgressBar],
  templateUrl: './meds-history-list.component.html',
  styleUrl: './meds-history-list.component.scss',
})
export class MedsHistoryListComponent {
  private readonly translate = inject(TranslateService);

  readonly history = input<PatientPrescriptionHistoryDto[]>([]);
  readonly loading = input(false);
  readonly expanded = input(false);
  readonly lookback = input<MedsLookback>(90);
  readonly lookbackOptions = input<MedsLookback[]>([90, 180, 365]);

  readonly expandedChange = output<boolean>();
  readonly lookbackChange = output<MedsLookback>();
  readonly add = output<PatientMedicationItemDto>();

  readonly activeMeds = computed(() =>
    this.history().flatMap((p) =>
      p.items
        .filter((i) => i.isCurrentlyActive)
        .map((i) => ({ ...i, issuedDate: p.issuedDate, prescriptionId: p.id, prescriptionStatus: p.status })),
    ),
  );
  readonly pastMeds = computed(() =>
    this.history().flatMap((p) =>
      p.items
        .filter((i) => !i.isCurrentlyActive)
        .map((i) => ({ ...i, issuedDate: p.issuedDate, prescriptionId: p.id, prescriptionStatus: p.status })),
    ),
  );

  displayName(item: PatientMedicationItemDto): string {
    return pickLocalizedMedicineName(item, this.translate);
  }

  dueInDays(item: PatientMedicationItemDto): number | null {
    return daysUntil(item.nextEligibleDate);
  }
}
