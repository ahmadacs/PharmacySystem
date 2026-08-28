import { HttpParams, httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
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
import { AuditAction, AuditEntryDto, PagedResult } from '../../../core/models/api.models';
import { emptyPage } from '../../../core/utils/empty-page';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';

export const AUDIT_ACTIONS: AuditAction[] = ['Created', 'Updated', 'Deleted'];

@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
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

  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly search = signal('');
  protected readonly action = signal('');
  protected readonly sortBy = signal('changedAt');
  protected readonly sortDir = signal('desc');

  protected readonly searchControl = new FormControl('');
  protected readonly searchValue = toSignal(this.searchControl.valueChanges, { initialValue: '' });

  protected readonly expandedId = signal<string | null>(null);

  protected readonly audit = httpResource<PagedResult<AuditEntryDto>>(
    () => {
      let params = new HttpParams()
        .set('page', this.page())
        .set('pageSize', this.pageSize())
        .set('search', this.search())
        .set('sortBy', this.sortBy())
        .set('sortDir', this.sortDir());
      if (this.action()) params = params.set('action', this.action());
      return { url: `${environment.apiUrl}/auditlog`, params };
    },
    { defaultValue: emptyPage<AuditEntryDto>() }
  );

  protected readonly totalCount = computed(() => this.audit.value()?.totalCount ?? 0);

  constructor() {
    effect((onCleanup) => {
      const value = (this.searchValue() ?? '').trim();
      const handle = setTimeout(() => {
        this.search.set(value);
        this.page.set(1);
      }, 300);
      onCleanup(() => clearTimeout(handle));
    });
  }

  onSortChange(sort: Sort): void {
    if (!sort.active) {
      return;
    }
    this.sortBy.set(sort.active);
    this.sortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.page.set(1);
  }

  onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  onFilterChange(): void {
    this.page.set(1);
  }

  toggleDetails(id: string): void {
    this.expandedId.update((current) => (current === id ? null : id));
  }
}