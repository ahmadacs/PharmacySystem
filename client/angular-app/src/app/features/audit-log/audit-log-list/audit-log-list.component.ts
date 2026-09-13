import { HttpParams } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatIconButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
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
import { MatTooltip } from '@angular/material/tooltip';
import { environment } from '../../../../environments/environment';
import { AuditAction, AuditEntryDto } from '../../../core/models/api.models';
import { createPagedResource, createPagedTable } from '../../../core/utils/paged-table.utils';
import { TranslatePipe } from '@ngx-translate/core';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';

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
    MatIconButton,
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
    MatTooltip,
    PageHeaderComponent,
    EmptyStateComponent
  ],
  templateUrl: './audit-log-list.component.html',
  styleUrls: ['./audit-log-list.component.scss']
})
export class AuditLogListComponent {
  protected readonly displayedColumns = ['action', 'entity', 'user', 'changedAt', 'expand'];
  protected readonly actions = AUDIT_ACTIONS;

  protected readonly table = createPagedTable({ defaultSortBy: 'changedAt', defaultSortDir: 'desc' });
  protected readonly page = this.table.page;
  protected readonly pageSize = this.table.pageSize;
  protected readonly search = this.table.search;
  protected readonly sortBy = this.table.sortBy;
  protected readonly sortDir = this.table.sortDir;
  protected readonly searchControl = this.table.searchControl;
  protected readonly action = signal('');

  protected readonly expandedId = signal<string | null>(null);

  protected readonly audit = createPagedResource<AuditEntryDto>(() => {
    let params = new HttpParams()
      .set('page', this.table.page())
      .set('pageSize', this.table.pageSize())
      .set('search', this.table.search())
      .set('sortBy', this.table.sortBy())
      .set('sortDir', this.table.sortDir());
    if (this.action()) params = params.set('action', this.action());
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

  toggleDetails(id: string): void {
    this.expandedId.update((current) => (current === id ? null : id));
  }
}