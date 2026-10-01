import { httpResource } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { MatCard, MatCardContent, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
import { MatProgressBar } from '@angular/material/progress-bar';
import { environment } from '../../../../environments/environment';
import { DashboardSummaryDto } from '../../../core/models/api.models';
import { LocalizationService } from '../../../core/services/localization.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    MatCard,
    MatCardHeader,
    MatCardTitle,
    MatCardSubtitle,
    MatCardContent,
    MatProgressBar,
    PageHeaderComponent,
    TranslatePipe
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent {
  private readonly localization = inject(LocalizationService);
  private readonly translate = inject(TranslateService);

  // Single API call - GET /api/v1/dashboard/summary (inventory counters only, no prescriptions)
  protected readonly summary = httpResource<DashboardSummaryDto>(
    () => ({ url: `${environment.apiUrl}/dashboard/summary` }),
    {
      defaultValue: {
        totalMedicines: 0,
        totalVariants: 0,
        prescriptionsCreatedToday: 0,
        dispensedToday: 0,
        adjustmentsToday: 0,
        lowStock: 0,
        expiredBatches: 0,
        expiringSoon: 0,
        generatedAt: new Date().toISOString()
      }
    }
  );

  // Dynamic subtitle: "{weekday}، {day} {month} — {live update text}" from API GeneratedAt (Asia/Riyadh)
  protected readonly dailySubtitle = computed(() => {
    const raw = this.summary.value().generatedAt;
    const date = raw ? new Date(raw) : new Date();
    const locale = this.localization.currentLang() === 'ar' ? 'ar' : 'en';
    const datePart = new Intl.DateTimeFormat(locale, {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      timeZone: 'Asia/Riyadh'
    }).format(date);
    return `${datePart} — ${this.translate.instant('dashboard.dailyLiveUpdate')}`;
  });

  protected readonly totalMedicinesCount = computed(() => this.summary.value().totalMedicines);
  protected readonly totalVariantsCount = computed(() => this.summary.value().totalVariants);
  protected readonly createdTodayCount = computed(() => this.summary.value().prescriptionsCreatedToday);
  protected readonly dispensedTodayCount = computed(() => this.summary.value().dispensedToday);
  protected readonly adjustmentsTodayCount = computed(() => this.summary.value().adjustmentsToday);
  protected readonly lowStockCount = computed(() => this.summary.value().lowStock);
  protected readonly expiredBatchesCount = computed(() => this.summary.value().expiredBatches);
  protected readonly expiringCount = computed(() => this.summary.value().expiringSoon);
}
