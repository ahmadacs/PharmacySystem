import { HttpParams, httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatSelect, MatOption } from '@angular/material/select';
import { MatSort, MatSortHeader, Sort } from '@angular/material/sort';
import { MatTab, MatTabGroup, MatTabLabel } from '@angular/material/tabs';
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
import { Permissions } from '../../../core/constants/permissions';
import {
  ExpiryAlertDto,
  ExpiryStatus,
  InventoryAdjustmentDto,
  InventoryAdjustmentType,
  LowStockDto,
  MedicineBatchDto,
  MedicineInventorySummaryDto,
  PagedResult,
  StockStatus
} from '../../../core/models/api.models';
import { emptyPage } from '../../../core/utils/empty-page';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';
import { AdjustStockDialogComponent } from '../adjust-stock-dialog/adjust-stock-dialog.component';
import { MedicineDetailDialogComponent } from '../medicine-detail-dialog/medicine-detail-dialog.component';

export type BatchExpiryStatus = 'All' | 'Valid' | 'ExpiringSoon' | 'Expired';
export type StockFilter = 'All' | StockStatus;
export type StatusFilter = 'All' | ExpiryStatus;

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTabGroup,
    MatTab,
    MatTabLabel,
    MatFormField,
    MatInput,
    MatLabel,
    MatSelect,
    MatOption,
    MatButton,
    MatIcon,
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
    MatSort,
    MatSortHeader,
    MatPaginator,
    MatProgressBar,
    RiyadhDatePipe,
    TranslatePipe,
    EnumTranslatePipe,
    PageHeaderComponent,
    EmptyStateComponent,
    HasPermissionDirective
  ],
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.scss']
})
export class InventoryComponent {
  private readonly dialog = inject(MatDialog);
  protected readonly translate = inject(TranslateService);
  private lang(): string { const c: any = (this.translate as any).currentLang; return typeof c === 'function' ? c() : c; }
  displayMedicineName(row: { name: string; nameAr?: string }): string { return this.lang() === 'ar' && (row as any).nameAr ? (row as any).nameAr : row.name; }
  displayGenericName(row: { genericName: string; genericNameAr?: string }): string { return this.lang() === 'ar' && (row as any).genericNameAr ? (row as any).genericNameAr : row.genericName; }
  displayMedicineBatchName(row: { medicineName: string; medicineNameAr?: string }): string { return this.lang() === 'ar' && (row as any).medicineNameAr ? (row as any).medicineNameAr : row.medicineName; }

  protected readonly permissions = Permissions;
  protected readonly expiryStatuses: BatchExpiryStatus[] = ['All', 'Valid', 'ExpiringSoon', 'Expired'];
  protected readonly stockFilters: StockFilter[] = ['All', 'InStock', 'LowStock', 'OutOfStock'];
  protected readonly statusFilters: StatusFilter[] = ['All', 'Critical', 'Warning', 'Safe', 'Expired'];
  protected readonly adjustmentTypes: InventoryAdjustmentType[] = [
    'Increase', 'Decrease', 'Correction', 'Damaged', 'Expired', 'Returned', 'Sold', 'TransferOut', 'TransferIn'
  ];
  protected readonly summaryColumns = ['name', 'stockStatus', 'totalQuantity', 'reorderLevel', 'variantCount', 'activeBatchCount', 'nearestExpiryDate'];
  protected readonly batchColumns = ['medicineName', 'batchNumber', 'expiryDate', 'quantityAvailable', 'dispensed', 'supplierName', 'status'];
  protected readonly adjColumns = ['adjustedAt', 'item', 'type', 'quantityChanged', 'beforeAfter', 'reason', 'adjustedBy'];
  protected readonly lowStockColumns = ['name', 'availableQuantity', 'reorderLevel'];
  protected readonly alertColumns = ['medicine', 'batch', 'expiryDate', 'daysToExpiry', 'remainingQuantity', 'status'];

  // ---- Medicines summary tab ----
  protected readonly summaryPage = signal(1);
  protected readonly summaryPageSize = signal(10);
  protected readonly summarySearch = signal('');
  protected readonly stockStatus = signal<StockFilter>('All');
  protected readonly summarySortBy = signal('name');
  protected readonly summarySortDir = signal('asc');

  protected readonly summarySearchControl = new FormControl('');
  protected readonly summarySearchValue = toSignal(this.summarySearchControl.valueChanges, { initialValue: '' });

