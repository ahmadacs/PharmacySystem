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
import { openForResult } from '../../../core/utils/dialog-helpers';
import type { PagedTable } from '../../../core/utils/paged-table.utils';
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

  // ---- Tab view-models: facade bundles + presentation columns ----
  protected readonly tabs = {
    summary: { ...this.facade.tabs.summary, columns: this.summaryColumns },
    batches: { ...this.facade.tabs.batches, columns: this.batchColumns },
    alerts: { ...this.facade.tabs.alerts, columns: this.alertColumns },
    adjustments: { ...this.facade.tabs.adjustments, columns: this.adjColumns },
    lowStock: { ...this.facade.tabs.lowStock, columns: this.lowStockColumns },
  };

  // Badge over the expiry-alerts tab (Critical + Warning counters).
  protected get alertBadgeCount() { return this.facade.alertBadgeCount; }

  // ---- Medicine detail dialog (medicine -> variants -> batches) ----
  protected openDetail(row: MedicineInventorySummaryDto): void {
    this.dialog.open(MedicineDetailDialogComponent, {
      width: '800px',
      maxWidth: '95vw',
      data: { id: row.id, name: row.name }
    });
  }

  protected onSort(table: PagedTable, sort: Sort): void {
    table.onSortChange(sort);
  }

  protected onPage(table: PagedTable, event: PageEvent): void {
    table.onPage(event);
  }

  openAdjust(): void {
    openForResult(this.dialog, AdjustStockDialogComponent, { width: '520px' }, () => {
      this.facade.reloadAfterAdjust();
    });
  }
}
