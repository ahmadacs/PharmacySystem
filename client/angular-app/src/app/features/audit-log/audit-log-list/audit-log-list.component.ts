import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
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
import { AuditAction, AuditEntryDto } from '../../../core/models/api.models';
import { createPagedResource, createPagedTable, buildPagedParams } from '../../../core/utils/paged-table.utils';
import { TranslatePipe } from '@ngx-translate/core';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';
import { AuditLogDetailsDialogComponent } from '../audit-log-details-dialog/audit-log-details-dialog.component';
import { shortAuditId } from '../audit-display.utils';

export const AUDIT_ACTIONS: AuditAction[] = ['Created', 'Updated', 'Deleted'];

@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    EnumTranslatePipe,
    RiyadhDatePipe,
    MatFormField,
    MatInput,
    MatLabel,
    MatSelect,
    MatOption,
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
    PageHeaderComponent,
    EmptyStateComponent
  ],
  templateUrl: './audit-log-list.component.html',
  styleUrls: ['./audit-log-list.component.scss']
})
export class AuditLogListComponent {
  protected readonly displayedColumns = ['action', 'entity', 'user', 'changedAt'];
  protected readonly actions = AUDIT_ACTIONS;

  protected readonly table = createPagedTable({ defaultSortBy: 'changedAt', defaultSortDir: 'desc' });
  protected readonly page = this.table.page;
  protected readonly pageSize = this.table.pageSize;
  protected readonly search = this.table.search;
  protected readonly sortBy = this.table.sortBy;
  protected readonly sortDir = this.table.sortDir;
  protected readonly searchControl = this.table.searchControl;
  protected readonly action = signal('');

  private readonly dialog = inject(MatDialog);

  protected readonly audit = createPagedResource<AuditEntryDto>(() => {
    const params = buildPagedParams(this.table, { action: this.action() || null });
    return { url: `${environment.apiUrl}/auditlog`, params };
  });

  protected readonly totalCount = this.audit.totalCount;

  onSortChange(sort: Sort): void {
    this.table.onSortChange(sort);
  }

  onPage(event: PageEvent): void {
    this.table.onPage(event);
  }

  onFilterChange(): void {
    this.table.resetToFirstPage();
  }

  protected openDetails(row: AuditEntryDto): void {
    this.dialog.open(AuditLogDetailsDialogComponent, { width: '720px', data: row });
  }

  protected shortId(id: string): string {
    return shortAuditId(id);
  }
}