  protected readonly summary = httpResource<PagedResult<MedicineInventorySummaryDto>>(
    () => {
      const params = new HttpParams()
        .set('page', this.summaryPage())
        .set('pageSize', this.summaryPageSize())
        .set('search', this.summarySearch())
        .set('stockStatus', this.stockStatus())
        .set('sortBy', this.summarySortBy())
        .set('sortDir', this.summarySortDir());
      return { url: `${environment.apiUrl}/inventory/summary`, params };
    },
    { defaultValue: emptyPage<MedicineInventorySummaryDto>() }
  );

  protected readonly summaryCount = computed(() => this.summary.value()?.totalCount ?? 0);

  // ---- Medicine detail dialog (medicine -> variants -> batches) ----
  protected openDetail(row: MedicineInventorySummaryDto): void {
    this.dialog.open(MedicineDetailDialogComponent, {
      width: '800px',
      maxWidth: '95vw',
      data: { id: row.id, name: row.name }
    });
  }

  // ---- Batches tab ----
  protected readonly batchPage = signal(1);
  protected readonly batchPageSize = signal(10);
  protected readonly batchSearch = signal('');
  protected readonly expiryStatus = signal<BatchExpiryStatus>('All');
  protected readonly batchSortBy = signal('expiryDate');
  protected readonly batchSortDir = signal('asc');

  protected readonly batchSearchControl = new FormControl('');
  protected readonly batchSearchValue = toSignal(this.batchSearchControl.valueChanges, { initialValue: '' });

  protected readonly batches = httpResource<PagedResult<MedicineBatchDto>>(
    () => {
      const params = new HttpParams()
        .set('page', this.batchPage())
        .set('pageSize', this.batchPageSize())
        .set('search', this.batchSearch())
        .set('expiryStatus', this.expiryStatus())
        .set('withinDays', 30)
        .set('sortBy', this.batchSortBy())
        .set('sortDir', this.batchSortDir());
      return { url: `${environment.apiUrl}/inventory/batches`, params };
    },
    { defaultValue: emptyPage<MedicineBatchDto>() }
  );

  protected readonly batchCount = computed(() => this.batches.value()?.totalCount ?? 0);

  // ---- Expiry alerts tab ----
  protected readonly alertPage = signal(1);
  protected readonly alertPageSize = signal(10);
  protected readonly alertSearch = signal('');
  protected readonly status = signal<StatusFilter>('All');
  protected readonly alertSortBy = signal('expiryDate');
  protected readonly alertSortDir = signal('asc');

  protected readonly alertSearchControl = new FormControl('');
  protected readonly alertSearchValue = toSignal(this.alertSearchControl.valueChanges, { initialValue: '' });

  protected readonly alerts = httpResource<PagedResult<ExpiryAlertDto>>(
    () => {
      const params = new HttpParams()
        .set('page', this.alertPage())
        .set('pageSize', this.alertPageSize())
        .set('search', this.alertSearch())
        .set('status', this.status())
        .set('sortBy', this.alertSortBy())
        .set('sortDir', this.alertSortDir());
      return { url: `${environment.apiUrl}/inventory/expiry-alerts`, params };
    },
    { defaultValue: emptyPage<ExpiryAlertDto>() }
  );

  protected readonly alertCount = computed(() => this.alerts.value()?.totalCount ?? 0);

  // Live Critical + Warning batch counts for the tab badge (independent of the
  // current filter/page so the badge always shows the global count).
  protected readonly criticalAlerts = httpResource<PagedResult<ExpiryAlertDto>>(
    () => ({
      url: `${environment.apiUrl}/inventory/expiry-alerts`,
      params: new HttpParams().set('status', 'Critical').set('pageSize', 1)
    }),
    { defaultValue: emptyPage<ExpiryAlertDto>() }
  );

  protected readonly warningAlerts = httpResource<PagedResult<ExpiryAlertDto>>(
    () => ({
      url: `${environment.apiUrl}/inventory/expiry-alerts`,
      params: new HttpParams().set('status', 'Warning').set('pageSize', 1)
    }),
    { defaultValue: emptyPage<ExpiryAlertDto>() }
  );

  protected readonly alertBadgeCount = computed(
    () => (this.criticalAlerts.value()?.totalCount ?? 0) + (this.warningAlerts.value()?.totalCount ?? 0)
  );

