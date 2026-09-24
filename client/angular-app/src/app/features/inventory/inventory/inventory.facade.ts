import { HttpParams } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
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
import { createPagedResource, createPagedTable } from '../../../core/utils/paged-table.utils';

export type BatchExpiryStatus = 'All' | 'Valid' | 'ExpiringSoon' | 'Expired';
export type StockFilter = 'All' | StockStatus;
export type StatusFilter = 'All' | ExpiryStatus;

/**
 * Data facade for the inventory tabs.
 * All 5 paged lists share createPagedResource (typed empty-page + totalCount);
 * badge counters reuse the same helper with fixed params.
 */
@Injectable()
export class InventoryFacade {
  // ---- Medicines summary tab ----
  readonly summaryTable = createPagedTable({ defaultSortBy: 'name' });
  readonly stockStatus = signal<StockFilter>('All');

  readonly summary = createPagedResource<MedicineInventorySummaryDto>(() => {
    const params = new HttpParams()
      .set('page', this.summaryTable.page())
      .set('pageSize', this.summaryTable.pageSize())
      .set('search', this.summaryTable.search())
      .set('stockStatus', this.stockStatus())
      .set('sortBy', this.summaryTable.sortBy())
      .set('sortDir', this.summaryTable.sortDir());
    return { url: `${environment.apiUrl}/inventory/summary`, params };
  });
  readonly summaryCount = this.summary.totalCount;

  // ---- Batches tab ----
  readonly batchTable = createPagedTable({ defaultSortBy: 'expiryDate' });
  readonly expiryStatus = signal<BatchExpiryStatus>('All');

  readonly batches = createPagedResource<MedicineBatchDto>(() => {
    const params = new HttpParams()
      .set('page', this.batchTable.page())
      .set('pageSize', this.batchTable.pageSize())
      .set('search', this.batchTable.search())
      .set('expiryStatus', this.expiryStatus())
      .set('withinDays', 30)
      .set('sortBy', this.batchTable.sortBy())
      .set('sortDir', this.batchTable.sortDir());
    return { url: `${environment.apiUrl}/inventory/batches`, params };
  });
  readonly batchCount = this.batches.totalCount;

  // ---- Expiry alerts tab ----
  readonly alertTable = createPagedTable({ defaultSortBy: 'expiryDate' });
  readonly status = signal<StatusFilter>('All');

  readonly alerts = createPagedResource<ExpiryAlertDto>(() => {
    const params = new HttpParams()
      .set('page', this.alertTable.page())
      .set('pageSize', this.alertTable.pageSize())
      .set('search', this.alertTable.search())
      .set('status', this.status())
      .set('sortBy', this.alertTable.sortBy())
      .set('sortDir', this.alertTable.sortDir());
    return { url: `${environment.apiUrl}/inventory/expiry-alerts`, params };
  });
  readonly alertCount = this.alerts.totalCount;

  // Live badge counters (fixed params, no table state).
  readonly criticalAlerts = createPagedResource<ExpiryAlertDto>(() => ({
    url: `${environment.apiUrl}/inventory/expiry-alerts`,
    params: new HttpParams().set('status', 'Critical').set('pageSize', 1),
  }));
  readonly warningAlerts = createPagedResource<ExpiryAlertDto>(() => ({
    url: `${environment.apiUrl}/inventory/expiry-alerts`,
    params: new HttpParams().set('status', 'Warning').set('pageSize', 1),
  }));
  readonly alertBadgeCount = computed(
    () => this.criticalAlerts.totalCount() + this.warningAlerts.totalCount(),
  );

  // ---- Adjustments tab ----
  readonly adjTable = createPagedTable({ defaultSortBy: 'adjustedAt', defaultSortDir: 'desc' });
  readonly adjType = signal<InventoryAdjustmentType | null>(null);

  readonly adjustments = createPagedResource<InventoryAdjustmentDto>(() => {
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
  });
  readonly adjCount = this.adjustments.totalCount;

  // ---- Low stock tab ----
  readonly lowStockTable = createPagedTable({ defaultSortBy: 'medicineName' });

  readonly lowStock = createPagedResource<LowStockDto>(() => {
    const params = new HttpParams()
      .set('page', this.lowStockTable.page())
      .set('pageSize', this.lowStockTable.pageSize())
      .set('sortBy', this.lowStockTable.sortBy())
      .set('sortDir', this.lowStockTable.sortDir());
    return { url: `${environment.apiUrl}/inventory/low-stock`, params };
  });
  readonly lowStockBadgeCount = this.lowStock.totalCount;

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
