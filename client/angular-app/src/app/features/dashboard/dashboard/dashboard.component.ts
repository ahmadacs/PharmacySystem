import { httpResource } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { MatCard, MatCardContent, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
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
import { environment } from '../../../../environments/environment';
import { DashboardSummaryDto } from '../../../core/models/api.models';
import { LocalizationService } from '../../../core/services/localization.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';

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
    PageHeaderComponent,
    EmptyStateComponent,
    TranslatePipe,
    EnumTranslatePipe
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent {
  private readonly localization = inject(LocalizationService);
  private readonly translate = inject(TranslateService);

  // Single API call - GET /api/v1/dashboard/summary (one MediatR query, 6 parallel counts + 2 lists)
  protected readonly summary = httpResource<DashboardSummaryDto>(
    () => ({ url: `${environment.apiUrl}/dashboard/summary` }),
    {
      defaultValue: {
        dispensedToday: 0,
        pending: 0,
        createdToday: 0,
        lowStock: 0,
        expiringSoon: 0,
        fragmented: 0,
        generatedAt: new Date().toISOString(),
        latestPending: [],
        latestFragmented: []
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

  protected readonly dispensedTodayCount = computed(() => this.summary.value().dispensedToday);
  protected readonly pendingCount = computed(() => this.summary.value().pending);
  protected readonly createdTodayCount = computed(() => this.summary.value().createdToday);
  protected readonly lowStockCount = computed(() => this.summary.value().lowStock);
  protected readonly expiringCount = computed(() => this.summary.value().expiringSoon);
  protected readonly fragmentedCount = computed(() => this.summary.value().fragmented);

  protected readonly latestPending = computed(() => this.summary.value().latestPending);
  protected readonly latestFragmented = computed(() => this.summary.value().latestFragmented);

  protected readonly pendingColumns = ['patientName', 'issuedDate', 'status', 'itemCount'];
  protected readonly fragmentedColumns = ['patientName', 'issuedDate', 'status', 'itemCount'];
}