  // ---- Adjustments tab ----
  protected readonly adjPage = signal(1);
  protected readonly adjPageSize = signal(10);
  protected readonly adjSearch = signal('');
  protected readonly adjType = signal<InventoryAdjustmentType | null>(null);
  protected readonly adjSortBy = signal('adjustedAt');
  protected readonly adjSortDir = signal('desc');

  protected readonly adjSearchControl = new FormControl('');
  protected readonly adjSearchValue = toSignal(this.adjSearchControl.valueChanges, { initialValue: '' });

  protected readonly adjustments = httpResource<PagedResult<InventoryAdjustmentDto>>(
    () => {
      let params = new HttpParams()
        .set('page', this.adjPage())
        .set('pageSize', this.adjPageSize())
        .set('search', this.adjSearch())
        .set('sortBy', this.adjSortBy())
        .set('sortDir', this.adjSortDir());
      if (this.adjType()) params = params.set('type', this.adjType()!);
      return { url: `${environment.apiUrl}/inventory/adjustments`, params };
    },
    { defaultValue: emptyPage<InventoryAdjustmentDto>() }
  );

  protected readonly adjCount = computed(() => this.adjustments.value()?.totalCount ?? 0);

  // ---- Low stock tab ----
  protected readonly lowStock = httpResource<LowStockDto[]>(
    () => ({ url: `${environment.apiUrl}/inventory/low-stock` }),
    { defaultValue: [] }
  );

  protected readonly lowStockBadgeCount = computed(() => this.lowStock.value().length);

  constructor() {
    effect((onCleanup) => {
      const value = (this.summarySearchValue() ?? '').trim();
      const handle = setTimeout(() => {
        this.summarySearch.set(value);
        this.summaryPage.set(1);
      }, 300);
      onCleanup(() => clearTimeout(handle));
    });

    effect((onCleanup) => {
      const value = (this.alertSearchValue() ?? '').trim();
      const handle = setTimeout(() => {
        this.alertSearch.set(value);
        this.alertPage.set(1);
      }, 300);
      onCleanup(() => clearTimeout(handle));
    });

    effect((onCleanup) => {
      const value = (this.batchSearchValue() ?? '').trim();
      const handle = setTimeout(() => {
        this.batchSearch.set(value);
        this.batchPage.set(1);
      }, 300);
      onCleanup(() => clearTimeout(handle));
    });

    effect((onCleanup) => {
      const value = (this.adjSearchValue() ?? '').trim();
      const handle = setTimeout(() => {
        this.adjSearch.set(value);
        this.adjPage.set(1);
      }, 300);
      onCleanup(() => clearTimeout(handle));
    });
  }

  onSummarySort(sort: Sort): void {
    if (!sort.active) return;
    this.summarySortBy.set(sort.active);
    this.summarySortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.summaryPage.set(1);
  }

  onSummaryPage(event: PageEvent): void {
    this.summaryPageSize.set(event.pageSize);
    this.summaryPage.set(event.pageIndex + 1);
  }

  onBatchSort(sort: Sort): void {
    if (!sort.active) return;
    this.batchSortBy.set(sort.active);
    this.batchSortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.batchPage.set(1);
  }

  onBatchPage(event: PageEvent): void {
    this.batchPageSize.set(event.pageSize);
    this.batchPage.set(event.pageIndex + 1);
  }

  onAlertSort(sort: Sort): void {
    if (!sort.active) return;
    this.alertSortBy.set(sort.active);
    this.alertSortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.alertPage.set(1);
  }

  onAlertPage(event: PageEvent): void {
    this.alertPageSize.set(event.pageSize);
    this.alertPage.set(event.pageIndex + 1);
  }

  onAdjSort(sort: Sort): void {
    if (!sort.active) return;
    this.adjSortBy.set(sort.active);
    this.adjSortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.adjPage.set(1);
  }

  onAdjPage(event: PageEvent): void {
    this.adjPageSize.set(event.pageSize);
    this.adjPage.set(event.pageIndex + 1);
  }

  openAdjust(): void {
    const ref = this.dialog.open(AdjustStockDialogComponent, { width: '520px' });
    ref.afterClosed().subscribe((adjusted: boolean) => {
      if (adjusted) {
        if (this.adjPage() === 1) {
          void this.adjustments.reload();
        } else {
          this.adjPage.set(1);
        }
        void this.batches.reload();
        void this.lowStock.reload();
        void this.summary.reload();
        void this.alerts.reload();
        void this.criticalAlerts.reload();
        void this.warningAlerts.reload();
      }
    });
  }
}