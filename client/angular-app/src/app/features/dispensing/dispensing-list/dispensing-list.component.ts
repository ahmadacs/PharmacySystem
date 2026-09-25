import { Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatSort, MatSortHeader, Sort } from '@angular/material/sort';
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
import { DispensingRecordDto } from '../../../core/models/api.models';
import { createPagedResource, createPagedTable, buildPagedParams, refreshPaged } from '../../../core/utils/paged-table.utils';
import { openForResult } from '../../../core/utils/dialog-helpers';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { TranslatePipe } from '@ngx-translate/core';
import { DispenseDialogComponent } from '../dispense-dialog/dispense-dialog.component';
import { DispensePickerDialogComponent } from '../dispense-picker-dialog/dispense-picker-dialog.component';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';

@Component({
  selector: 'app-dispensing-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormField,
    MatInput,
    MatLabel,
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
    PageHeaderComponent,
    EmptyStateComponent,
    HasPermissionDirective,
    TranslatePipe
  ],
  templateUrl: './dispensing-list.component.html',
  styleUrls: ['./dispensing-list.component.scss']
})
export class DispensingListComponent {
  private readonly dialog = inject(MatDialog);

  protected readonly permissions = Permissions;
  protected readonly displayedColumns = ['dispensedAt', 'patientName', 'pharmacistName', 'items', 'notes'];

  protected readonly table = createPagedTable({ defaultSortBy: 'dispensedAt', defaultSortDir: 'desc' });
  protected readonly page = this.table.page;
  protected readonly pageSize = this.table.pageSize;
  protected readonly search = this.table.search;
  protected readonly sortBy = this.table.sortBy;
  protected readonly sortDir = this.table.sortDir;
  protected readonly searchControl = this.table.searchControl;

  protected readonly records = createPagedResource<DispensingRecordDto>(() => {
    const params = buildPagedParams(this.table);
    return { url: `${environment.apiUrl}/dispensing`, params };
  });

  protected readonly totalCount = this.records.totalCount;

  onSortChange(sort: Sort): void {
    this.table.onSortChange(sort);
  }

  onPage(event: PageEvent): void {
    this.table.onPage(event);
  }

  private refreshRecords(): void {
    refreshPaged(this.table, this.records);
  }

  itemsLabel(record: DispensingRecordDto): string {
    return record.items.map((item) => `${item.medicineName} ${item.variantName} (${item.batchNumber}) x${item.quantity}`).join(', ');
  }

  openDispense(): void {
    openForResult(this.dialog, DispensePickerDialogComponent, { width: '560px' }, (prescriptionId: string) => {
      openForResult(this.dialog, DispenseDialogComponent, { width: '560px', data: prescriptionId }, () => {
        this.refreshRecords();
      });
    });
  }
}