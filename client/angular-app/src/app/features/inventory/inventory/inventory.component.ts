import { Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
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
import { Permissions } from '../../../core/constants/permissions';
import {
  InventoryAdjustmentType,
  MedicineInventorySummaryDto
} from '../../../core/models/api.models';
import { pickLocalizedGenericName, pickLocalizedMedicineName, pickLocalizedName } from '../../../core/utils/localized-name.utils';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';
import { AdjustStockDialogComponent } from '../adjust-stock-dialog/adjust-stock-dialog.component';
import { MedicineDetailDialogComponent } from '../medicine-detail-dialog/medicine-detail-dialog.component';
import { BatchExpiryStatus, InventoryFacade, StatusFilter, StockFilter } from './inventory.facade';

@Component({
  selector: 'app-inventory',
  standalone: true,
  providers: [InventoryFacade],
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
  protected readonly facade = inject(InventoryFacade);

  displayMedicineName(row: { name: string; nameAr?: string }): string { return pickLocalizedName(row, this.translate); }
  displayGenericName(row: { genericName: string; genericNameAr?: string }): string { return pickLocalizedGenericName(row, this.translate); }
  displayMedicineBatchName(row: { medicineName: string; medicineNameAr?: string }): string { return pickLocalizedMedicineName(row, this.translate); }

  protected readonly permissions = Permissions;
  protected readonly expiryStatuses: BatchExpiryStatus[] = ['All', 'Valid', 'ExpiringSoon', 'Expired'];
  protected readonly stockFilters: StockFilter[] = ['All', 'InStock', 'LowStock', 'OutOfStock'];
  protected readonly statusFilters: StatusFilter[] = ['All', 'Critical', 'Warning', 'Safe', 'Expired'];
  protected readonly adjustmentTypes: InventoryAdjustmentType[] = [
    'Increase', 'Decrease', 'Correction', 'Damaged', 'Expired', 'Returned', 'Sold', 'TransferOut', 'TransferIn'
  ];
  protected readonly summaryColumns = ['name', 'totalQuantity', 'reorderLevel', 'variantCount', 'activeBatchCount', 'nearestExpiryDate'];
  protected readonly batchColumns = ['medicineName', 'batchNumber', 'expiryDate', 'quantityAvailable', 'dispensed', 'supplierName', 'status'];
  protected readonly adjColumns = ['adjustedAt', 'item', 'type', 'quantityChanged', 'beforeAfter', 'reason', 'adjustedBy'];
  protected readonly lowStockColumns = ['medicine', 'variant', 'availableQuantity', 'reorderLevel', 'status'];
  protected readonly alertColumns = ['medicine', 'batch', 'expiryDate', 'daysToExpiry', 'remainingQuantity', 'status'];

  // ---- Facade delegates (template API unchanged) ----
  // Summary tab
  protected get summaryPage() { return this.facade.summaryTable.page; }
  protected get summaryPageSize() { return this.facade.summaryTable.pageSize; }
  protected get summarySearch() { return this.facade.summaryTable.search; }
  protected get summarySortBy() { return this.facade.summaryTable.sortBy; }
  protected get summarySortDir() { return this.facade.summaryTable.sortDir; }
  protected get summarySearchControl() { return this.facade.summaryTable.searchControl; }
  protected get stockStatus() { return this.facade.stockStatus; }
  protected get summary() { return this.facade.summary; }
  protected get summaryCount() { return this.facade.summaryCount; }

  // Batches tab
  protected get batchPage() { return this.facade.batchTable.page; }
  protected get batchPageSize() { return this.facade.batchTable.pageSize; }
  protected get batchSearch() { return this.facade.batchTable.search; }
  protected get batchSortBy() { return this.facade.batchTable.sortBy; }
  protected get batchSortDir() { return this.facade.batchTable.sortDir; }
  protected get batchSearchControl() { return this.facade.batchTable.searchControl; }
  protected get expiryStatus() { return this.facade.expiryStatus; }
  protected get batches() { return this.facade.batches; }
  protected get batchCount() { return this.facade.batchCount; }

  // Alerts tab
  protected get alertPage() { return this.facade.alertTable.page; }
  protected get alertPageSize() { return this.facade.alertTable.pageSize; }
  protected get alertSearch() { return this.facade.alertTable.search; }
  protected get alertSortBy() { return this.facade.alertTable.sortBy; }
  protected get alertSortDir() { return this.facade.alertTable.sortDir; }
  protected get alertSearchControl() { return this.facade.alertTable.searchControl; }
  protected get status() { return this.facade.status; }
  protected get alerts() { return this.facade.alerts; }
  protected get alertCount() { return this.facade.alertCount; }
  protected get alertBadgeCount() { return this.facade.alertBadgeCount; }

  // Adjustments tab
  protected get adjPage() { return this.facade.adjTable.page; }
  protected get adjPageSize() { return this.facade.adjTable.pageSize; }
  protected get adjSearch() { return this.facade.adjTable.search; }
  protected get adjSortBy() { return this.facade.adjTable.sortBy; }
  protected get adjSortDir() { return this.facade.adjTable.sortDir; }
  protected get adjSearchControl() { return this.facade.adjTable.searchControl; }
  protected get adjType() { return this.facade.adjType; }
  protected get adjustments() { return this.facade.adjustments; }
  protected get adjCount() { return this.facade.adjCount; }

  // Low stock tab
  protected get lowStock() { return this.facade.lowStock; }
  protected get lowStockBadgeCount() { return this.facade.lowStockBadgeCount; }

  // ---- Medicine detail dialog (medicine -> variants -> batches) ----
  protected openDetail(row: MedicineInventorySummaryDto): void {
    this.dialog.open(MedicineDetailDialogComponent, {
      width: '800px',
      maxWidth: '95vw',
      data: { id: row.id, name: row.name }
    });
  }

  onSummarySort(sort: Sort): void {
    this.facade.summaryTable.onSortChange(sort);
  }

  onSummaryPage(event: PageEvent): void {
    this.facade.summaryTable.onPage(event);
  }

  onBatchSort(sort: Sort): void {
    this.facade.batchTable.onSortChange(sort);
  }

  onBatchPage(event: PageEvent): void {
    this.facade.batchTable.onPage(event);
  }

  onAlertSort(sort: Sort): void {
    this.facade.alertTable.onSortChange(sort);
  }

  onAlertPage(event: PageEvent): void {
    this.facade.alertTable.onPage(event);
  }

  onAdjSort(sort: Sort): void {
    this.facade.adjTable.onSortChange(sort);
  }

  onAdjPage(event: PageEvent): void {
    this.facade.adjTable.onPage(event);
  }

  openAdjust(): void {
    const ref = this.dialog.open(AdjustStockDialogComponent, { width: '520px' });
    ref.afterClosed().subscribe((adjusted: boolean) => {
      if (adjusted) {
        this.facade.reloadAfterAdjust();
      }
    });
  }
}
