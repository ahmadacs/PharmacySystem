import { Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';
import { pickLocalizedMedicineName } from '../../../core/utils/localized-name.utils';
import {
  MedsLookback,
  PatientMedicationItemDto,
  PatientPrescriptionHistoryDto,
} from '../../../core/models/api.models';

export type { MedsLookback };

/**
 * Minimal medicine history: one row per medicine showing only its name and
 * whether it is continuously taken. Active medicines first.
 */
@Component({
  selector: 'app-meds-history-list',
  standalone: true,
  imports: [TranslatePipe, MatButton, MatIconButton, MatIcon, MatProgressBar],
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

  /** One row per medicine — first occurrence wins, active rows first. */
  readonly allMeds = computed(() => {
    const seen = new Set<string>();
    const unique: PatientMedicationItemDto[] = [];
    for (const prescription of this.history()) {
      for (const item of prescription.items) {
        if (!seen.has(item.medicineId)) {
          seen.add(item.medicineId);
          unique.push(item);
        }
      }
    }
    return unique.sort((a, b) => Number(b.isCurrentlyActive) - Number(a.isCurrentlyActive));
  });

  readonly totalCount = computed(() => this.allMeds().length);

  readonly activeCount = computed(() =>
    this.allMeds().filter((i) => i.isCurrentlyActive).length,
  );

  displayName(item: PatientMedicationItemDto): string {
    return pickLocalizedMedicineName(item, this.translate);
  }
}
