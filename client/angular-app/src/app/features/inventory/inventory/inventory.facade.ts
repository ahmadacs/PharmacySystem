import { HttpParams, httpResource } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { FormControl } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { effect } from '@angular/core';
import { environment } from '../../../../environments/environment';
import {
  ExpiryAlertDto,
  ExpiryStatus,
  InventoryAdjustmentDto,
  InventoryAdjustmentType,
  LowStockDto,
  MedicineBatchDto,
  MedicineInventorySummaryDto,
  PagedResult,
  StockStatus,
} from '../../../core/models/api.models';
import { emptyPage } from '../../../core/utils/empty-page';
import { createPagedTable } from '../../../core/utils/paged-table.utils';

export type BatchExpiryStatus = 'All' | 'Valid' | 'ExpiringSoon' | 'Expired';
export type StockFilter = 'All' | StockStatus;
export type StatusFilter = 'All' | ExpiryStatus;

/**
 * Data facade extracted from the 366-line InventoryComponent.
 * Owns all 7 httpResources + search/sort/page state; the component keeps
 * only columns, display helpers and dialog wiring so the template is untouched.
 */
@Injectable()
export class InventoryFacade {
  // ---- Medicines summary tab ----
  readonly summaryTable = createPagedTable({ defaultSortBy: 'name' });
  readonly stockStatus = signal<StockFilter>('All');

  readonly summary = httpResource<PagedResult<MedicineInventorySummaryDto>>(
    () => {
      const params = new HttpParams()
        .set('page', this.summaryTable.page())
        .set('pageSize', this.summaryTable.pageSize())
        .set('search', this.summaryTable.search())
        .set('stockStatus', this.stockStatus())
        .set('sortBy', this.summaryTable.sortBy())
        .set('sortDir', this.summaryTable.sortDir());
      return { url: `${environment.apiUrl}/inventory/summary`, params };
    },
    { defaultValue: emptyPage<MedicineInventorySummaryDto>() },
  );
  readonly summaryCount = computed(() => this.summary.value()?.totalCount ?? 0);

  // ---- Batches tab ----
  readonly batchTable = createPagedTable({ defaultSortBy: 'expiryDate' });
  readonly expiryStatus = signal<BatchExpiryStatus>('All');

  readonly batches = httpResource<PagedResult<MedicineBatchDto>>(
    () => {
      const params = new HttpParams()
        .set('page', this.batchTable.page())
        .set('pageSize', this.batchTable.pageSize())
        .set('search', this.batchTable.search())
        .set('expiryStatus', this.expiryStatus())
        .set('withinDays', 30)
        .set('sortBy', this.batchTable.sortBy())
        .set('sortDir', this.batchTable.sortDir());
      return { url: `${environment.apiUrl}/inventory/batches`, params };
    },
    { defaultValue: emptyPage<MedicineBatchDto>() },
  );
  readonly batchCount = computed(() => this.batches.value()?.totalCount ?? 0);

  // ---- Expiry alerts tab ----
  readonly alertTable = createPagedTable({ defaultSortBy: 'expiryDate' });
  readonly status = signal<StatusFilter>('All');

  readonly alerts = httpResource<PagedResult<ExpiryAlertDto>>(
    () => {
      const params = new HttpParams()
        .set('page', this.alertTable.page())
        .set('pageSize', this.alertTable.pageSize())
        .set('search', this.alertTable.search())
        .set('status', this.status())
        .set('sortBy', this.alertTable.sortBy())
        .set('sortDir', this.alertTable.sortDir());
      return { url: `${environment.apiUrl}/inventory/expiry-alerts`, params };
    },
    { defaultValue: emptyPage<ExpiryAlertDto>() },
  );
  readonly alertCount = computed(() => this.alerts.value()?.totalCount ?? 0);

  // Live Critical + Warning batch counts for the tab badge (independent of the
  // current filter/page so the badge always shows the global count).
  readonly criticalAlerts = httpResource<PagedResult<ExpiryAlertDto>>(
    () => ({
      url: `${environment.apiUrl}/inventory/expiry-alerts`,
      params: new HttpParams().set('status', 'Critical').set('pageSize', 1),
    }),
    { defaultValue: emptyPage<ExpiryAlertDto>() },
  );
  readonly warningAlerts = httpResource<PagedResult<ExpiryAlertDto>>(
    () => ({
      url: `${environment.apiUrl}/inventory/expiry-alerts`,
      params: new HttpParams().set('status', 'Warning').set('pageSize', 1),
    }),
    { defaultValue: emptyPage<ExpiryAlertDto>() },
  );
  readonly alertBadgeCount = computed(
    () => (this.criticalAlerts.value()?.totalCount ?? 0) + (this.warningAlerts.value()?.totalCount ?? 0),
  );

  // ---- Adjustments tab ----
  readonly adjTable = createPagedTable({ defaultSortBy: 'adjustedAt', defaultSortDir: 'desc' });
  readonly adjType = signal<InventoryAdjustmentType | null>(null);

  readonly adjustments = httpResource<PagedResult<InventoryAdjustmentDto>>(
    () => {
      let params = new HttpParams()
        .set('page', this.adjTable.page())
        .set('pageSize', this.adjTable.pageSize())
        .set('search', this.adjTable.search())
        .set('sortBy', this.adjTable.sortBy())
        .set('sortDir', this.adjTable.sortDir());
      if (this.adjType()) {
        params = params.set('type', this.adjType()!);
      }
      return { url: `${environment.apiUrl}/inventory/adjustments`, params };
    },
    { defaultValue: emptyPage<InventoryAdjustmentDto>() },
  );
  readonly adjCount = computed(() => this.adjustments.value()?.totalCount ?? 0);

  // ---- Low stock tab ----
  readonly lowStock = httpResource<LowStockDto[]>(
    () => ({ url: `${environment.apiUrl}/inventory/low-stock` }),
    { defaultValue: [] },
  );
  readonly lowStockBadgeCount = computed(() => this.lowStock.value().length);

  reloadAfterAdjust(): void {
    if (this.adjTable.page() === 1) {
      void this.adjustments.reload();
    } else {
      this.adjTable.page.set(1);
    }
    void this.batches.reload();
    void this.lowStock.reload();
    void this.summary.reload();
    void this.alerts.reload();
    void this.criticalAlerts.reload();
    void this.warningAlerts.reload();
  }
}

// Re-exported for components that still import the types from the old location.
export type { PagedResult };
export { FormControl, toSignal, effect };
