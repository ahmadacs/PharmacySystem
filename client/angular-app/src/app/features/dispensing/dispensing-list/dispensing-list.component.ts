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
import { DispensingRecordDto, PagedResult } from '../../../core/models/api.models';
import { emptyPage } from '../../../core/utils/empty-page';
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

  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly search = signal('');
  protected readonly sortBy = signal('dispensedAt');
  protected readonly sortDir = signal('desc');

  protected readonly searchControl = new FormControl('');
  protected readonly searchValue = toSignal(this.searchControl.valueChanges, { initialValue: '' });

  protected readonly records = httpResource<PagedResult<DispensingRecordDto>>(
    () => {
      const params = new HttpParams()
        .set('page', this.page())
        .set('pageSize', this.pageSize())
        .set('search', this.search())
        .set('sortBy', this.sortBy())
        .set('sortDir', this.sortDir());
      return { url: `${environment.apiUrl}/dispensing`, params };
    },
    { defaultValue: emptyPage<DispensingRecordDto>() }
  );

  protected readonly totalCount = computed(() => this.records.value()?.totalCount ?? 0);

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
    if (!sort.active) return;
    this.sortBy.set(sort.active);
    this.sortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.page.set(1);
  }

  onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  private refreshRecords(): void {
    if (this.page() === 1) {
      void this.records.reload();
    } else {
      this.page.set(1);
    }
  }

  itemsLabel(record: DispensingRecordDto): string {
    return record.items.map((item) => `${item.medicineName} ${item.variantName} (${item.batchNumber}) x${item.quantity}`).join(', ');
  }

  openDispense(): void {
    const picker = this.dialog.open(DispensePickerDialogComponent, { width: '560px' });
    picker.afterClosed().subscribe((prescriptionId: string | null) => {
      if (!prescriptionId) return;
      const ref = this.dialog.open(DispenseDialogComponent, { width: '560px', data: prescriptionId });
      ref.afterClosed().subscribe((dispensed: boolean) => {
        if (dispensed) {
          this.refreshRecords();
        }
      });
    });
  }
}