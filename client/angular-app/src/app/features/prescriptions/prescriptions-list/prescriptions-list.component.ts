import { HttpParams } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatSelect, MatOption } from '@angular/material/select';
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
import { NgClass } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';
import { AuthStore } from '../../../core/auth/auth.store';
import { Permissions, Roles } from '../../../core/constants/permissions';
import { PrescriptionListItemDto, PrescriptionStatus } from '../../../core/models/api.models';
import { createPagedResource, createPagedTable } from '../../../core/utils/paged-table.utils';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { DispenseDialogComponent } from '../../dispensing/dispense-dialog/dispense-dialog.component';
import { PrescriptionDetailsDialogComponent } from '../prescription-details-dialog/prescription-details-dialog.component';
import { PrescriptionFormDialogComponent } from '../prescription-form-dialog/prescription-form-dialog.component';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';

export const PRESCRIPTION_STATUSES: PrescriptionStatus[] = [
  'Pending', 'PartiallyDispensed', 'FullyDispensed', 'Cancelled', 'Expired'
];

@Component({
  selector: 'app-prescriptions-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormField,
    MatInput,
    MatLabel,
    MatSelect,
    MatOption,
    MatButton,
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
    NgClass,
    PageHeaderComponent,
    EmptyStateComponent,
    HasPermissionDirective,
    TranslatePipe,
    EnumTranslatePipe
  ],
  templateUrl: './prescriptions-list.component.html',
  styleUrls: ['./prescriptions-list.component.scss']
})
export class PrescriptionsListComponent {
  private readonly dialog = inject(MatDialog);
  protected readonly auth = inject(AuthStore);
  private readonly translate = inject(TranslateService);

  protected readonly permissions = Permissions;
  protected readonly roles = Roles;
  protected readonly statuses = PRESCRIPTION_STATUSES;
  protected readonly displayedColumns = computed(() => {
    const columns = ['patientName', 'doctorName', 'issuedDate', 'status', 'itemCount'];
    if (this.auth.hasPermission(Permissions.DispensingCreate)) {
      columns.push('actions');
    }
    return columns;
  });

  protected readonly table = createPagedTable({ defaultSortBy: 'issuedDate', defaultSortDir: 'desc' });
  protected readonly page = this.table.page;
  protected readonly pageSize = this.table.pageSize;
  protected readonly search = this.table.search;
  protected readonly sortBy = this.table.sortBy;
  protected readonly sortDir = this.table.sortDir;
  protected readonly searchControl = this.table.searchControl;
  protected readonly status = signal<PrescriptionStatus | null>(null);

  protected readonly prescriptions = createPagedResource<PrescriptionListItemDto>(() => {
    let params = new HttpParams()
      .set('page', this.table.page())
      .set('pageSize', this.table.pageSize())
      .set('search', this.table.search())
      .set('sortBy', this.table.sortBy())
      .set('sortDir', this.table.sortDir());
    if (this.status()) params = params.set('status', this.status()!);
    return { url: `${environment.apiUrl}/prescriptions`, params };
  });

  protected readonly totalCount = this.prescriptions.totalCount;

  onSortChange(sort: Sort): void {
    this.table.onSortChange(sort);
  }

  onPage(event: PageEvent): void {
    this.table.onPage(event);
  }

  statusClass(status: PrescriptionStatus): string {
    return `badge-${status.toLowerCase()}`;
  }

  private refreshPrescriptions(): void {
    if (this.page() === 1) {
      void this.prescriptions.reload();
    } else {
      this.page.set(1);
    }
  }

  openCreate(): void {
    const ref = this.dialog.open(PrescriptionFormDialogComponent, {
      width: '980px',
      maxWidth: '96vw',
      panelClass: 'sidenav-aware-dialog'
    });
    ref.afterClosed().subscribe((created: boolean) => {
      if (created) {
        this.refreshPrescriptions();
      }
    });
  }

  openDetails(prescription: PrescriptionListItemDto): void {
    this.dialog.open(PrescriptionDetailsDialogComponent, { width: '720px', data: prescription.id });
  }

  openDispense(prescription: PrescriptionListItemDto): void {
    const ref = this.dialog.open(DispenseDialogComponent, { width: '560px', data: prescription.id });
    ref.afterClosed().subscribe((dispensed: boolean) => {
      if (dispensed) {
        this.refreshPrescriptions();
      }
    });
  }
}